# Testing the Work Service
Most of the Work Service testing effort should be focused on testing that the **jobs** are having the desired effects. For example, are the statistics being aggregated? Are backup databases being created? Are package backups being created?

## Getting status information from the Work Service.
The first simple command to get a basic status output from the Work Service is the "work get" command. In PowerShell, if you have the Admin Password for the service in the "$pass" variable, this can be achieved using the following command from inside a [NuOps](../../NuOps.md) shell.

	.\nucmd work get all -l 10 -pass $pass

That command retrieves all Invocations in reverse chronological order (most recent first), BUT limits the results to only 10 rows. You can adjust this as necessary to view more data.

Other useful commands include:

	# Get the faulted jobs
	.\nucmd work get faulted -pass $pass

	# Get a summary of job statistics
	.\nucmd work stats Summary