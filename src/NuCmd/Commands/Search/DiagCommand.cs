using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Search
{
    [Description("Gets diagnostics information from the index")]
    public class DiagCommand : SearchServiceCommandBase
    {
        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }
            await Console.WriteTraceLine(Strings.Commands_UsingServiceUri, ServiceUri.AbsoluteUri);

            // Execute the query
            var response = await client.GetDiagnostics();

            if (await ReportHttpStatus(response))
            {
                dynamic results = await response.ReadContent();
                await Console.WriteObject(new
                {
                    TotalMemory = FormatSize((int)results.TotalMemory),
                    DocumentCount = results.NumDocs,
                    IndexId = results.SearcherManagerIdentity,
                    LastCommit = results.CommitUserData["commit-time-stamp"],
                    LastAction = results.CommitUserData["commit-description"],
                    Affected = results.CommitUserData["commit-document-count"]
                });
            }
        }

        private static readonly string[] _sizes = new[] {
            "bytes",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        private string FormatSize(int sizeInBytes)
        {
            double currentSize = 0;
            int count = 0;
            do
            {
                currentSize = sizeInBytes / (Math.Pow(1024, count));
                count++;
            } while (currentSize > 1024 && count < _sizes.Length);
            return String.Format("{0:0.00} {1}", currentSize, _sizes[count-1]);
        }
    }
}
