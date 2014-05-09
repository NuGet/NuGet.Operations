<#
.SYNOPSIS
    Creates a new X.509 Certificate with a NuGet-compatible name.

.PARAMETER Name
    The name of this certificate (used as the CN field)

.PARAMETER Purpose
    The purpose of this certificate (used as the OU field)

.PARAMETER Target
    The object being authenticated using this certificate (used as the O field).
#>
function New-Certificate() {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][string]$Purpose,
        [Parameter(Mandatory=$false)][string]$Target,
        [Parameter(Mandatory=$false)][string]$IssuerName,
        [switch]$Force)

    if(!$Force -and (Get-Command git -ErrorAction SilentlyContinue)) {
        $status = git status 2>&1;
        if($status -notlike "fatal: Not a git repository*") {
            throw "You are in a Git Repository. Checking in a certificate is a BAD IDEA. Use -Force if you really know what you're doing..."
        }
    }

    $dn = "";
    $env = "Unknown Environment"
    if($NuOps) {
        if(![String]::IsNullOrEmpty($NuOps.DistinguishedName)) {
            $dn = ", " + $NuOps.DistinguishedName
        }
        if($NuOps.CurrentEnvironment) {
            $env = $NuOps.CurrentEnvironment.Name
        }
    }

    $FullName = "CN=$Name"
    if($Target) 
    {
        $FullName += ", OU=$Target"
    }
    $FullName += ", OU=$Purpose, OU=$env, OU=nuget-services$dn"

    Write-Host "Generating Certificate..."
    $FileName = Join-Path (Convert-Path .) "$Name.$Purpose.cer"
    $PfxFileName = Join-Path (Convert-Path .) "$Name.$Purpose.pfx"
    if(Test-Path $FileName) {
        if($Force) {
            del $FileName
        } else {
            throw "There is already a cert at $FileName. Delete it or move it before running this command, or specify the -Force argument to have this script replace it."
        }
    }
    if(Test-Path $PfxFileName) {
        if($Force) {
            del $PfxFileName
        } else {
            throw "There is already a cert at $PfxFileName. Delete it or move it before running this command, or specify the -Force argument to have this script replace it."
        }
    }

    $extraArgs = $null;
    if($IssuerName) {
        $extraArgs = @("-in", $IssuerName) # Signed by $IssuerName
    } else {
        $extraArgs = @("-r") # Self-signed
    }
    makecert -sky exchange -n $FullName -pe -a sha1 -len 2048 -ss My $FileName @extraArgs

    # Get the Thumbprint and find the private key in the store
    $FileName = (Convert-Path $FileName)
    Write-Host "Certificate ($FullName) created. Public Key is at $FileName"
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $FileName
    $CertificateThumbprint = $cert.Thumbprint

    $cert = get-item "cert:\CurrentUser\My\$CertificateThumbprint"
    $CertData = $cert.Export("Pkcs12", [String]::Empty);
    [IO.File]::WriteAllBytes($PfxFileName, $CertData)
}