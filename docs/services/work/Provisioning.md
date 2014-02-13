# Provisioning
This guide lists the resources required to run an instance of the Work Service and provides a guide for provisioning them in Windows Azure.

Deployment and Provisioning of non-Azure-hosted Work Services is **not covered in this guide**. We would happily accept pull requests for guides to deploy outside of Azure but since our deployments are all done in Azure, we do not provide a guide for that deployment

## Storage Accounts
The Work Service requires the following Storage Accounts be provided:
1. Primary - A Storage account IN the target datacenter. This should be named "&lt;app&gt;&lt;environment&gt;&lt;dc#&gt;", for example "nugetdev0".
2. Legacy - A Storage account containing APIv2 Data (i.e. packages, etc.). The name of this is not defined and depends on your target environment
3. [Optional] Backup - A Storage account IN a partner (backup) datacenter. This should be the PRIMARY account for that datacenter. For example, "nugetdev1"

## SQL Databases
The Work Service requires the following SQL Databases be provided:
1. Primary - A SQL Database containing an up-to-date deployment of the NuGet.Services.Work.Database Data-tier Application. This should be named "&lt;app&gt;-&lt;environment&gt;-&lt;dc#&gt;", for example "nuget-dev-0"
2. Legacy - A SQL Database containing APIv2 Data. The name of this is not defined and depends on your target environment.
3. Warehouse - A SQL Database containing an APIv2 Warehouse. The name of this is not defined and depends on your target environment.

## Cloud Services/Machines
The Work Service runs on a standard Windows Machine. In Azure, this is provided through an Azure Cloud Service. The cloud service should be named "&lt;app&gt;-&lt;environment&gt;-&lt;dc#&gt;-&lt;hostname&gt;" (when the Work service is hosted on a dedicated machine, we recommend using the hostname "work"). For example, "nuget-dev-0-work".

NOTE: The Work Service API must use TLS/SSL for communication. An SSL Certificate should be uploaded to the Azure Cloud Service and referenced in the CSCFG file.

## Certificates
TODO

## CSCFG Setup
TODO

## Azure Scheduler
In order to actually perform work, Invocations must be queued on the Work Service by an external agent. The primary service we use for that purpose is the [Windows Azure Scheduler](http://www.windowsazure.com/en-us/services/scheduler/). There are commands in `nucmd` for managing scheduler resources, specifically those focused on creating invocations.

Azure Scheduler jobs are grouped in to Job Collections, which are further grouped in to Scheduler Services. In order to begin provisioning jobs, one of each must be created using the below commands. Replace "dev" with the name of the environment, and "North Central US" with the name of the Azure Region in which the scheduler is to be placed. **Scheduler Services** are datacenter-independent, there is one per environment (nuget-[environment name]-scheduler). **Job Collections** are datacenter-dependent, there is one per datacenter (nuget-[environment name]-0-scheduler)

```posh
nucmd scheduler newservice nuget-dev-scheduler -d "NuGet Scheduler Services (dev environment)" -r "North Central US" -l nuget-dev-scheduler
nucmd scheduler newcol nuget-dev-0-scheduler -l nuget-dev-0-scheduler
```

The API is a little quirky, so after creating the service and collection, go to the Portal, open the Scheduler Tab, click on the Job Collection you just created (nuget-dev-0-scheduler in the example above) and go to the Scale tab. Change the plan to "Standard", the Max Jobs to 50 and the Max Frequency to Minute. Then press save.
