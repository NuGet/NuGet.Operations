using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Services.Operations.Model;
using NuGet.Services.Operations.Secrets;

namespace NuGet.Services.Operations.Config
{
    public class ServiceModel
    {
        private Service _service;
        private SecretStore _secrets;
        private ConfigObject _attributes;

        public string adminUri { get { return GetAdminUri().AbsoluteUri; } }
        public string uri { get { return _service.Uri.AbsoluteUri; } }
        public string name { get { return _service.Name; } }
        public string fullName { get { return _service.FullName; } }
        public string type { get { return _service.Type; } }
        public ConfigObject attributes { get { return _attributes; } }

        public ServiceModel(Service service, SecretStore secrets)
        {
            _service = service;
            _secrets = secrets;
            _attributes = new ConfigObject(service.Attributes.ToDictionary(p => p.Key, p => (object)p.Value));
        }

        public Uri GetAdminUri()
        {
            var secret = _secrets.Read(
                new SecretName("http.admin:" + _service.Name, _service.Datacenter.Id), 
                "resolve:" + _service.Type + "." + _service.Name)
                .Result;
            return new UriBuilder(_service.Uri)
            {
                UserName = "admin",
                Password = secret == null ? null : secret.Value
            }.Uri;
        }
    }
}
