using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using cwbx;

namespace iSeries
{
    public class Connector : IDisposable
    {
        private readonly AS400System system;
        private readonly Program program;
        private readonly Command command;
        private bool disposed = false;

        public Connector()
        {
            system = new AS400System();
            program = new Program();
            command = new Command();
        }

        public Connector(string systemName, string userId, string password, bool useSecureSockets = false)
            : this()
        {
            SystemName = systemName;
            UserId = userId;
            Password = password;
            system.Define(SystemName);
            system.UserID = UserId;
            //system.Password = Password;
            system.Password = password;
            system.UseSecureSockets = useSecureSockets;
            system.PromptMode = cwbcoPromptModeEnum.cwbcoPromptNever;
            program.system = system;
            command.system = system;
        }

        public string SystemName
        {
            get;
            private set;
        }

        public bool UseSecureSockets
        {
            get;
            private set;
        }

        public string UserId
        {
            get;
            private set;
        }

        private string Password
        {
            get;
            set;
        }

        public ProgramParameters Parameters
        {
            get;
            private set;
        }

        public void Connect()
        {
            if (string.IsNullOrWhiteSpace(SystemName))
            {
                throw new InvalidOperationException("The SystemName property has not been initialized.");
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                throw new InvalidOperationException("The UserId property has not been initialized.");
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("The Password property has not been initialized.");
            }

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
            Parameters = parameters;

            try
            {
                program.Call(Parameters);
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

            libraryList.ForEach(x => Run($"ADDLIBLE {x} POSITION(*AFTER QTEMP)"));
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

            parameters = new ProgramParameters();

            parameters.Append("UserID", cwbrcParameterTypeEnum.cwbrcInput, 10);
            stringConverter.Length = 10;
            parameters["UserID"].Value = stringConverter.ToBytes(newUser.Trim().ToUpper());

            parameters.Append("Password", cwbrcParameterTypeEnum.cwbrcInput, 10);
            stringConverter.Length = 10;
            parameters["Password"].Value = stringConverter.ToBytes("*NOPWDCHK");

            parameters.Append("Handle", cwbrcParameterTypeEnum.cwbrcOutput, 12);

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

            parameters = new ProgramParameters();
            parameters.Append("Handle", cwbrcParameterTypeEnum.cwbrcInput, 12);
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

            Errors errors = system.Errors;

            foreach (Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = program.Errors;

            foreach (Error error in errors)
            {
                exception.AppendLine(error.Text);
            }

            errors = command.Errors;

            foreach (Error error in errors)
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
