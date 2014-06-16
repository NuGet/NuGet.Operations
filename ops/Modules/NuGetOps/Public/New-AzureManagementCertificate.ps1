<#
.SYNOPSIS
    Creates a new X.509 Certificate for use as an Azure Management Certificate

.PARAMETER Service
    The name of the service using this certificate

.PARAMETER SubscriptionName
    The name of the subscription this certificate will grant access to. OPTIONAL if you are in a NuOps Environment

.PARAMETER SubscriptionId
    The Id of the subscription this certificate will grant access to. OPTIONAL if you are in a NuOps Environment
#>
function New-AzureManagementCertificate() {
    [CmdletBinding(DefaultParameterSetName="AutoSubscription")]
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$Service,
        [Parameter(Mandatory=$true, Position=1, ParameterSetName="SpecificSubscription")][string]$SubscriptionName,
        [Parameter(Mandatory=$true, Position=2, ParameterSetName="SpecificSubscription")][string]$SubscriptionId,
        [switch]$Force)

    if($PSCmdlet.ParameterSetName -eq "AutoSubscription") {
        $sub = Get-AzureSubscription -Current
        if(!$sub) {
            throw "No current subscription! Set one by either selecting an environment using the 'env' command, or by using the Select-AzureSubscription cmdlet."
        }
        $SubscriptionName = $sub.SubscriptionName;
        $SubscriptionId = $sub.SubscriptionId;
    }

    if(!$SubscriptionName -or !$SubscriptionId) {
        throw "Missing subscription info!"
    }

    New-Certificate -Name $Service -Purpose "azure-management" -Target "$SubscriptionName[$SubscriptionId]" -Force:$Force
}