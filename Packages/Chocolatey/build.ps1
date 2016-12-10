$ErrorActionPreference = 'STOP'

# Tools
git clean -xfd .\fxgqlc\Tools\
cp ..\..\FxGql\FxGqlC\bin\Release\* .\fxgqlc\Tools
$fxgqlc = $(.\fxgqlc\Tools\FxGqlC.exe)

$regex = '(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[A-Za-z0-9\-\.]+))?(?:\+(?<build>[A-Za-z0-9\-\.]+))?'
$regex2 = "^.*\s(v$regex)\s.*$"

$title = $fxgqlc | ?{ $_ -match $regex2 } | select -First 1
$copyright = $fxgqlc | ?{ $_ -match 'copyright' } | select -First 1
$version = $title -replace $regex2, '$1'

$nuget_version = $version.TrimStart('v')

Write-Warning "Using version for nuget: $nuget_version"

$fxgqlc_orig = $(gc .\fxgqlc\fxgqlc-orig.nuspec)
$fxgqlc_orig `
    -replace '<version>.*</version>', "<version>$([Security.SecurityElement]::Escape($nuget_version))</version>" `
    -replace '<copyright>.*</copyright>', "<copyright>$([Security.SecurityElement]::Escape($copyright))</copyright>" `
    | sc .\fxgqlc\fxgqlc.nuspec

gc .\fxgqlc\fxgqlc.nuspec

# Create package
choco pack .\fxgqlc\fxgqlc.nuspec
if ($LASTEXITCODE -ne 0) {
    Write-Error "choco pack FAILED! ($LASTEXITCODE)"
}
