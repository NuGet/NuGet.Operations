using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;

namespace ApiProto
{
    public class SearchService : ISearch
    {
        public async Task<IList<SearchResult>> Query(string term)
        {
            return await Task.Run(() =>
            {
                return Execute(term);
            });
        }

        private static IList<SearchResult> Execute(string term)
        {
            IList<SearchResult> results = new List<SearchResult>();

            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=nugetgalleryjohtaylo;AccountKey=J1eHhN9WZxRB8KKah8gzliTuQvqjGLmhG1WNIKwW84A/qMa+IbprbqxcWG903Y36iLWxLLdEuGBFZSF34tQ1EQ==";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            AzureDirectory directory = new AzureDirectory(storageAccount, "apiv3index");

            StandardAnalyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);

            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "high", analyzer);
            parser.AllowLeadingWildcard = true;

            string luceneQuery = string.Format("high:{0} medium:{0}^0.8 low:{0}^0.25", term);

            Query query = parser.Parse(luceneQuery);

            IndexSearcher searcher = new IndexSearcher(directory, true);

            DateTime before = DateTime.Now;

            TopDocs docs = searcher.Search(query, 30);

            foreach (ScoreDoc scoreDoc in docs.ScoreDocs)
            {
                Document document = searcher.Doc(scoreDoc.Doc);

                SearchResult result = new SearchResult
                {
                    Uri = document.Get("uri"),
                    Explanation = searcher.Explain(query, scoreDoc.Doc).ToString(),
                    Details = JObject.Parse(document.Get("details"))
                };

                results.Add(result);
            }

            return results;
        }
    }
}
