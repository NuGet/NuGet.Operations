using System;
using System.Net.Http;

namespace FormatProcessor {
    public class HttpLinkResolver : ILinkResolver {
        HttpClient _client;

        public HttpLinkResolver() {
            _client = new HttpClient();
        }

        public string Get(Uri url) {
            return _client.GetStringAsync(url).Result;
        }
    }
}