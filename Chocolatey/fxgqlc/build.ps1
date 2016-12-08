$ErrorActionPreference = 'STOP'

# Tools
git clean -xfd .\Tools\
cp ..\..\FxGql\FxGqlC\bin\Release\* .\Tools
$fxgqlc = $(.\Tools\FxGqlC.exe)
$title = $fxgqlc | ?{ $_ -match '^.*\s(v\d+\.\d+\.\w+)\s.*$' } | select -First 1
$copyright = $fxgqlc | ?{ $_ -match 'copyright' } | select -First 1
$version = $title -replace '^.*\s(v\d+\.\d+\.\w+)\s.*$', '$1'

if ($version -match '^v\d+\.\d+\.\d+$') {
    # release
    Write-Error 'TO BE TESTED FIRST!'
    $nuget_version = $version
} elseif ($version -match '^v(\d+\.\d+)\.(\w+)$') {
    # pre-release
    $nuget_version = $Matches[1] + '.0-' + $Matches[2]
} else {
    # unknown format
    Write-Error "Version information invalid ($version)"
}

Write-Warning "Using version for nuget: $nuget_version"

$fxgqlc_orig = $(gc .\fxgqlc-orig.nuspec)
$fxgqlc_orig `
    -replace '<version>.*</version>', "<version>$([Security.SecurityElement]::Escape($nuget_version))</version>" `
    -replace '<copyright>.*</copyright>', "<copyright>$([Security.SecurityElement]::Escape($copyright))</copyright>" `
    | sc .\fxgqlc.nuspec

gc .\fxgqlc.nuspec

# Create package
choco pack .\fxgqlc.nuspec
if ($LASTEXITCODE -ne 0) {
    Write-Error "choco pack FAILED! ($LASTEXITCODE)"
}