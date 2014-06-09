$asms = @(
    "NuGet.Services.Operations.dll",
    "RazorEngine.dll",
    "Microsoft.IdentityModel.Clients.ActiveDirectory.dll"
)

function Load-Session {
    param($NuGetAppModel)

    # Try to load the assembly containing the types
    $OpsAsmRoot = Join-Path (Convert-Path "$PSScriptRoot\..\..\..\..\src\NuGet.Services.Operations") "bin\Debug"
    if(!(Test-Path "$OpsAsmRoot\NuGet.Services.Operations.dll")) {
        throw "Unable to load environments. NuGet.Services.Operations has not been built."
    }

    # Shadow-copy the assembly and dependencies
    $tmp = Join-Path ([IO.Path]::GetTempPath()) "NuOps"
    if(!(Test-Path $tmp)) {
        mkdir $tmp | Out-Null
    }
    $asms | ForEach-Object {
        $asm = Join-Path $tmp $_
        if(Test-Path $asm) {
            rm -for $asm
        }
        cp "$OpsAsmRoot\$_" $asm
    }

    Add-Type -Path "$tmp\NuGet.Services.Operations.dll"

    $session = [NuGet.Services.Operations.OperationsSession]::Load((Convert-Path $NuGetAppModel));
    $session
}