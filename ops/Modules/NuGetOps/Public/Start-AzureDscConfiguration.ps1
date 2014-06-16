function Start-AzureDscConfiguration {
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$Path,
        [Parameter(Mandatory=$true, Position=1)][string]$ServiceName,
        [Parameter(Mandatory=$false, Position=2)][string]$MachineName,
        [Parameter(Mandatory=$false)][PSCredential]$Credential,
        [Parameter(Mandatory=$false)][switch]$Wait,
        [Parameter(Mandatory=$false)][switch]$Force,
        [Parameter(Mandatory=$false)][string]$JobName,
        [Parameter(Mandatory=$false)][int]$ThrottleLimit,
        [Parameter(Mandatory=$false)][switch]$WhatIf,
        [Parameter(Mandatory=$false)][switch]$Confirm)

    Write-Host "Connecting to CIM on the target service..."
    $cim = New-AzureCimSession $ServiceName $MachineName $Credential

    Start-DscConfiguration -Path $Path -CimSession $cim -Wait:$Wait -Force:$Force -JobName $JobName -ThrottleLimit $ThrottleLimit -WhatIf:$WhatIf -Confirm:$Confirm
}