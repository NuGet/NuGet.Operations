using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderBlobs
{
    class StorageAddress
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string Container { get; set; }
        public string ConnectionString
        {
            get { return string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", AccountName, AccountKey); }
        }
        public string BaseAddress
        {
            get { return string.Format("http://{0}.blob.core.windows.net/{1}", AccountName, Container); }
        }
    }
}
