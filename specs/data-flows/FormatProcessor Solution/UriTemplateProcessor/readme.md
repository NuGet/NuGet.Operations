# Readme

The following is a .NET API for working with URI templates as defined by [RFC 6570](http://tools.ietf.org/html/rfc6570).

## Scope

From the specification, "A URI Template is a compact sequence of characters for describing a range of Uniform Resource Identifiers through variable expansion." 

As a result the object model provided by this library should provide the following core capaiblities:

* Identify whether a URI is a URI template
* Load and parse a URI template from a string
* Expose variable placeholders for which the developer can then supply a value
* Validate that all variable constraints are satisfied
* Generate a concrete URI from the filled in template - this fulfills the "Template Processor" as definied in RFC 6570.