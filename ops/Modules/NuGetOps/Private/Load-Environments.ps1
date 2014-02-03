param($NuGetInternalRoot)

function Get-Subscriptions($NuGetOpsDefinition) {
    $Subscriptions = @{};
    $SubscriptionsList = Join-Path $NuGetOpsDefinition "Subscriptions.xml"
    if(Test-Path $SubscriptionsList) {
        $x = [xml](cat $SubscriptionsList)
        $x.subscriptions.subscription | ForEach-Object {
            # Get the subscription object
            $sub = $null;
            if($accounts.Length -gt 0) {
                $sub = Get-AzureSubscription $_.name
            }
            if($sub -eq $null) {
                Write-Warning "Could not find subscription $_ in Subscriptions.xml. Do you have access to it?"
            }

            $Subscriptions[$_.name] = New-Object PSCustomObject
            Add-Member -NotePropertyMembers @{
                Version = $NuGetOpsVersion;
                Id = $_.id;
                Name = $_.name;
                Subscription = $sub;
            } -InputObject $Subscriptions[$_.name]
        }
    } else {
        Write-Warning "Subscriptions list not found at $SubscriptionsList. No Subscriptions will be available."
    }

    $Subscriptions
}

function Get-Environments($NuGetOpsDefinition) {
    $Environments = @{};
    $Subscriptions = $null;

    $EnvironmentsList = Join-Path $NuGetOpsDefinition "Environments.xml"
    if(!(Test-Path $EnvironmentsList)) {
        return
    }

    if($NuGetOpsDefinition -and (Test-Path $NuGetOpsDefinition)) {
        $Subscriptions = Get-Subscriptions -NuGetOpsDefinition $NuGetOpsDefinition

        $x = [xml](cat $EnvironmentsList);
        $x.environments.environment | ForEach-Object {
            $env = New-Object PSCustomObject
            $sub = $Subscriptions[$_.subscription]
            Add-Member -NotePropertyMembers @{
                Version = $_.version;
                Name = $_.name;
                Subscription = $sub;
                Protected = $_.protected -and ([String]::Equals($_.protected, "true", "OrdinalIgnoreCase"));
                Datacenters = $_.datacenter | ForEach-Object {
                    $dc = New-Object PSCustomObject
                    Add-Member -NotePropertyMembers @{
                        Id = $_.id;
                        Region = $_.region;
                        AffinityGroup = $_.affinityGroup;
                        Resources = $_.resources.SelectNodes("*") | ForEach-Object {
                            $res = New-Object PSCustomObject
                            Add-Member -NotePropertyMembers @{
                                Type = $_.LocalName;
                                Name = $_.name;
                                Value = $_."#text";
                            } -InputObject $res
                            $res
                        };
                        Services = $_.services.SelectNodes("*") | ForEach-Object {
                            $svc = New-Object PSCustomObject
                            Add-Member -NotePropertyMembers @{
                                Type = $_.LocalName;
                                Name = $_.name;
                                Value = $_."#text";
                                Url = New-Object System.Uri $_.url;
                            } -InputObject $svc
                            $svc
                        };
                    } -InputObject $dc
                    $dc
                }
            } -InputObject $env
            $Environments[$_.name] = $env
        }
    }

    $ret = New-Object PSCustomObject
    Add-Member -InputObject $ret -NotePropertyMembers @{
        "Version"=3;
        "Environments"=$Environments;
        "Subscriptions"=$Subscriptions
    }
    $ret
}

Get-Environments $NuGetInternalRoot