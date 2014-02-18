using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace NuGet.Services.Operations.Model
{
    public class SharepointSecretStoreConnection : SecretStoreConnection
    {
        public static readonly string TypeName = "sharepoint";
        public static readonly Version MaxVersion = new Version(1, 0);

        public string SiteRoot { get; private set; }
        public Version Version { get; private set; }

        public SharepointSecretStoreConnection(SecretStore store)
        {
            SiteRoot = store.Value;
            Version = store.Version;

            if (Version > MaxVersion)
            {
                throw new NotSupportedException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.SharepointSecretStoreConnection_UnsupportedVersion,
                    Version.ToString()));
            }
        }

        public override Task<Secret> Get(string scope, string resourceType, string resourceName, string key)
        {
            // Sharepoint API isn't Async :(
            // Open the site connection
            var context = new ClientContext(SiteRoot);

            var list = context.Web.Lists.GetByTitle("Secret Store");

            // Build the full key
            string fullKey = resourceType + "." + resourceName;
            if (!String.IsNullOrEmpty(key))
            {
                fullKey += ":" + key;
            }

            // Build a Caml query
            var query = new CamlQuery();
            query.ViewXml = @"
            <View>
                <Query>
                    <Where>
                        <And>
                            <Eq>
                                <FieldRef Name='Key' />
                                <Value Type='Text'>" + fullKey + @"</Value>
                            </Eq>
                            <Contains>
                                <FieldRef Name='Scopes' />
                                <Value Type='Text'>" + scope + @"</Value>
                            </Contains>
                        </And>
                    </Where>
                </Query>
                <RowLimit>1</RowLimit>
            </View>";

            // Get the items
            var itemQuery = list.GetItems(query);
            context.Load(itemQuery);
            context.ExecuteQuery();

            var items = itemQuery.ToList();
            if (items.Count > 1)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.SharepointSecretStoreConnection_AmbiguousMatch,
                    fullKey,
                    scope));
            }

            var item = items.FirstOrDefault();
            if (item == null)
            {
                return null;
            }
            var secret = new Secret();
            secret.Title = (string)item["Title"];
            secret.ResourceType = resourceType;
            secret.ResourceName = resourceName;
            secret.Key = key;
            secret.Username = (string)item["Username"];
            secret.Password = (string)item["Password"];
            secret.Value = (string)item["Value"];
            return Task.FromResult(secret);
        }

        public override Task Set(Secret value)
        {
            throw new NotImplementedException();
        }
    }
}
