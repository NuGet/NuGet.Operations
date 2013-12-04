using Xunit;
using Xunit.Extensions;

namespace UriTemplateProcessor.Specs
{
    public class TemplateDataTests {
        [Theory,
        InlineData(TemplateData.SimpleString),
        InlineData(TemplateData.MultipleStringsPlusLengthConstraint),
        InlineData(TemplateData.MultipleQueryStrings)]
        public void Parse_RfcExampleTemplates_Succeeds(string uri) {
            UriTemplate template = null;
            var parsed = UriTemplate.TryParse(uri, out template);
            Assert.Equal(true, parsed);
            Assert.NotNull(template);
            Assert.IsType<UriTemplate>(template);
        }
    }
}
