<#
.SYNOPSIS
Regenerates the Storage Account key NOT currently in use and generates a new connection string

.PARAMETER Service
The service to generate a new connection string for

.PARAMETER Datacenter
The datacenter in which that service runs

.PARAMETER AccountKind
The type of storage account to regenerate the key for (Primary, Legacy, Backup)

.PARAMETER Clip
Use this switch to put the key in the clipboard
#>
function New-StorageConnectionString {
    param(
        [Parameter(Mandatory=$true,Position=0)][string]$Service,
        [Parameter(Mandatory=$true,Position=1)][int]$Datacenter,
        [Parameter(Mandatory=$false,Position=2)][string]$AccountKind = "Primary",
        [Parameter(Mandatory=$false)][switch]$Clip)
    if(!(Get-AzureSubscription -Current)) {
        throw "This operation requires a selected Azure Subscription. Select an environment to ensure a subscription is selected or use Select-AzureSubscription."
    }
    if(!$NuOps -or !$NuOps.CurrentEnvironment) {
        throw "This operation requires a current environment. Use 'env' to set one."
    }

    # Get the service
    $svc = $NuOps.CurrentEnvironment.GetService($Datacenter, $Service)
    if(!$svc) {
        throw "Unknown service $Service."
    }
    if(![String]::Equals($svc.Type, "azureRole", "OrdinalIgnoreCase")) {
        throw "Non-Azure Service: $($svc.Name)."
    }
    $azureService = Get-AzureService $svc.Value
    if(!$azureService) {
        throw "Failed to get Azure Service: $($svc.Value)"
    }

    $azureDeployment = Get-AzureDeployment $azureService.ServiceName -Slot "production" -ErrorAction SilentlyContinue

    $account = $null;
    $currentKey = $null;
    if(!$azureDeployment) {
        # No deployment yet. Prompt for storage account
        $str = $null;
        do {
            Write-Host "There is no production deployment of this service yet. Enter the storage account name you wish to use"
            $str = Read-Host "Account Name"
            $str = $str.Trim()
        } while([String]::IsNullOrWhitespace($str))
        Write-Host "Using Account $str"
        $account = $str
    } else {
        $current = ([xml]$azureDeployment.Configuration).ServiceConfiguration.Role.ConfigurationSettings.Setting | where { $_.name -eq "Storage.$AccountKind" } | select -ExpandProperty value
        $parsed = Parse-StorageConnectionString $current
        if(!$parsed["AccountName"]) {
            throw "Connection string is missing AccountName field!"
        }
        $account = $parsed["AccountName"];
        if(!$parsed["AccountKey"]) {
            throw "Connection string is missing AccountKey field!"
        }
        $currentKey = $parsed["AccountKey"];
    }

    if(!$account) {
        throw "Failed to get storage account!"
    }

    # Get the account
    $azureStorageAccount = Get-AzureStorageAccount $account;
    if(!$azureStorageAccount) {
        throw "Unknown Storage Account: $account"
    }
    $azureStorageKey = Get-AzureStorageKey $account;

    $genKey = "Primary"
    if($currentKey) {
        if([String]::Equals($currentKey, $azureStorageKey.Primary, "Ordinal")) {
            $genKey = "Secondary"
        }
    }
    Write-Host "About to regenerate $genKey Storage Key for $account."
    if(!(Confirm "Rengerate Now?" $false)) {
        throw "User cancelled"
    }
    $newKey = New-AzureStorageKey -KeyType $genKey -StorageAccountName $account
    $keyStr = $newKey.$genKey

    $str = "DefaultEndpointsProtocol=https;AccountName=$account;AccountKey=$keyStr";
    Write-Host "New Storage Connection String value for 'Storage.$AccountKind': "
    Write-Host $str
    if($Clip) {
        $str | clip
        Write-Host "Copied connection string to clipboard."
    }
}