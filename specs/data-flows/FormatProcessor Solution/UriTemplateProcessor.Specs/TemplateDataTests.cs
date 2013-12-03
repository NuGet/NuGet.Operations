using Xunit;
using Xunit.Extensions;

namespace UriTemplateProcessor.Specs
{
    public class TemplateDataTests {
        [Theory,
        InlineData("http://example.com/~{username}/"),
        InlineData("http://example.com/dictionary/{term:1}/{term}"),
        InlineData("http://example.com/search{?q,lang}")]
        public void Parse_RfcExampleTemplates_Succeeds(string uri) {
            UriTemplate template = null;
            var parsed = UriTemplate.TryParse(uri, out template);
            Assert.Equal(true, parsed);
            Assert.NotNull(template);
            Assert.IsType<UriTemplate>(template);
        }
    }
}
