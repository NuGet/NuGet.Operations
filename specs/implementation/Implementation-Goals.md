# Implementation Goals

This document specifies some of the implementation-focused design goals (implementation goals? whatever :)) that should be considered when implementing APIv3 designs in order to create a highly available and effective service.

## First Class Diagnostics
Diagnostics has to be the first thing considered when implementing designs. Our existing API and services suffer from mediocre diagnostics which have gotten the job done but still leave us scrambling more than they should when issues arise. All elements of the service MUST be self-monitoring and externally monitored. This can be broken down into a few key goals:

1. Don't spam the logs - Use Verbose tracing appropriately, we don't want to overload the logs. Success messages are good in small numbers to give us a heartbeat, but should be used sparingly
2. Correlate logs as much as possible - Assign/get a unique identifier for each request and ensure every log entry related to that request is either a) stamped with that ID or b) stamped with an ID that can be automatically traced back to the original request.


.. todo, more ...
