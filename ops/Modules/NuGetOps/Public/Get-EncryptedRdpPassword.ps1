<#
.SYNOPSIS
Encrypts the provided password using the provided certificate

.PARAMETER Password
The password to encrypt

.PARAMETER Certificate
A certificate file to use for encryption

#>
function Get-EncryptedRdpPassword {
    param(
        [Parameter(Mandatory=$true,Position=0)][string]$Password,
        [Parameter(Mandatory=$true,Position=1)][string]$Certificate)
    if(!(Test-Path $Certificate)) {
        throw "Certificate file $Certificate does not exist."
    }

    [System.Reflection.Assembly]::LoadWithPartialName("System.Security") | Out-Null

    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 (Convert-Path $Certificate)
    $passBytes = [Text.Encoding]::UTF8.GetBytes($Password)
    $content = New-Object System.Security.Cryptography.Pkcs.ContentInfo @(,$passBytes)
    $envelope = New-Object System.Security.Cryptography.Pkcs.EnvelopedCms $content
    $envelope.Encrypt((New-Object System.Security.Cryptography.Pkcs.CmsRecipient($cert)))

    [Convert]::ToBase64String($envelope.Encode())
}