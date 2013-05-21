using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;

namespace RenderBlobs
{
    class LuceneGallery
    {
        public static async Task Save(string storageConnectionString, Gallery gallery, string warehouseSqlConnectionString)
        {
            IDictionary<string, int> ranking = Warehouse.GetRanking(warehouseSqlConnectionString);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            await Task.Run(() => 
            {
                IndexPackageRegistrations(storageAccount, gallery, ranking);
            });
        }

        private static void IndexPackageRegistrations(CloudStorageAccount storageAccount, Gallery gallery, IDictionary<string, int> ranking)
        {
            using (AzureDirectory directory = new AzureDirectory(storageAccount, "apiv3index"))
            {
                StandardAnalyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

                //using (IndexWriter indexWriter = new IndexWriter(directory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                using (IndexWriter indexWriter = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    indexWriter.DeleteAll();
                    indexWriter.Commit();

                    //Parallel.ForEach(gallery.PackageRegistrations.Values, (packageRegistration) =>
                    //{
                    //    IndexPackageRegistration(indexWriter, packageRegistration, ranking);
                    //});

                    foreach (Gallery.PackageRegistration packageRegistration in gallery.PackageRegistrations.Values)
                    {
                        IndexPackageRegistration(indexWriter, packageRegistration, ranking);
                    }

                    indexWriter.Optimize();
                    indexWriter.Commit();
                }
            }
        }

        private static void IndexPackageRegistration(IndexWriter indexWriter, Gallery.PackageRegistration packageRegistration, IDictionary<string, int> ranking)
        {
            Gallery.Package latest = packageRegistration.Latest; // maybe this should just be invisible (i.e. PR auto-delegates to P)

            Document document = new Document();

            //  fields for raw text search - boosted in the query high, medium or low

            string id = packageRegistration.Id;
            AddField(document, CreateField("high", id, Field.Store.NO, Field.Index.ANALYZED));
            AddField(document, CreateField("medium", SplitId(id), Field.Store.NO, Field.Index.ANALYZED));
            AddField(document, CreateField("low", CamelSplitId(id), Field.Store.NO, Field.Index.ANALYZED));

            string title = latest.Title ?? id;

            //  does this imply that if you add a title you automatically dilute your score?
            //  in which case should we be boosting (up) titles somehow?

            AddField(document, CreateField("high", title, Field.Store.NO, Field.Index.ANALYZED));
            AddField(document, CreateField("medium", SplitId(title), Field.Store.NO, Field.Index.ANALYZED));
            AddField(document, CreateField("low", CamelSplitId(title), Field.Store.NO, Field.Index.ANALYZED));

            if (latest.Tags != null)
            {
                AddField(document, CreateField("high", latest.Tags, Field.Store.NO, Field.Index.ANALYZED));
            }

            //  fields we want to use from the document - just add all these as the JSON document we actually want

            JObject details = new JObject();
            details.Add("uri", packageRegistration.Name);
            details.Add("id", id);
            details.Add("description", latest.Description);
            details.Add("title", latest.Title ?? id);
            details.Add("iconUrl", (latest.IconUrl ?? new Uri(Gallery.PackageDefaultIcon)).AbsoluteUri);
            details.Add("downloads", latest.DownloadCount);
            JArray owners = new JArray();
            foreach (Gallery.Owner item in packageRegistration.Owners.Values)
            {
                JObject owner = new JObject();
                owner.Add("uri", item.Name);
                owner.Add("userName", item.UserName);
                owners.Add(owner);
            }
            details.Add("owners", owners);
            JArray tags = new JArray();
            foreach (string tag in Gallery.Package.ExtractTags(latest.Tags))
            {
                tags.Add(tag);
            }
            details.Add("tags", tags);

            AddField(document, CreateField("details", details.ToString(), Field.Store.YES, Field.Index.NO));

            //  boosting the document

            float boost = DetermineDocumentBoost(packageRegistration.Id, ranking);

            document.Boost = boost;

            indexWriter.AddDocument(document);
        }

        static Field CreateField(string name, object value, Field.Store store, Field.Index index, float boost = 1.0f)
        {
            if (value == null)
            {
                return null;
            }

            //TODO: this could be improved: Lucene does understand some types
            string strValue = value.ToString();

            Field newField = new Field(name, strValue, store, index);

            if (index == Field.Index.ANALYZED)
            {
                newField.Boost = boost;
            }

            return newField;
        }

        static void AddField(Document document, Field field)
        {
            if (field != null)
            {
                document.Add(field);
            }
        }

        static float DetermineDocumentBoost(string key, IDictionary<string, int> ranking)
        {
            int index;
            if (ranking.TryGetValue(key, out index))
            {
                if (index <= 10) return 4.0f;
                if (index <= 20) return 3.5f;
                if (index <= 40) return 3.0f;
                if (index <= 60) return 2.5f;
                if (index <= 80) return 2.0f;
                if (index <= 100) return 1.5f;
            }

            return 1.0f;
        }

        internal static readonly char[] IdSeparators = new[] { '.', '-' };

        // Split up the id by - and . then join it back into one string (tokens in the same order).
        internal static string SplitId(string term)
        {
            var split = term.Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries);
            return split.Any() ? string.Join(" ", split) : "";
        }

        internal static string CamelSplitId(string term)
        {
            var split = term.Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries);
            var tokenized = split.SelectMany(CamelCaseTokenize);
            return tokenized.Any() ? string.Join(" ", tokenized) : "";
        }

        private static IEnumerable<string> CamelCaseTokenize(string term)
        {
            const int minTokenLength = 3;
            if (term.Length < minTokenLength)
            {
                yield break;
            }

            int tokenEnd = term.Length;
            for (int i = term.Length - 1; i > 0; i--)
            {
                // If the remainder is fewer than 2 chars or we have a token that is at least 2 chars long, tokenize it.
                if (i < minTokenLength || (Char.IsUpper(term[i]) && (tokenEnd - i >= minTokenLength)))
                {
                    if (i < minTokenLength)
                    {
                        // If the remainder is smaller than 2 chars, just return the entire string
                        i = 0;
                    }

                    yield return term.Substring(i, tokenEnd - i);
                    tokenEnd = i;
                }
            }

            // Finally return the term in entirety
            yield return term;
        }
    }
}
