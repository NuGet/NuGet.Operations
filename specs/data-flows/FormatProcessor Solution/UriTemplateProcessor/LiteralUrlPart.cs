namespace UriTemplateProcessor {
    class LiteralUrlPart : IUrlPart {
        readonly string _partString;

        public LiteralUrlPart(string partString) {
            _partString = partString;
        }

        public override string ToString() {
            return _partString;
        }
    }
}