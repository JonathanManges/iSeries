using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using cwbx;

namespace iSeriesConnector
{
    public class Connector
    {
        private AS400System system;
        private Program program;
        private Command command;
        private bool disposed = false;

        private Connector()
        {
            this.system = new AS400System();
            this.program = new Program();
            this.command = new Command();
        }

        public Connector(string systemName, string userId, string password)
            : this()
        {
            this.SystemName = systemName;
            this.UserId = userId;
            this.Password = password;
            this.system.Define(this.SystemName);
            this.system.UserID = this.UserId;
            this.system.Password = this.Password;
            this.system.PromptMode = cwbcoPromptModeEnum.cwbcoPromptNever;
            this.program.system = this.system;
            this.command.system = this.system;
        }

        public string SystemName
        {
            get;
            private set;
        }

        public string UserId
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }

        public ProgramParameters ProgramParameters
        {
            get;
            private set;
        }

        public void Connect()
        {
            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                this.system.Connect(cwbcoServiceEnum.cwbcoServiceRemoteCmd);
                // Notice: This allows the error messages to be automatically answered which in turn returns the error back to this class instead of waiting on a response from the operator
                this.Run("CHGJOB INQMSGRPY(*DFT)");
            }
        }

        public void Call(string programName, string libraryName, ProgramParameters parameters)
        {
            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                this.Connect();
            }

            this.program.ProgramName = programName;
            this.program.LibraryName = libraryName;
            this.ProgramParameters = parameters;

            try
            {
                this.program.Call(this.ProgramParameters);
            }
            catch (Exception ex)
            {
                ThrowCustomException(ex);
            }
        }

        public void Run(string command)
        {
            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                this.Connect();
            }

            try
            {
                this.command.Run(command);
            }
            catch (Exception ex)
            {
                ThrowCustomException(ex);
            }
        }

        public void SetLibraryList(string libraries)
        {
            List<string> libraryList = libraries.TrimEnd().Split(',').ToList();
            libraryList.Reverse();

            foreach (string library in libraryList)
            {
                this.Run(string.Format("ADDLIBLE {0} POSITION(*AFTER QTEMP)", library));
            }
        }

        public void SwitchUser(string newUser)
        {
            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                this.Connect();
            }

            StringConverter stringConverter = new StringConverter();
            LongConverter longConverter = new LongConverter();

            ProgramParameters parameters = new ProgramParameters();

            parameters = new cwbx.ProgramParameters();

            parameters.Append("UserID", cwbx.cwbrcParameterTypeEnum.cwbrcInput, 10);
            stringConverter.Length = 10;
            parameters["UserID"].Value = stringConverter.ToBytes(newUser.Trim().ToUpper());

            parameters.Append("Password", cwbx.cwbrcParameterTypeEnum.cwbrcInput, 10);
            stringConverter.Length = 10;
            parameters["Password"].Value = stringConverter.ToBytes("*NOPWDCHK");

            parameters.Append("Handle", cwbx.cwbrcParameterTypeEnum.cwbrcOutput, 12);

            //Structure errorStucture = new Structure();
            //errorStucture.Fields.Append("bytesprov", 4);
            //errorStucture.Fields.Append("bytesavail", 4);
            //errorStucture.Fields.Append("messageid", 7);
            //errorStucture.Fields.Append("err", 1);
            //errorStucture.Fields.Append("messagedta", 32750);
            //stringConverter.Length = errorStucture.Length;
            //errorStucture.Bytes = stringConverter.ToBytes(string.Empty);
            //errorStucture.Fields["bytesavail"].Value = longConverter.ToBytes(errorStucture.Length);

            //parameters.Append("ErrorCode", cwbrcParameterTypeEnum.cwbrcInout, errorStucture.Length);
            //parameters["ErrorCode"].Value = errorStucture.Bytes;

            this.Call("QSYGETPH", "*LIBL", parameters);

            //errorStucture.Bytes = parameters["ErrorCode"].Value;

            //long bytesprov = longConverter.FromBytes(errorStucture.Fields["bytesprov"].Value);

            //long bytesavail = longConverter.FromBytes(errorStucture.Fields["bytesavail"].Value);

            //stringConverter.Length = 7;
            //string messageid = stringConverter.FromBytes(errorStucture.Fields["messageid"].Value).Trim();

            //stringConverter.Length = 1;
            //string err = stringConverter.FromBytes(errorStucture.Fields["err"].Value).Trim();

            //stringConverter.Length = 32750;
            //string messagedta = stringConverter.FromBytes(errorStucture.Fields["messagedta"].Value).Trim();

            byte[] handle = parameters["Handle"].Value;

            parameters = new cwbx.ProgramParameters();
            parameters.Append("Handle", cwbx.cwbrcParameterTypeEnum.cwbrcInput, 12);
            parameters["Handle"].Value = handle;

            this.Call("QWTSETP", "*LIBL", parameters);
        }

        public void RevertToSelf()
        {
            this.SwitchUser(this.UserId);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public bool VerifyUserIdPassword(string userId, string password)
        {
            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                this.Connect();
            }

            try
            {
                this.system.VerifyUserIDPassword(userId, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Handle managed objects here
            }

            if (this.system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) != 0)
            {
                this.system.Disconnect(cwbcoServiceEnum.cwbcoServiceRemoteCmd);
            }

            Marshal.ReleaseComObject(this.command);
            Marshal.ReleaseComObject(this.program);
            Marshal.ReleaseComObject(this.system);

            this.disposed = true;
        }

        private void ThrowCustomException(Exception ex)
        {
            StringBuilder exception = new StringBuilder();

            cwbx.Errors errors = this.system.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = this.program.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = this.command.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            throw new Exception(exception.ToString());
        }

        ~Connector()
        {
            this.Dispose(false);
        }
    }
}
