using System;
using System.Security.Policy;
using Machine.Specifications;

namespace FormatProcessor.Specs
{
    class When_loading_a_resource_from_a_string {
        Because of = () => resource = Resource.LoadString(strings.nuget_index_json);
        It should_return_non_null_object = () => resource.ShouldNotBeNull();
        It should_have_a_valid_root = () => resource.Root.ShouldNotBeNull();
       
        static Resource resource;
    }

    class When_attempting_to_load_invalid_json {
        Because of = () => exception = Catch.Exception(() => Resource.LoadString(strings.nuget_index_invalid_json));
        It should_throw = () => exception.ShouldNotBeNull();
        It should_throw_FormatException = () => exception.ShouldBeOfType<FormatException>(); // we don't want to pass through a json.net exception as it would require consumers to have a direct dependency on json.net types
        static Exception exception;
    }

    class When_attempting_to_load_null {
        Because of = () => exception = Catch.Exception(() => Resource.LoadString(null));
        It should_throw = () => exception.ShouldNotBeNull();
        It should_throw_ArgumentNullException = () => exception.ShouldBeOfType<ArgumentNullException>();
        static Exception exception;
    }

    class When_loading_a_resource_from_a_url {
        Establish that = () => {
            url = "https://raw.github.com/NuGet/NuGetApi/master/specs/data-flows/index.json";
        };
        Because of = () => {
            resource = Resource.LoadUrl(new Uri(url));
        };
        It should_execute_http_request;
        It should_return_non_null_object = () => resource.ShouldNotBeNull();
        It should_have_a_valid_root = () => resource.Root.ShouldNotBeNull();
        static string url;
        static Resource resource;
    }
}
