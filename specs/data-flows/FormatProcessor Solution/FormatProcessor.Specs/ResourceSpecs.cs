using Machine.Specifications;

namespace FormatProcessor.Specs
{
    class When_loading_a_resource_from_a_string {
        Because of = () => resource = Resource.Load(strings.nuget_index_json);
        It should_return_non_null_object = () => resource.ShouldNotBeNull();
        It should_have_a_valid_root = () => resource.Root.ShouldNotBeNull();
       
        static Resource resource;
    }

    class When_attempting_to_load_null {
        It should_throw;
        It should_throw_ArgumentNullException;
    }

    class When_attempting_to_load_invalid_json {
        It should_throw;
        It should_throw_wrapped_exception; // we don't want to pass through a json.net exception as it would require consumers to have a direct dependency on json.net types
    }

    class When_loading_a_resource_from_a_url {
        It should_execute_http_request;
        It should_return_non_null_object;
        It should_have_a_valid_root;
    }
}
