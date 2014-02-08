$RepoRoot = (Convert-Path "$PsScriptRoot\..\..\..")
Export-ModuleMember -Variable RepoRoot

# Extract Ops NuGetOpsVersion
$NuGetProps = [xml](cat (Join-Path $RepoRoot "build\NuGet.props"))
$NuGetOpsVersion = New-Object System.Version "$($NuGetProps.Project.PropertyGroup.MajorVersion).$($NuGetProps.Project.PropertyGroup.MinorVersion)"
Export-ModuleMember -Variable NuGetOpsVersion

# Find the Azure SDK
$SDKParent = "$env:ProgramFiles\Microsoft SDKs\Windows Azure\.NET SDK"
$AzureSDKRoot = $null;
if(Test-Path $SDKParent) {
	# Pick the latest
	$AzureSDKRoot = (dir $SDKParent | sort Name -desc | select -first 1).FullName
}
Export-ModuleMember -Variable AzureSDKRoot

if(!$AzureSDKRoot) {
	Write-Warning "Couldn't find the Azure SDK. Some commands may not work."
} else {
	Write-Host "Using Azure SDK at: $AzureSDKRoot"
}

if(!(Get-Command Get-AzureAccount -ErrorAction SilentlyContinue)) {
	throw "The NuGet Operations Module requires that you have the Azure PowerShell Module installed and imported (Import-NuGetOps will import the Azure module automatically, if it is installed)."
}

# Import Private Functions
dir "$PsScriptRoot\Private\*.ps1" | foreach {
	. $_.FullName
}

# Import Public Functions
dir "$PsScriptRoot\Public\*.ps1" | foreach {
	. $_.FullName
	$fnName = $([System.IO.Path]::GetFileNameWithoutExtension($_.FullName));
	if(Test-Path "function:$fnName") {
		Export-ModuleMember -Function $fnName
	}
}

$accounts = @(Get-AzureAccount)
if($accounts.Length -eq 0) {
	Write-Warning "No Azure Accounts found. Run Add-AzureAccount to configure your Azure account."
}

# Locate the NuGet application model
# Check the environment Variable
$NuGetAppModel = $env:NUOPS_APP_MODEL
if(!$NuGetInternalRepo) {
	# Try to find it
	$DefaultInternalRepo = Join-Path (Split-Path -Parent $RepoRoot) "Deployment\AppModel.xml"
	if(Test-Path "$DefaultInternalRepo") {
		$NuGetAppModel = $DefaultInternalRepo
		$env:NUOPS_APP_MODEL = $NuGetAppModel
	} else {
		Write-Warning "Could not find App Model file. Use the NUOPS_APP_MODEL environment variable to set the location of this repository if it is not in the default location of: $DefaultInternalRepo"
		Write-Warning "Some commands may not work without this path"
	}
}
Export-ModuleMember -Variable NuGetAppModel

$Script:NuOps = $null
Export-ModuleMember -Variable NuOps

function Reset-ServiceModel {
	$Script:NuOps = Load-Session $NuGetAppModel
}
Export-ModuleMember -Function Reset-ServiceModel

# If we have the internal repo, load the Environments list
if($NuGetAppModel) {
	Reset-ServiceModel
}

function Write-NuGetOpsPrompt {
	Write-Host -NoNewLine -ForegroundColor White "[env:"
	if($NuOps -and $NuOps.CurrentEnvironment) {
		Write-Host -NoNewLine -ForegroundColor Magenta $NuOps.CurrentEnvironment.Name
	} else {
		Write-Host -NoNewLine -ForegroundColor Gray "none"
	}
	Write-Host -NoNewLine -ForegroundColor White "]"
}
Export-ModuleMember -Function Write-NuGetOpsPrompt

<#
.SYNOPSIS
Gets a list of environments. OR sets the current environment

.PARAMETER Name
The name of the environment to set as current. If not specified, a list of environments will be provided
#>
function env {
    param([Parameter(Mandatory=$false)][string]$Name)
	# Shortcut to BOTH Get-Environment and Set-Environment
	if($Name) {
		Set-Environment $Name
	} else {
		Get-Environment
	}
}
Export-ModuleMember -Function env

$env:PATH = "$env:PATH;$RepoRoot\src\NuCmd\bin\Debug"

Write-Host -BackgroundColor Blue -ForegroundColor White @"
 _____     _____     _      _____ _____ _____ 
|   | |_ _|   __|___| |    |     |     |   __|
| | | | | |  |  |  | - |   |  |  |  |__|__   |
|_|___|___|_____|___|_|    |_____|__|  |_____|
                                              
"@
Write-Host -ForegroundColor Black -BackgroundColor Yellow "Welcome to the NuGet Operations Console (v$NuGetOpsVersion)"