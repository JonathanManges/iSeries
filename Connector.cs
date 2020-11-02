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
            system = new AS400System();
            program = new Program();
            command = new Command();
        }

        public Connector(string systemName, string userId, string password)
            : this()
        {
            SystemName = systemName;
            UserId = userId;
            Password = password;
            system.Define(SystemName);
            system.UserID = UserId;
            system.Password = Password;
            system.PromptMode = cwbcoPromptModeEnum.cwbcoPromptNever;
            program.system = system;
            command.system = system;
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
            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                system.Connect(cwbcoServiceEnum.cwbcoServiceRemoteCmd);
                // Notice: This allows the error messages to be automatically answered which in turn returns the error back to this class instead of waiting on a response from the operator
                Run("CHGJOB INQMSGRPY(*DFT)");
            }
        }

        public void Call(string programName, string libraryName, ProgramParameters parameters)
        {
            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                Connect();
            }

            program.ProgramName = programName;
            program.LibraryName = libraryName;
            ProgramParameters = parameters;

            try
            {
                program.Call(ProgramParameters);
            }
            catch (Exception ex)
            {
                ThrowCustomException(ex);
            }
        }

        public void Run(string commandName)
        {
            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                Connect();
            }

            try
            {
                command.Run(commandName);
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
                Run(string.Format("ADDLIBLE {0} POSITION(*AFTER QTEMP)", library));
            }
        }

        public void SwitchUser(string newUser)
        {
            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                Connect();
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

            Call("QSYGETPH", "*LIBL", parameters);

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

            Call("QWTSETP", "*LIBL", parameters);
        }

        public void RevertToSelf()
        {
            SwitchUser(UserId);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public bool VerifyUserIdPassword(string userId, string password)
        {
            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) == 0)
            {
                Connect();
            }

            try
            {
                system.VerifyUserIDPassword(userId, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Handle managed objects here
            }

            if (system.IsConnected(cwbcoServiceEnum.cwbcoServiceRemoteCmd) != 0)
            {
                system.Disconnect(cwbcoServiceEnum.cwbcoServiceRemoteCmd);
            }

            Marshal.ReleaseComObject(command);
            Marshal.ReleaseComObject(program);
            Marshal.ReleaseComObject(system);

            disposed = true;
        }

        private void ThrowCustomException(Exception ex)
        {
            StringBuilder exception = new StringBuilder();

            cwbx.Errors errors = system.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = program.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = command.Errors;

            foreach (cwbx.Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            throw new Exception(exception.ToString());
        }

        ~Connector()
        {
            Dispose(false);
        }
    }
}
