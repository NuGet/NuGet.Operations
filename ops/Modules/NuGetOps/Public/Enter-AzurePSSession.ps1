function Enter-AzurePSSession {
    param(
        [Parameter(Mandatory=$true, Position=0)][string]$ServiceName,
        [Parameter(Mandatory=$false, Position=1)][string]$MachineName,
        [Parameter(Mandatory=$false)][PSCredential]$Credential)

    $sess = New-AzurePSSession -ServiceName $ServiceName -MachineName $MachineName -Credential $Credential
    Enter-PSSession $sess
}