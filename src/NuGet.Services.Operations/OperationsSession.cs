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
        public static readonly string AppModelEnvironmentVariableName = "NUOPS_APP_MODEL";
        public static readonly string CurrentEnvironmentVariableName = "NUOPS_CURRENT_ENVIRONMENT";

        public AppModel Model { get; private set; }
        public DeploymentEnvironment CurrentEnvironment { get; private set; }
        public AzureTokenManager AzureTokens { get; private set; }

        public static string DefaultTokenStore
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuOps"); }
        }

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

        public OperationsSession(AppModel model, AzureTokenManager azureTokens)
        {
            Model = model;
            AzureTokens = azureTokens;
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
            var session = Load(serviceModel);
            string currentEnvironment = Environment.GetEnvironmentVariable(CurrentEnvironmentVariableName);
            if (!String.IsNullOrEmpty(currentEnvironment))
            {
                session.SetCurrentEnvironment(currentEnvironment);
            }
            return session;
        }

        /// <summary>
        /// Loads an operations session using the ServiceModel file located at the specified path
        /// </summary>
        /// <param name="serviceModel">The path to the service model file</param>
        /// <returns></returns>
        public static OperationsSession Load(string serviceModel)
        {
            return Load(serviceModel, DefaultTokenStore);
        }

        /// <summary>
        /// Loads an operations session using the ServiceModel file located at the specified path
        /// </summary>
        /// <param name="serviceModel">The path to the service model file</param>
        /// <param name="tokenStore">The path to the directory that holds Azure Tokens</param>
        /// <returns></returns>
        public static OperationsSession Load(string serviceModel, string azureTokenStore)
        {
        // We want to throw FileNotFound if the service model doesn't exist
            if (!File.Exists(serviceModel))
            {
                throw new FileNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.OperationsSession_ServiceModelDoesNotExist,
                    serviceModel));
            }

            var model = XmlServiceModelDeserializer.LoadServiceModel(serviceModel);
            var tokens = new AzureTokenManager(azureTokenStore);
            return new OperationsSession(model, tokens);
        }

        public void SetCurrentEnvironment(string name)
        {
            SetCurrentEnvironment(name, throwOnFailure: true);
        }
        
        public void SetCurrentEnvironment(string name, bool throwOnFailure)
        {
            DeploymentEnvironment env = this[name];
            if (env == null && throwOnFailure)
            {
                throw new KeyNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.OperationsSession_UnknownEnvironment,
                    name));
            }
            if (env != null)
            {
                CurrentEnvironment = env;
            }
        }
    }
}
