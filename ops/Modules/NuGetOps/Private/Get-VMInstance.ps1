function Get-VMInstance {
     param(
        [Parameter(Mandatory=$true, Position=0)][string]$ServiceName,
        [Parameter(Mandatory=$false, Position=1)][string]$MachineName)

    $vm = @(Get-AzureVM $ServiceName)

    $instance = $vm[0];
    if($vm.Length -gt 1) {
        if(!$MachineName) {
            throw "Multiple instances found! Specify one of the instances in the MachineName parameter."
        }
        $candidates = @($vm | Where { $_.Name -eq $MachineName });
        if($candidates.Length -eq 0) {
            throw "No instance $MachineName found in the $ServiceName service"
        } elseif($candidates.Length -gt 1) {
            throw "Multiple instances $MachineName found in the $ServiceName service"
        }
        $instance = $candidates[0];
    }
    $instance;
}