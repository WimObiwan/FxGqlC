$ErrorActionPreference = 'STOP'

# Tools
git clean -xfd ./FxGqlC
mkdir -p ./FxGqlC            
copy ../../FxGql/FxGqlC/bin/Release/* ./FxGqlC

$fxgqlc = $(./FxGqlC/FxGqlC.exe)

$regex = '(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[A-Za-z0-9\-\.]+))?(?:\+(?<build>[A-Za-z0-9\-\.]+))?'
$regex2 = "^.*\s(v$regex)\s.*$"

$title = @($fxgqlc | ?{ $_ -match $regex2 } | select -First 1)[0]
$copyright = @($fxgqlc | ?{ $_ -match 'copyright' } | select -First 1)[0]
$version = $title -replace $regex2, '$1'

$version = $version.TrimStart('v')

Write-Warning "Using version: $version"

zip -r "fxgqlc-$version.zip" ./FxGqlC
tar -czf "fxgqlc-$version.tar.gz" FxGqlC
