using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Services.Monitoring
{
    public class TracingMiddleware : OwinMiddleware
    {
        private RequestTraceEventSource _trace;

        public TracingMiddleware(OwinMiddleware next, RequestTraceEventSource trace) : base(next)
        {
            _trace = trace;
        }

        public override async Task Invoke(IOwinContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            _trace.StartRequest(requestId, context.Request.Method, context.Request.Uri.AbsoluteUri);

            try
            {
                await Next.Invoke(context);
            }
            finally
            {
                long contentLength = context.Response.ContentLength ?? -1;
                _trace.EndRequest(requestId, context.Response.StatusCode, context.Response.ReasonPhrase, contentLength, context.Response.ContentType);
            }
        }
    }
}

namespace Owin
{
    public static class TracingMiddlewareExtensions
    {
        public static IAppBuilder UseTracing(this IAppBuilder app, NuGet.Services.Monitoring.RequestTraceEventSource eventSource)
        {
            return app.Use(typeof(NuGet.Services.Monitoring.TracingMiddleware), eventSource);
        }
    }
}
