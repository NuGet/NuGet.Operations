function Parse-StorageConnectionString {
    param($ConnectionString)
    $split = $ConnectionString.Split(";");
    $vals = @{};
    $split | ForEach-Object {
        $splat = $_.Split("=")
        $key = $splat[0]
        $value = [String]::Join("=", $splat[1..$splat.Length])
        $vals[$key] = $value
    }
    $vals
}