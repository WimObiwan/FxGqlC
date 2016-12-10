$ErrorActionPreference = 'STOP'

# Tools
git clean -xfd ./fxgqlc
copy -Recurse -Force ./fxgqlc-orig/ ./fxgqlc
mkdir -p ./fxgqlc/opt/FxGqlC/
copy ../../FxGql/FxGqlC/bin/Release/* ./fxgqlc/opt/FxGqlC/
$fxgqlc = $(./fxgqlc/opt/FxGqlC/FxGqlC.exe)

$regex = '(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[A-Za-z0-9\-\.]+))?(?:\+(?<build>[A-Za-z0-9\-\.]+))?'
$regex2 = "^.*\s(v$regex)\s.*$"

$title = @($fxgqlc | ?{ $_ -match $regex2 } | select -First 1)[0]
$copyright = @($fxgqlc | ?{ $_ -match 'copyright' } | select -First 1)[0]
$version = $title -replace $regex2, '$1'

$deb_version = $version.TrimStart('v')

Write-Warning "Using version for nuget: $deb_version"

$fxgqlc_orig = $(gc ./fxgqlc/DEBIAN/control)
$fxgqlc_orig `
    -replace 'Version:.*', "Version:$deb_version" `
    | sc ./fxgqlc/DEBIAN/control

gc ./fxgqlc/DEBIAN/control

# Create package
fakeroot dpkg-deb -v --build fxgqlc
if ($LASTEXITCODE -ne 0) {
    Write-Error "dpkg-deb FAILED! ($LASTEXITCODE)"
}

move -Force fxgqlc.deb "fxgqlc-$deb_version.deb"
