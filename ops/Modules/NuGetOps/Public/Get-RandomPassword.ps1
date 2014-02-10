<#
.SYNOPSIS
Returns a random, timestamped, password
#>
function Get-RandomPassword {
    param([Parameter(Mandatory=$false)][switch]$NoTimestamp)
    # Base64-encode the Guid to add some additional characters
    $basePass = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([Guid]::NewGuid().ToString()))
    if($NoTimestamp) {
        $basePass
    } else {
        [DateTime]::Now.ToString("MMMddyy") + "!" + $basePass
    }
}