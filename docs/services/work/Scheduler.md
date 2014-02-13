# Azure Scheduler Setup

## Provisioning
If this is the first deployment of the Work Service, you will need to provision the necessary scheduler resources. See the "Azure Scheduler" section of the [Provisioning Guide](Provisioning.md) for more info.

## Scheduling a Job
To add a job to the schedule, run the `nucmd scheduler newjob` command. Below is the help for that command (which you can view by running `nucmd help scheduler newjob`)

```
trace: NuCmd v3.0.0.0
help : NuCmd Usage:
help :   nucmd <command>
help :   nucmd <group> <command>
help : nucmd scheduler newjob - Creates a new job in the scheduler
help : Usage: NuCmd options
help :
help :    OPTION                  TYPE                      POSITION   DESCRIPTION
help :    -Job (-j)               string*                   0          The job to invoke
help :    -InstanceName (-i)      string*                   1          The name of the job instance to create
help :    -Collection (-c)        string                    1          The collection to operate within
help :    -Password (-pass)       string*                   NA         The password for the work service
help :    -Payload (-p)           string                    NA         The JSON dictionary payload to provide to the job
help :    -EncodedPayload (-ep)   string                    NA         A base64-encoded UTF8 payload string to use. Designed for command-line piping
help :    -ServiceUri (-url)      uri                       NA         The URI to the root of the work service
help :    -Frequency (-f)         jobrecurrencefrequency*   NA         The frequency to invoke the job at
help :                                                                   Minute
help :                                                                   Hour
help :                                                                   Day
help :                                                                   Week
help :                                                                   Month
help :                                                                   Year
help :    -Interval (-in)         integer*                  NA         The interval to invoke the job at (example: Frequency = Minute, Interval = 30 => Invoke every 30 minutes)
help :    -Count (-ct)            nullable`1                NA         The maximum number of invocations of this job. Defaults to infinite.
help :    -EndTime (-et)          nullable`1                NA         The time at which recurrence should cease. Defaults to never.
help :    -StartTime (-st)        nullable`1                NA         The time at which the first invocation should occur. Defaults to now.
help :    -Singleton (-sing)      switch                    NA         Set this flag and the job invocation will only be queued if there is no invocation already in progress
help :    -CloudService (-cs)     string                    NA         Specifies the scheduler service to work with. Defaults to the standard one for this environment (nuget-[environment]-scheduler)
help :    -Environment (-e)       string                    NA         The environment to work in (defaults to the current environment)
help :    -WhatIf (-n, -!)        switch                    NA         Report what the command would do but do not actually perform any changes
help :
```

In most cases the "-c" and "-cs" parameters can be left unspecified as they will be assumed to be the default names you used in the Provisioning Guide, so the three parameters you **MUST** specify are "-j", "-i" and "-pass". One additional parameter you will likely need is "-p", which specifies the payload to provide the job. Finally, the remaining parameters mostly control the recurence of the job, which will be discussed below.

### Examples
Invoke the CreateOnlineDatabaseBackup job every 10 minutes to create a live copy of the Primary database if one hasn't been made in the past 45 minutes. But do not invoke this job if it is already running (Singleton Job):

```posh
$pass = Get-AdminKey work
nucmd scheduler newjob -j CreateOnlineDatabaseBackup -i BackupPrimaryDatabase -pass $pass -p "{TargetServer:'Primary',MaxAge:'00:45:00'}" -sing -f Minute -in 10
```
