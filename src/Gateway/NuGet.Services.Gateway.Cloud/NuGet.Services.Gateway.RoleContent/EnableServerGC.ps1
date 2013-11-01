if(!(Test-Path "env:\RoleRoot")) {
	throw "Not in an Azure Environment!"
}

# Load up the XML
$configFile = "${env:RoleRoot}\base\x64\WaWorkerHost.exe.config"
[xml]$waXML = Get-Content $configFile

if (($waXML.configuration.runtime.gcServer -eq $null) -and ($waXML.configuration.runtime.gcConcurrent -eq $null))
{
    # Modify XML
    $gcServerEl = $waXML.CreateElement('gcServer')
    $gcConcurrentrEl = $waXML.CreateElement('gcConcurrent')

    $gcServerAtt = $waXML.CreateAttribute("enabled")
    $gcServerAtt.Value = "true"
    $gcConcurrentrAtt = $waXML.CreateAttribute("enabled")
    $gcConcurrentrAtt.Value = "true"

    $gcServerEl.Attributes.Append($gcServerAtt) | Out-Null
    $gcConcurrentrEl.Attributes.Append($gcConcurrentrAtt) | Out-Null


    $waXML.configuration.runtime.appendChild($gcServerEl) | Out-Null
    $waXML.configuration.runtime.appendChild($gcConcurrentrEl) | Out-Null

    $waXML.Save($configFile)
    
    # Restart WaWorkerHost.Exe
    Get-Process | ? {$_.name -match "WaHostBootstrapper"} | Stop-Process -Force
    Get-Process | ? {$_.name -match "WaWorkerHost"} | Stop-Process -Force
}