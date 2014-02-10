<#
.SYNOPSIS
Generates a deployment name
#>
function Get-DeploymentName {
    $commit = (git rev-parse --short HEAD)
    $branch = (git rev-parse --abbrev-ref HEAD)
    $date = [DateTime]::Now.ToString("yyyyMMMdd")
    $time = [DateTime]::Now.ToString("HHmm")
    "$date @ $time ($commit on $branch)"
}