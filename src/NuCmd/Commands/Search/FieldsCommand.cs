using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Search
{
    [Description("Gets a list of fields stored in the Lucene index")]
    public class FieldsCommand : SearchServiceCommandBase
    {
        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }
            await Console.WriteTraceLine(Strings.Commands_UsingServiceUri, ServiceUri.AbsoluteUri);

            // Execute the query
            var response = await client.GetStoredFieldNames();

            if (await ReportHttpStatus(response))
            {
                var results = await response.ReadContent();
                await Console.WriteTable(results, s => new
                {
                    FieldName = s
                });
            }
        }
    }
}
