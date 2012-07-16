
pushd
cd .\bin\release\

$warm = 5
$cnt = 20
$s
for ($t = 0; $t -lt $warm; $t++) { $i = (Measure-Command { .\FxGqlTest.exe }).TotalMilliseconds; Write-Host $i }
Write-Host '------'
for ($t = 0; $t -lt $cnt; $t++) { $i = (Measure-Command { .\FxGqlTest.exe }).TotalMilliseconds; $s += $i; Write-Host $i }
Write-Host '------'
$s /= $cnt
Write-Host $s

popd
