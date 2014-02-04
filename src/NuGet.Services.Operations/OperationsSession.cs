using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NuGet.Services.Operations.Model;

namespace NuGet.Services.Operations
{
    public class OperationsSession
    {
        public static readonly string AppModelEnvironmentVariableName = "NUGET_APP_MODEL";

        public AppModel Model { get; private set; }
        public DeploymentEnvironment CurrentEnvironment { get; private set; }
ee
        private IDictionary<string, DeploymentEnvironment> Environments { get; set; }

        public DeploymentEnvironment this[string environmentName]
        {
            get
            {
                DeploymentEnvironment env;
                if (!Environments.TryGetValue(environmentName, out env))
                {
                    return null;
                }
                return env;
            }
        }

        public OperationsSession(AppModel model)
        {
            Model = model;
            Environments = Model.Environments.ToDictionaryByFirstItemWithKey(e => e.Name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads an operations session using the current value of the NUGET_APP_MODEL environment variable, if present
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The NUGET_APP_MODEL environment variable is empty or refers to a directory that does not exist</exception>
        public static OperationsSession LoadFromEnvironment()
        {
            string serviceModel = Environment.GetEnvironmentVariable(AppModelEnvironmentVariableName);
            if (String.IsNullOrEmpty(serviceModel))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.OperationsSession_EnvironmentVariableIsEmpty,
                    AppModelEnvironmentVariableName));
            }
            return Load(serviceModel);
        }

        /// <summary>
        /// Loads an operations session using the ServiceModel file located at the specified path
        /// </summary>
        /// <param name="serviceModel">The path to the service model file</param>
        /// <returns></returns>
        public static OperationsSession Load(string serviceModel)
        {
            if (!File.Exists(serviceModel))
            {
                throw new FileNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.OperationsSession_ServiceModelDoesNotExist,
                    serviceModel));
            }

            var model = XmlServiceModelDeserializer.LoadServiceModel(serviceModel);

            return new OperationsSession(model);
        }

        public void SetCurrentEnvironment(string name)
        {
            DeploymentEnvironment env = this[name];
            if (env == null)
            {
                throw new KeyNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.OperationsSession_UnknownEnvironment,
                    name));
            }
            CurrentEnvironment = env;
        }
    }
}
