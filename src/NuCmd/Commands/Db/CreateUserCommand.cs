using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using NuGet.Services;
using PowerArgs;

namespace NuCmd.Commands.Db
{
    public class CreateUserCommand : Command
    {
        [ArgDescription("A full Connection String for the database with a user who has Admin access. NOT NORMALLY USED.")]
        public string AdminConnectionString { get; set; }

        [ArgShortcut("db")]
        [ArgDescription("The type of the SQL Database to create the user on")]
        public KnownSqlConnection Database { get; set; }

        [ArgShortcut("s")]
        [ArgDescription("The name of the service the user is for (i.e. 'work', 'search', etc.)")]
        public string Service { get; set; }

        [ArgShortcut("a")]
        [ArgDescription("If set, the user will be an administrator on the database server")]
        public bool Admin { get; set; }

        protected override Task OnExecute()
        {
            throw new NotImplementedException();
        }
    }
}
