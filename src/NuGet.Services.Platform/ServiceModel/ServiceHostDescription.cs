using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Services.ServiceModel
{
    public class ServiceHostDescription
    {
        public ServiceHostInstanceName InstanceName { get; private set; }
        public string MachineName { get; private set; }

        public ServiceHostDescription(ServiceHostInstanceName instanceName, string machineName)
        {
            InstanceName = instanceName;
            MachineName = machineName;
        }
    }
}
