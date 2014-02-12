# Deploying the Work Service
**NOTE**: These notes are _primarily_ written to be relevant to the Microsoft-operated NuGet Services (NuGet.org)

The primary deployment vessel for the Work Service is an [Azure Service Host](../../deployment/AzureHosting.md). The "NuGet.Services.Work.Cloud" project is an Azure Cloud Service project that is configured to create an Azure Worker Role that hosts the Work Service. During the integration build on [build.nuget.org](http://build.nuget.org), this project is used to produce a CSPKG file with the same name (NuGet.Services.Work.Cloud.cspkg).

## Deployment Process

### Part 0. Prepare for Deployment
First, download the following artifacts from the build you plan to deploy:

	Services/Work/NuGet.Services.Work.Cloud.cspkg
	Services/Work/NuGet.Services.Work.Database.dacpac

Then, enter the ops console: In a console window at the root of a clone of the NuGetApi repository, build the solution by running `.\build`, then run `.\ops` to enter the Ops Console. If you have not yet configured Ops, see [Configuring NuOps](../../ops/README.md). Enter the target environment by running the following command

```posh
env target
```

(Where _target_ is the environment you are deploying to, use `env` with no arguments to see a list)

Verify that 'nucmd' is working by running the following command

```posh
nucmd
```

You should see a list of available commands and command groups. If you see an error indicating 'nucmd' could not be found, ensure you have built the repository using `.\build`

### Part 1. Deploy the latest version of the database.
1. Run the following command to check what needs to be deployed from this package

```posh
nucmd db checkdac -db primary -dc 0 -p "C:\path\to\app.dacpac"
```

(Where _C:\path\to\app.dacpac_ is the path to the DACPAC file you downloaded in part 0)

2. Check the output

You should see either "Nothing to be deployed. The database is up-to-date!" or a list of operations to be performed. If this is the first ever deployment of this service, you will see quite a few operations, but there's no need to worry too much about what they are since there's no existing data to be overwritten. If this is a later deployment, verify that the new items in the list of operations match up with your expectations (based on bugs and change notes).

4. If step 3 indicated there were operations to be performed, deploy the DAC using the following command. This command will abort if data loss would occur so it should be safe.

```posh
nucmd db deploy -db primary -dc 0 -p "C:\path\to\app.dacpac"
```

(Where _C:\path\to\app.dacpac_ is the path to the DACPAC file you downloaded)

### Part 2. Regenerate Credentials and update configuration
1. Open the existing CSCFG file for the target service. If one does not exist, see the [Provisioning Guide](Provisioning.md) for help creating one.

2. Regenerate the Primary Storage key using the following command in NuOps. After running the command, the new string will be in the clipboard **and the storage account key will have already been changed in Azure.**

```posh
New-StorageConnectionString -Service work -Datacenter 0 -Clip
```

NOTE: If this is the first deployment, you will be prompted to enter the name of the target storage account.

3. Paste the new storage connection string in as the new value for `Storage.Primary` **AND** `Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString` CSCFG settings

4. Generate a new SQL User for the service by running the following command in NuOps (while in the root of the NuGetApi repository)

```posh
nucmd db createuser -dc 0 -sv work -sa -s work -db primary -Clip
```

5. Paste the new SQL connection string in as the new value for the "Sql.Primary" CSCFG setting

### Part 3. Deploy the package
First, generate a deployment name:

```posh
Get-DeploymentName | clip
```

Upload the CSPKG and CSCFG file using to the destination service using the Azure portal. For the work service, there is no use in deploying to Staging and VIP swapping, the package should be deployed directly to production. Since the front-end HTTP API is not something that users will access, a VIP swap introduces both an unnecessary step and a possibility for work being done by both the Production and Staging Work Services, which is not a terrible thing, but is an unnecessary complexity.

### Part 4. Verify the deployment
Verify that the package was deployed successfully by testing it out using nucmd. To test the service, grab the admin key using the Get-AdminKey script.

```posh
$pass = Get-AdminKey -Service Work -Datacenter 0
```

Then, use the following command to test the service

```posh
nucmd work jobs -pass $pass
```

The service should respond with a list of available jobs. That should be everything! If you want to verify that the invocation infrastructure is all working, you can invoke the TestPing job:

```posh
nucmd work invoke -j TestPing -pass $pass
```

### Final Steps
If any new Azure Scheduler jobs need to be queued in the new environment, go to the [Scheduler Setup](Scheduler.md) section.