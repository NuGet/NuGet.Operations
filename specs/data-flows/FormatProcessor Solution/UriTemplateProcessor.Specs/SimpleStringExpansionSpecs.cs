using System;
using System.Linq;
using Machine.Specifications;

namespace UriTemplateProcessor.Specs
{
    class When_parsed_simple_string {
        Because of = () => {
            parsed = UriTemplate.TryParse(TemplateData.SimpleString, out template);
        };

        It should_succeed = () => parsed.ShouldBeTrue();

        It should_have_1_expression = () => template.Expressions.Count().ShouldEqual(1);

        It should_have_username_epxression;
        
        static bool parsed;
        static UriTemplate template;
    }

    class When_parsed_template_with_no_closing_expression_token {
        It should_throw;
    }

    // Note: this is a throw-away test just to verify that string expansion is happening; correct 
    // behavior is to throw if the expression value is not supplied
    class When_geting_string_from_uri_template {
        Establish that = () => UriTemplate.TryParse(TemplateData.SimpleString, out template);

        Because of = () => {
            template.GetExpression("username").SetValue("howard");
            templateString = template.ToString();
        };

        It Should_be_same_as_input_string = () => templateString.ShouldEqual(TemplateData.SimpleString);

        static string templateString;
        static string expectedTemplateString = "http://example.com/~howard/";
        static UriTemplate template;
    }
}