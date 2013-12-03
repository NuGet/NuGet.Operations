using System;
using System.Collections;
using System.Collections.Generic;
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

        protected UriTemplate() {
            _expressions = new List<TemplateExpression>();
        }

        public static bool TryParse(string uri, out UriTemplate template) {
            if(UriTemplateExpression.IsMatch(uri)) {
                template = CreateUriTemplate(uri);
                return true;
            }
            template = null;
            return false;
        }

        static UriTemplate CreateUriTemplate(string uri) {
            var template = new UriTemplate();
            
            throw new NotImplementedException();
            
            // get match groups

            // for each Expression match/group
            string match = null;
            template._expressions.Add(CreateExpression(match));

        }

        public IEnumerable<TemplateExpression> Expressions {
            get { return _expressions; }
        }

        static TemplateExpression CreateExpression(string match) {
            // factory method that creates a different concrete expression object based on the expression type.
            // We'll start out by creating a simple level 1 template (which is just string expansion)
            throw new NotImplementedException();
        }
    }

    public class TemplateExpression {}
}