function Get-SecretCredential {
    param($SecretName, $UserName = "nuget")

    $dataLine = & nucmd -data secrets get -k $SecretName
    if(!$dataLine -or !$dataLine.StartsWith("data : ")) {
        throw "No credential found in secret store"
    }
    $pass = ConvertTo-SecureString $dataLine.Substring("data : ".Length) -AsPlainText -Force
    
    New-Object System.Management.Automation.PSCredential $UserName, $pass
}