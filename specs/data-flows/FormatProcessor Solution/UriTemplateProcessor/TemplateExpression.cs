using System.Runtime.InteropServices.ComTypes;

namespace UriTemplateProcessor {
    public class TemplateExpression : IUrlPart {
        readonly string _partString;

        public TemplateExpression(string partString) {
            _partString = partString;
        }

        public string Name { get; set; }

        public override string ToString() {
            //TODO
            return string.Format("{{{0}}}", _partString);
        }
    }
}