# NuOps
NuOps is the operations console and command-line tool for managing NuGet Services deployments.

## Prerequisites
To configure NuOps, you need the following:

1. A [Deployment Settings Repository](DeploymentRepo.md) (this can be a git repository or just a folder on disk)
2. An App Model file, within the Deployment Settings. A sample can be found [here](SampleAppModel.xml).
3. A clone of the NuGetApi repository

You must install the following components on your machine, if you have not already:
1. (Optional, but recommended) [posh-git](https://github.com/dahlbyk/posh-git)
2. Windows Azure Powershell (available from the [Microsoft Web Platform Installer](http://go.microsoft.com/fwlink/p/?linkid=320376&clcid=0x409))
3. Visual Studio 2013

## Configuring

_NOTE: For engineers working on the NuGet.org Service there is a script which can set up the necessary clones for you, check the internal team site for more information._

1. (Optional, but recommended) Set up a folder to hold your repositories, for example: `C:\Code\Git\NuGet` (replace `C:\Code\Git\NuGet` in later steps with the folder you used if it is different)

2. Clone/Copy/whatever Deployment Settings Repository to `C:\Code\Git\NuGet\Api\Deployment` - If your environment uses a Git repository for this, clone it, otherwise get it on disk somewhere.

3. Clone this repository to `C:\Code\Git\NuGet\Api\NuGet.Operations`

4. Open a Console/PowerShell window to `C:\Code\Git\NuGet\Api\NuGet.Operations` and run `.\ops` to enter the NuOps console

## Using your own folder structure

If you don't want to use the `\Deployment` and `\Api` subfolder structure, you can set NuOps up to use a different folder structure by setting the `NUOPS_APP_MODEL` environment variable to point at the "AppModel.xml" file within your Deployment Settings Repository.
