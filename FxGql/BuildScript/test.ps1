pwd

pushd .

cd ".\FxGql\FxGqlTest\bin\Release"

.\FxGqlTest.exe

if ($LASTEXITCODE -ne 0) {
  throw "Tests failed."
}

popd
