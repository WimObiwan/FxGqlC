scriptdir=`dirname $0`
cd "$scriptdir/../.."
pwd

git pull
git status --porcelain > /tmp/_fxgqlc_gitchanges.txt
if [[ -s /tmp/_fxgqlc_gitchanges.txt ]] ; then
	echo "There are pending changes. Build canceled."
	exit
fi;

mdtool build --target:Clean --configuration:Release FxGql/FxGql.sln
rm ./FxGql/FxGqlC/bin/Release/* --force

#INCREMENT BUILD NUMBER
#select (case when ($line match 'assembly\: AssemblyVersion\(\"\d+\.\d+\.\d+\.(\d+)\"\)') then matchregex($line, '^(.*\(\"\d+\.\d+\.\d+)\.\d+(\"\).*)$', '$1.' + (1 + convert(int, matchregex($line, 'assembly\: AssemblyVersion\(\"\d+\.\d+\.\d+\.(\d+)\"\)'))) + '$2') else $line end) into ['/tmp/_fxgqlc_AssemblyInfo.cs' -overwrite] from [FxGqlC/AssemblyInfo.cs]

# First build FxGqlC to have a working FxGqlC.exe
mdtool build --target:Build --configuration:Release FxGql/FxGql.sln

# Update version number
./FxGql/FxGqlC/bin/Release/FxGqlC.exe -gqlfile ./FxGql/BuildScript/IncrementBuildNumber.gql
mv /tmp/_fxgqlc_AssemblyInfo.cs ./FxGql/FxGqlC/AssemblyInfo.cs

#select matchregex($line, 'assembly\: AssemblyVersion\(\"(\d+\.\d+\.\d+\.\d+)\"\)') from [FxGqlC/AssemblyInfo.cs] where ($line match 'assembly\: AssemblyVersion\(\"\d+\.\d+\.\d+\.(\d+)\"\)')

mdtool build --target:Build --configuration:Release FxGql/FxGql.sln

./FxGql/FxGqlC/bin/Release/FxGqlC.exe > /tmp/_fxgqlc_versionoutput.txt
./FxGql/FxGqlC/bin/Release/FxGqlC.exe -c "select matchregex(\$line, '- (v.*) -') into ['/tmp/_fxgqlc_versionoutput2.txt' -overwrite] from ['/tmp/_fxgqlc_versionoutput.txt'] where \$line match '- (v.*) -'"

version=`cat /tmp/_fxgqlc_versionoutput2.txt`

zip /tmp/FxGqlC-$version.zip FxGql/FxGqlC/bin/Release/*

git add ./FxGql/FxGqlC/AssemblyInfo.cs
git commit -m "$version"
git push
echo "Increased version to $version"
git tag -a –f "$version"
git push --tags

echo $version
git describe master

