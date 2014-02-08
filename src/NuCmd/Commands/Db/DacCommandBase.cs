using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;

namespace NuCmd.Commands.Db
{
    public abstract class DacCommandBase : DatabaseCommandBase
    {
        protected DacServices ConnectDac(SqlConnectionInfo connInfo)
        {
            // We have to use an insecure string :(
            var connStr = new SqlConnectionStringBuilder(connInfo.ConnectionString.ChangeDatabase("master").ConnectionString);
            connStr.UserID = connInfo.Credential.UserId;

            var passwordPtr = Marshal.SecureStringToBSTR(connInfo.Credential.Password);
            connStr.Password = Marshal.PtrToStringBSTR(passwordPtr);
            Marshal.FreeBSTR(passwordPtr);

            var services = new DacServices(connStr.ConnectionString);

            services.Message += (s, a) =>
            {
                Task t = null;
                switch (a.Message.MessageType)
                {
                    case DacMessageType.Error:
                        t = Console.WriteErrorLine(a.Message.Message);
                        break;
                    case DacMessageType.Message:
                        t = Console.WriteInfoLine(a.Message.Message);
                        break;
                    case DacMessageType.Warning:
                        t = Console.WriteWarningLine(a.Message.Message);
                        break;
                }
                if (t != null)
                {
                    t.Wait();
                }
            };
            return services;
        }
    }
}
