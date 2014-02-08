function Confirm() {
    param($Message, [bool]$Default = $true)

    $choices = "[Y/n]"
    if(!$Default) {
        $choices = "[y/N]"
    }

    $val = $null
    do {
        $str = Read-Host "$Message $choices"
        if([String]::IsNullOrWhitespace($str)) {
            $val = $Default
        } elseif([String]::Equals($str, "y", "OrdinalIgnoreCase") -or [String]::Equals($str, "yes", "OrdinalIgnoreCase")) {
            $val = $true
        } elseif([String]::Equals($str, "n", "OrdinalIgnoreCase") -or [String]::Equals($str, "no", "OrdinalIgnoreCase")) {
            $val = $false
        } else {
            $val = $null
        }
    } while($val -eq $null)
    $val
}