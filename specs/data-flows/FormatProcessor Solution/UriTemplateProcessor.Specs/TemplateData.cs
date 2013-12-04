namespace UriTemplateProcessor.Specs {
    public class TemplateData {
        public const string SimpleString = "http://example.com/~{username}/";
        public const string MultipleStringsPlusLengthConstraint = "http://example.com/dictionary/{term:1}/{term}";
        public const string MultipleQueryStrings = "http://example.com/search{?q,lang}";
    }
}