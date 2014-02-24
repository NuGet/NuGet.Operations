using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Services.Operations.Model;
using PowerArgs;

namespace NuCmd.Commands
{
    public abstract class EnvironmentCommandBase : Command
    {
        [ArgShortcut("e")]
        [ArgDescription("The environment to work in (defaults to the current environment)")]
        public string Environment { get; set; }

        protected virtual DeploymentEnvironment GetEnvironment(bool required)
        {
            return GetEnvironment(Environment, required);
        }

        protected virtual DeploymentEnvironment GetEnvironment()
        {
            return GetEnvironment(Environment);
        }

        protected virtual Datacenter GetDatacenter(int datacenter)
        {
            return GetDatacenter(datacenter, required: true);
        }

        protected virtual Datacenter GetDatacenter(int datacenter, bool required)
        {
            var env = GetEnvironment(required);
            if (env == null)
            {
                return null;
            }
            return GetDatacenter(env, datacenter, required);
        }
    }
}
