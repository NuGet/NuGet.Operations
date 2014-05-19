function Install-WinRMCertificate
{
    param($instance)

	$certPath = "cert:\LocalMachine\Root\$($instance.VM.DefaultWinRmCertificateThumbprint)"
    if(!(Test-Path $certPath)) {
        # Install the Cert
        Write-Host "Installing the WinRM cert. Admin permission is required for this operation."
        $cert = Get-AzureCertificate -ServiceName $instance.ServiceName -Thumbprint $instance.VM.DefaultWinRmCertificateThumbprint -ThumbprintAlgorithm sha1

        $certTempFile = [IO.Path]::GetTempFileName()
        $cert.Data | Out-File $certTempFile

        $script = @'
# Target The Cert That Needs To Be Imported
$CertToImport = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 "!!CERTTEMPFILE!!"

$store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root", "LocalMachine"
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$store.Add($CertToImport)
$store.Close()
'@
        $script = $script.Replace("!!CERTTEMPFILE!!", $certTempFile)

        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
        Start-Process -FilePath "powershell" -ArgumentList @("-NoProfile", "-NoLogo" , "-EncodedCommand", $encoded) -Wait -Verb runas
        Remove-Item $certTempFile

        if(!(Test-Path $certPath)) {
            throw "Failed to install WinRM cert! Try again after manually installing..."
        }
    } else {
        Write-Host "WinRM certificate is already installed!"
    }
}