using System;

namespace FormatProcessor {
    public interface ILinkResolver {
        string Get(Uri url);
    }
}