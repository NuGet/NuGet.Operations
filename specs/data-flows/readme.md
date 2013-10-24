# README
The documents contained in this folder are data flows for the scenarios defined in the [NuGet API v3 Scenarios document](https://docs.google.com/document/d/1eezKrni077ic4IcVpN6Pa8o_1hYOI28l06ZS8vyfiWo/edit?usp=sharing). See the document to view and comment on the scenarios. Propose new scenarios using the issue tracker for this repository.

## Format
The format for responses is based on a terse set of conventions over raw JSON. As a result the formal content-type for response will be application/json.

### Format conventions
Adherence to the following set of basic rules should enable generic hypermedia clients to be written for processing responses.

* Every representation contains one or more JSON records
* A JSON record has a key that is a URL
* A URL is absolute and can be remote or local (e.g. fragments are allowed)
* A JSON record has a value that is an object
* A JSON record value object contains a set of links and a data object
* A link has
  * A key that is used by clients to understand the link's semantics 
  * A value that meets one of the following criteria
    * A URL (for outbound, local or templated links) 
    * An object containing prescription for idempotent/non-idempotent links
    * An array for a collection of links of the same type; each value may be either a URL string or an object containing prescription for idempotent/non-idempotent links.
    * An object used as an associative arry and serving the same purose as an array; each value may be either a URL string or an object containing prescription for idempotent/non-idempotent links.
* A JSON record value object can have a key "data" with the value being an object of simple key-value pairs
* The data object should be a simple set of key values and not a deep object graph

### Link relationship types
* self: a fully qualified URL to the JSON record