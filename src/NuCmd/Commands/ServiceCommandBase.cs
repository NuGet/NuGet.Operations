using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands
{
    public abstract class ServiceCommandBase<TClient> : Command
        where TClient : class
    {
        [ArgShortcut("url")]
        [ArgDescription("The URI to the root of the search service")]
        public Uri ServiceUri { get; set; }

        [ArgShortcut("pass")]
        [ArgDescription("The admin password for the service")]
        public string Password { get; set; }

        [ArgShortcut("ice")]
        [ArgDescription("Ignore certificate errors")]
        public bool IgnoreCertErrors { get; set; }

        protected string ServiceName { get; set; }

        protected Func<HttpClient, TClient> ClientFactory { get; set; }

        protected ServiceCommandBase(string serviceName, Func<HttpClient, TClient> clientFactory)
        {
            ServiceName = serviceName;
            ClientFactory = clientFactory;
        }

        protected virtual async Task<TClient> OpenClient()
        {
            if (IgnoreCertErrors || (ServiceUri != null && String.Equals(ServiceUri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return true;
                };
            }

            // Prefill values that come from the environment
            if (Session != null && Session.CurrentEnvironment != null && ServiceUri == null)
            {
                ServiceUri = Session.CurrentEnvironment.GetServiceUri(datacenter: 0, service: ServiceName);
            }

            if (ServiceUri == null)
            {
                await Console.WriteErrorLine(Strings.ParameterRequired, "SerivceUri");
                return null;
            }
            else
            {
                // Create a client
                var httpClient = new HttpClient(
                    new ConsoleHttpTraceHandler(
                        Console,
                        new WebRequestHandler()
                        {
                            Credentials = String.IsNullOrEmpty(Password) ? null : new NetworkCredential("admin", Password)
                        }))
                {
                    BaseAddress = ServiceUri
                };

                return ClientFactory(httpClient);
            }
        }
    }
}
