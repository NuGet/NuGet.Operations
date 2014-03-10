function exec($cmd) {
    Write-Host "$cmd $args" -ForegroundColor Magenta
    & $cmd @args
}

# Fetch the latest
Write-Host "Fetching the latest from the origin..."
exec git fetch origin --tags
exec git fetch origin

# Check for the previous release
Write-Host "Identifying the previous release"
$releaseBranches = @(exec git branch -r --column=never --color=never --list origin/releases/* | foreach { $_.Trim() })
$latestRelease = $releaseBranches | 
    where { $_ -match "^origin/releases/(?<ver>.*)$" } | 
    foreach { New-Object System.Version $matches["ver"] } |
    sort -desc |
    foreach { $_ } |
    select -first 1

$nextRelease = $null
if(!$latestRelease) {
    Write-Host "Unable to identify the previous release. Skipping tagging"
} else {
    $latestName = "releases/$($latestRelease.ToString())"
    $tag = "v$latestRelease"
    if((exec git tag) -contains $tag) {
        Write-Host "Tag $tag already exists, skipping tagging."
    }
    else {
        Write-Host "Tagging the end of release $latestRelease."
        exec git checkout "releases/$latestRelease"
        exec git rebase
        exec git tag $tag
        exec git push origin $tag
    }
    $nextRelease = New-Object System.Version @($latestRelease.Major, $latestRelease.Minor, ($latestRelease.Build + 1))
}

if(!$nextRelease) {
    Write-Host "Unable to guess the next release. Enter the version of the next release"
    $ver = Read-Host "For example (3.0.1)"
    $nextRelease = New-Object System.Version $ver
}

Write-Host "Updating master"
exec git checkout master
exec git rebase

Write-Host "Creating release branch"
$branch = "releases/$($nextRelease.ToString())"
exec git checkout -b $branch
exec git push origin $branch

if($latestRelease) {
    $completed = "Iteration $($latestRelease.ToString()) Released. "
}
Write-Host "$($completed)Moved Iteration $($nextRelease.ToString()) to testing."