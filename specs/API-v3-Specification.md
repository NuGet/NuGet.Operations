## Status
All designs specified should be considered in an early draft state and should not be viewed as final or authoritative. Feedback is highly desired.

## Goals

### Scenarios to address

1. Can't change the client/server interaction for our clients without also affecting unknown clients 
  * An example is when we wanted to use Lucene for search 
  * We need to maintain existing capabilities 
  * Ability to make targeted server-side changes to aide our clients 
     * EX: change search from Odata to Lucene 
1. Can't currently pass in arbitrary arguments or filtering or sorting 
  * We are currently constrained by Odata for our search 
1. Reduce risk of unintended regressions 
1. Ability to add new logic for new versions of the clent(s) without affecting old clients 
  * Versionable API by operation 
1. Feed source for VS that doesn't contain the API version 
  * Make the client less bound to specific version URLs 
1. Ability to scale the API separately from the Web site (e.g. http://api.nuget.org) 
1. Efficient APIs for ecosystem 
  * Instead of today's "download the world" approach 
  * Catering replication/mirroring 
1. Ability to handle new tasks such as "does this package/version exist?" ala HEAD request for a package 
1. Fallback URLs for downloading packages 
  * Handle CDN blocked scenario 
1. Capability to grow into 1:N gallery:feeds by user 
  * This is the "everything can be a feed" scenario 
1. Better transparency for HTTP requests (akin to NPM) 
1. Backwards-compatible with old clients and Odata consumers 
1. Very little model logic in the client 
1. Ability for client to recognize server's API version 
  * Compatibility with other galleries/feeds

### Candidate list of service capabilities

* Get list of services
* Get list of packages
* Search for packages
* Get list of updates for a set of packages
* Get all versions for a package
* Get list of alternate feed sources
* Get all packages for an author
* Get all packages for an owner (or organization)
* push a package
* setup a curated feed
* Get package statistics
* Get announcements (could drive the site homepage)
* Push a new announcement
* Create a new organization
* Register a user
* Authenticate a user
* Get package dependencies
* Update package metadata (specifically thinking of tags here when they are separated from the nupkg file)
* Report abuse
* [New] Contact all dependents
* Unlist (delete) a package
* Add package owner
* Remove package owner
* Show all versions for a package
* Show dependency graph for a version of a package
* [New] follow a package
* [New] Get feed for followed packages for a user
* [New] follow a user's followed packages
* Edit user profile
* Get list of known licenses/license URLs

## Design
The following sections outline the process for designing the NuGet v3 API as well as design artifacts.

### Process
Because the API v3 should be both flexible in terms of the capabilities it can deliver at any given point in time and self-describing, the majority of design effort as it relates to the contract between client and server will be spent describing the representations (as opposed to describing URL namespaces or functions). Once the representation design has reached a basic level of maturity, client and server authors can independently build implementations that will be compatible with one another.

### Representation
The following section contains details about the design of API v3 representations. The representation design will form the contract between NuGet client and server implementations as the client will be written to understand data elements and link relationship types in order to consume server capabilities. In the even that a representation contains data that cannot be understood by a connector, that connector SHOULD ignore the data.

* The v3 API will use JSON as the underlying media type for its representations. 
* *Open:* what HTTP-specific representation metadata should be used for versioning the representation?
* The design needs to include the ability for the server to dynamically reference another resource as either an external link or an embedded resource. At the moment, use of [URI fragments](http://tools.ietf.org/html/rfc3986#page-24) seems like a good way to go about this.
* *Open:* should queries be treated as a separate application state or should it be rolled into the services state? (leaning towards the former)

NuGet is always in one of the following states:
* packages
  * list of NuGet packages, equivalent to the current 'feed' concept
* package version
  * details on a specific package version - typically used for downloading the package during install.
* Error
  * error state; present details on the error
* query
  * list of the queries that can be executed against 
* services  
  * list of related services 
  * can include services such as mirrors, auth
  * *Open:* should this include search service?
  * *Open:* should publishing be treated as a separate service or as a template state of this service?

#### Representation Design

##### Top level state blocks
A representation can contain one or more of these sections based on the current state of the application

```js
{
  "packages" : [ARRAY],
  "package-version" : [ARRAY],
  "queries" : [ARRAY],
  "error" : {OBJECT},
}

```

```js
{
"services" : {
"search" : "",			// http://tools.ietf.org/html/rfc6570
// another requirement is to enable filtering as a part of search
// this means that search needs to be some kind of a form
"mirrors" : ["", ""],	// Q: is it acceptable to state that the value can be either a single value OR an array?
"auth" : ["", ""],		// could support multiple auth services
"register" : "",			// link to form to reference a new user - may not be in scope for v1, but would allow for a user to regsiter with NuGet from within Visual Studio
"profile" : "",
"packages" : "/packages",
"pacakges-top10" : "/packages/top10",
"events" : "/eventstream"
},     
"statistics" : {
// note that these are UI friendly names and they can be localized via lang-based conneg
"Unique Packages" : "11,071", 
"Total Downloads" : "50,697,974",
"Total Packages" : "79,512"
},
"packages" : {
"links" : {
"publish" : "/packages",
"next" : "",
"previous" : ""
},
"items" : [
{}


{
"links" : {
"self" : "",
"follow" : "",
"unfollow" : "",
"unlist" : "",
"list" : "",
"package" : "", 	// to blob storage
"gallery-page": "",
"icon" : "",
"license" : "",
"project" : "",
"report abuse" : ""
}
"title" : "",
"summary" : "", 
"updated" : "",
"authors" : [
"", ""		// how do we do this where it can be either a link or an embedded resource?
],
"version" : "",
"copyrght" : "",
"created" : "", 	// is this really necessary?
"dependencies" : [
{},			// how do we do this where it can be either a link or an embedded resource?
{}
],
"description" : "",
"downloads" : "",
"is latest" : "",
"is absolute latest" : "",	// what does this even mean? seems like an implementation detail.
"is prerelease" : "",
"language" : "",
"published date" : "",
"package hash" : "",
"package hash algorithm" : "",
"package size" : "",
"release notes" : "",
"require license acceptance" : "",
"tags" : [							// how should this be modeled?
{"{tag name}", "{link of packages by tag}"},
{"{tag name}", "{link of packages by tag}"}
],
"version download count" : "",
},
{}
]
},
// should I be able to support multiple errors in the body?
"error" : {	// https://github.com/collection-json/spec
"title" : "",
"code" : "",
"message" : ""
}
}
```

TODO: clean up formatting on the json