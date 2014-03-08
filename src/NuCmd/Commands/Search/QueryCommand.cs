using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace NuCmd.Commands.Search
{
    [Description("Queries the search service")]
    public class QueryCommand : SearchServiceCommandBase
    {
        [ArgPosition(0)]
        [ArgShortcut("-q")]
        [ArgDescription("The query to execute")]
        public string Query { get; set; }

        [ArgShortcut("-l")]
        [ArgDescription("If this flag is set, the query is interpreted as a raw lucene query and should not be pre-parsed")]
        public bool IsLuceneQuery { get; set; }

        [ArgShortcut("-c")]
        [ArgDescription("Only fetch the count of matching rows instead of executing the full query")]
        public bool CountOnly { get; set; }

        [ArgShortcut("-prj")]
        [ArgDescription("Filter relevance data by project type")]
        public string ProjectType { get; set; }

        [ArgShortcut("-pre")]
        [ArgDescription("Include Pre-release packages")]
        public bool IncludePrerelease { get; set; }

        [ArgShortcut("-f")]
        [ArgDescription("Filter by curated feed")]
        public string CuratedFeed { get; set; }

        [ArgShortcut("-sk")]
        [ArgDescription("Skips the specified number of records")]
        public int Skip { get; set; }

        [ArgShortcut("-ta")]
        [PowerArgs.DefaultValue(10)] // Grr... why does PowerArgs define it's own DefaultValueAttribute??
        [ArgDescription("Takes the specified number of records")]
        public int Take { get; set; }

        [ArgShortcut("-av")]
        [ArgDescription("If this flag is set, all versions of all packages are queried.")]
        public bool AllVersions { get; set; }

        protected override async Task OnExecute()
        {
            var client = await OpenClient();
            if (client == null) { return; }
            await Console.WriteTraceLine(Strings.Commands_UsingServiceUri, ServiceUri.AbsoluteUri);

            // Execute the query
            var response = await client.Search(
                Query, 
                ProjectType, 
                IncludePrerelease, 
                CuratedFeed, 
                Skip, 
                Take, 
                IsLuceneQuery, 
                CountOnly, 
                false,
                AllVersions);
            if (await ReportHttpStatus(response))
            {
                var results = await response.ReadContent();
                await Console.WriteInfoLine(Strings.Search_QueryCommand_Hits, results.TotalHits);
                if (!CountOnly)
                {
                    await Console.WriteTable((IEnumerable<dynamic>)results.Data, d => new
                    {
                        Key = d.Key,
                        Id = d.PackageRegistration.Id,
                        Version = d.NormalizedVersion,
                        Published = d.Published,
                        LastEdited = d.LastEdited
                    });
                }
            }
        }
    }
}
