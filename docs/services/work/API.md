# Work Service API
The Work Service API is simple and consists of the following resources:

## work/invocations
Represents an invocation in the system.

### GET work/invocations
Gets a list of all ACTIVE (i.e. State **is not** Executed or Cancelled) invocations

### GET work/invocations/{criteria}
Where _criteria_ = All, Active, Completed, Executing, Pending, Hidden, Suspended

Gets a list of all invocations matching the specified criteria:

1. All - All, duh!
2. Active - State **is not** Executed or Cancelled
3. Completed - State **is** Executed or Cancelled
4. Executing - State **is** Executing
5. Pending - Invocation's NextVisibleAt value is **before** the current time (i.e. the invocation is eligible for dequeuing)
6. Hidden - Invocation's NextVisibleAt value is **after** the current time (the invocation cannot be dequeued)
7. Suspended - State **is** Suspended

### GET work/invocations/{id}
Where _id_ is the Invocation ID (see PUT for more info)

Gets all the latest information for a specific invocation

### GET work/invocations/{id}/log
Where _id_ is the Invocation ID (see PUT for more info)

Gets the latest log data for the specified invocation. Log data is only uploaded from the Worker when the Invocation completes or is suspended so this data may lag by quite a bit.

### GET work/invocations/stats
Gets statistics about how many invocations are in each state

### PUT work/invocations
Creates a new invocation

Expected body: 

	{
		job: '', // The name of the job to execute
		source: '', // A freeform string describing the source that created this invocation
		jobInstanceName: '', // See Singleton Jobs below
		unlessAlreadyRunning: true, // See Singleton Jobs in the [Architecture](README.md) section
		payload: {...}, // A JSON dictionary of payload values
		visibilityDelay: '2014-02-05T01:30Z' // The time at which this invocation should become visible (null => Immediately)
	}

Returns a response that includes the ID of the invocation that was created.

### DELETE work/invocations/{id}
Where _id_ is the Invocation ID (see PUT for more info)

Purges the invocation records from the database **after** backing them up to Blob Storage

## work/workers
Represents a Worker thread in the system

### GET work/workers/stats
Gets statistics about the state of invocations run by each worker

## work/jobs
Represents a job in the system

### GET work/jobs
Gets a list of all the jobs in the system

### GET work/jobs/stats
Gets statistics about the state of invocations of each job