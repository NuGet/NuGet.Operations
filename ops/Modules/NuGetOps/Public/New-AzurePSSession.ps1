function New-AzurePSSession {
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$ServiceName,
        [Parameter(Mandatory=$false, Position=1)][string]$MachineName,
        [Parameter(Mandatory=$false)][PSCredential]$Credential)

    $instance = Get-VMInstance $ServiceName $MachineName

    # Check if we have the cert
    Install-WinRMCertificate $instance

    # Get the URI
    $uri = Get-AzureWinRMUri $instance.ServiceName $instance.Name

    # Try to get credentials
    if(!$Credential) {
        $Credential = Get-SecretCredential "rdp.$($instance.ServiceName)"
    }

    # Connect
    Write-Host "Creating PowerShell Session on $uri"
    New-PSSession -ConnectionUri $uri -Credential $Credential 
}