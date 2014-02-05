# NuCmd Reference for Work Service

## nucmd work
Group containing commands for working with the NuGet Work Service

### nucmd work run
Simulates the execution of an Invocation locally **in the nucmd.exe process**.

Run `nucmd help work run` for more parameter descriptions

### nucmd work invoke
Queues an Invocation on a specified Work Service

Run `nucmd help work invoke` for more parameter descriptions

### nucmd work stats
Displays statistics from the Work Service. Stats can be retrieved as a Summary or as lists by Job or by Worker.

Run `nucmd help work stats` for more parameter descriptions

### nucmd work jobs
List Jobs available for execution either Locally (in-process) or on the target Work Service

Run `nucmd help work jobs` for more parameter descriptions

### nucmd work purge
Purges completed Invocations from the database, optionally using a provided timespan to avoid purging recent Invocations

Run `nucmd help work purge` for more parameter descriptions

### nucmd work get
Retrieves Invocations by criteria (see nucmd help) or retrieves the status of a single Invocation (if given an Invocation ID)

Run `nucmd help work get` for more parameter descriptions

### nucmd work log
Retrieves invocation log for a specific Invocation

Run `nucmd help work log` for more parameter descriptions

## nucmd scheduler
Group containing commands for working with the Azure Scheduler

### nucmd scheduler newservice
Creates a new [Scheduler Service](http://msdn.microsoft.com/en-us/library/windowsazure/dn528943.aspx). The services should use a standard Service Host name, with "scheduler" as the Host name. For example "nuget-int-0-scheduler". If this pattern is used, the "-cs" pattern can be omitted from operations on Job Collections and Jobs.

Run `nucmd help scheduler newservice` for more parameter descriptions

### nucmd scheduler deleteservice
Deletes a [Scheduler Service](http://msdn.microsoft.com/en-us/library/windowsazure/dn528936.aspx). 

Run `nucmd help scheduler deleteservice` for more parameter descriptions

### nucmd scheduler services
Lists available [Scheduler Services](http://msdn.microsoft.com/en-us/library/windowsazure/dn495648.aspx). 

Run `nucmd help scheduler services` for more parameter descriptions

### nucmd scheduler newcol
Creates a new [Job Collection](http://msdn.microsoft.com/en-us/library/windowsazure/dn528940.aspx). Job Collection names should take the Scheduler Service name and append an index. For example "nuget-int-0-scheduler-0". These names will appear in the Azure Portal.

Run `nucmd help scheduler newcol` for more parameter descriptions

### nucmd scheduler deletecol
Deletes a [Job Collection](http://msdn.microsoft.com/en-us/library/windowsazure/dn479787.aspx). This can also be done from the Azure Portal. All Jobs in the Collection will be deleted.

Run `nucmd help scheduler deletecol` for more parameter descriptions

### nucmd scheduler collections
Lists available [Job Collections](http://msdn.microsoft.com/en-us/library/windowsazure/dn495647.aspx).

Run `nucmd help scheduler collections` for more parameter descriptions

### nucmd scheduler newjob
Creates a new [Scheduler Job](http://msdn.microsoft.com/en-us/library/windowsazure/dn528937.aspx) which will invoke the specified Work Service Job when fired. If you use an "Instance Name" that already exists, that job will be **overwritten**.

Run `nucmd help scheduler newjob` for more parameter descriptions

### nucmd scheduler deletejob
**NOT IMPLEMENTED YET**: This can be done from the portal in the interim

### nucmd scheduler jobs
Lists scheduled [Jobs](http://msdn.microsoft.com/en-us/library/windowsazure/dn528933.aspx). Or gets information about a single [Job](http://msdn.microsoft.com/en-us/library/windowsazure/dn528948.aspx).

Run `nucmd help scheduler jobs` for more parameter descriptions

### nucmd scheduler refreshjob
Updates the Scheduler Job with the latest URL for the Work Service. Useful if the DNS name of the Service Host for the Work Service changes.

Run `nucmd help scheduler refreshjob` for more parameter descriptions