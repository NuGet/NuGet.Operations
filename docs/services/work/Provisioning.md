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

## Certificates
TODO

## CSCFG Setup
TODO