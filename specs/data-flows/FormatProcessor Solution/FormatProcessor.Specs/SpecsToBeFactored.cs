using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormatProcessor.Specs
{
    class SpecsToBeFactored
    {
        private void Sandbox() {

//            var resource = Processor.LoadString(sampleString);    // should also enable loading stream, URL, and UNC (string, URL and UNC may be extension methods that return the appropriate stream)
//
//            // note: may eventually want to allow configuring different link resolvers (e.g. local vs. external vs. UNC, vs. other?)
//
//            // feature: primary record for the resource (it's debatable whether or not sub-resources need to be exposed at the top level)
//            resource.root; // if we don't surface other than the first resource, do we need to expose a property called "root"
//
//            // feature: expose data
//            int someVal = resource.root.data.GetValue<int>("key");
//            int someVal2 = resource.root.data["key"];
//
//            // feature: activate link
//            var resource2 = resource.link("relationship").activate();
//
//            // feature: activate idempotent or non-idempotent link
//            // note >> this could later be improved to a fluent syntax
//            var link = resource.link("formtype");
//            var formData = link.getForm();
//            formData.foo = "val";
//            formData.bar = 1;
//            var resource3 = resource.link("formtype").activate(formData);
//
//            // feature: iterate through link array
//            var links = resource.links("arrayoflinks");
//            foreach (var l in links) {
//                l.activate().data.GetValue<int>("key");
//            }
//
//            // feature: associative array

        }
    }
}
