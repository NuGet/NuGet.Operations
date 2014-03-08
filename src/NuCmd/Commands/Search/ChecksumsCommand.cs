using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Search
{
    [Description("Gets the checksums for Package Keys within the specified range")]
    public class ChecksumsCommand : SearchServiceCommandBase
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgShortcut("min")]
        [ArgDescription("The first Package Key to retrieve checksums for")]
        public int StartKey { get; set; }

        [ArgRequired]
        [ArgPosition(1)]
        [ArgShortcut("max")]
        [ArgDescription("The last Package Key to retrieve checksums for")]
        public int EndKey { get; set; }

        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }
            await Console.WriteTraceLine(Strings.Commands_UsingServiceUri, ServiceUri.AbsoluteUri);

            // Execute the query
            var response = await client.GetChecksums(StartKey, EndKey);

            if (await ReportHttpStatus(response))
            {
                var results = await response.ReadContent();
                await Console.WriteTable(results, pair => new
                {
                    pair.Key,
                    Checksum = pair.Value
                });
            }
        }
    }
}
