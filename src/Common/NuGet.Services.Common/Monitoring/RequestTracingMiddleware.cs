using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGet.Services.Monitoring
{
    public class RequestTracingMiddleware : OwinMiddleware
    {
        internal const string RequestIdEnvironmentKey = "nuget.requestId";
        
        public RequestTracingMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            RequestTraceEventSource.Log.StartRequest(requestId, context.Request.Method, context.Request.Uri.AbsoluteUri);
            context.Environment[RequestIdEnvironmentKey] = requestId;
            
            try
            {
                await Next.Invoke(context);
            }
            finally
            {
                long contentLength = context.Response.ContentLength ?? -1;
                RequestTraceEventSource.Log.EndRequest(requestId, context.Response.StatusCode, context.Response.ReasonPhrase, contentLength, context.Response.ContentType);
            }
        }
    }
}

namespace Owin
{
    public static class RequestTracingMiddlewareExtensions
    {
        public static IAppBuilder UseRequestTracing(this IAppBuilder app)
        {
            return app.Use(typeof(NuGet.Services.Monitoring.RequestTracingMiddleware));
        }

        public static string GetRequestId(this IOwinContext context)
        {
            return (string)context.Environment[NuGet.Services.Monitoring.RequestTracingMiddleware.RequestIdEnvironmentKey];
        }
    }
}
