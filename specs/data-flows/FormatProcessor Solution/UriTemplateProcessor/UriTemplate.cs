using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace UriTemplateProcessor {
    public class UriTemplate {
        static readonly Regex UriTemplateExpression = new Regex(
            "^(?<Protocol>\\w+)\r\n:\\/\\/\r\n(?<Domain>[\\w@][\\w.:@]+)\r\n\\/" +
            "?\r\n(?:\r\n[\\w\\.?=%&=\\-@/$,~]*\r\n(?<Expression>\\{[\\w\\d?,:]" +
            "+\\})*\r\n)*\r\n$",
            RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled
            );

        List<TemplateExpression> _expressions;
        Queue<IUrlPart> _urlParts;
        const char expressionStartToken = '{';
        const char expressionEndToken = '}';

        protected UriTemplate() {
            _expressions = new List<TemplateExpression>();
            _urlParts = new Queue<IUrlPart>();
        }

        public static bool TryParse(string uri, out UriTemplate template) {
            if(UriTemplateExpression.IsMatch(uri)) { 
                template = new UriTemplate {_urlParts = ParseTemplate(uri)};
                return true;
            }
            template = null;
            return false;
        }

        static Queue<IUrlPart> ParseTemplate(string uri) {
            var parts = new Queue<IUrlPart>();
            
            var stringStartPos = 0;
            var expressionStartPos = uri.IndexOf(expressionStartToken, stringStartPos);
            int expressionEndPos;
            
            while (expressionStartPos > -1) {
                parts.Enqueue(new LiteralUrlPart(uri.Substring(stringStartPos, expressionStartPos - stringStartPos)));
                
                expressionEndPos = uri.IndexOf(expressionEndToken, expressionStartPos);

                parts.Enqueue(new TemplateExpression(uri.Substring(expressionStartPos + 1, expressionEndPos - expressionStartPos - 1)));

                stringStartPos = expressionEndPos + 1;
                expressionStartPos = uri.IndexOf(expressionStartToken, stringStartPos);
            }

            if(stringStartPos <= uri.Length - 1)
                parts.Enqueue((new LiteralUrlPart(uri.Substring(stringStartPos))));

            return parts;
        }

        public IEnumerable<TemplateExpression> Expressions {
            get {
                return
                    _urlParts.Where(part => part.GetType() == typeof (TemplateExpression)).Cast<TemplateExpression>();
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            while (_urlParts.Count > 0) {
                sb.Append(_urlParts.Dequeue().ToString());
            }
            return sb.ToString();
        }

        public Expression GetExpression(string expressionName) {
            return Expressions.First(e => e.Name == expressionName);
        }
    }
}