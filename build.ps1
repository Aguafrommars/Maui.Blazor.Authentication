$result = 0
Get-ChildItem -rec `
| Where-Object { $_.Name -eq "TestResults" } `
| ForEach-Object {
	Remove-Item  $_.FullName -Recurse
}

Get-ChildItem -rec `
| Where-Object { $_.Name -eq "SonarQube.xml" } `
| ForEach-Object {
	Remove-Item  $_.FullName
}

$prNumber = $env:APPVEYOR_PULL_REQUEST_NUMBER
if ($prNumber) {
	$prArgs = "-d:sonar.pullrequest.key=$prNumber"
}
elseif ($env:APPVEYOR_REPO_BRANCH) {
	$prArgs = "-d:sonar.branch.name=$env:APPVEYOR_REPO_BRANCH"
}

if ($env:CI -And ((-Not $env:APPVEYOR_PULL_REQUEST_NUMBER) -Or ($env:APPVEYOR_PULL_REQUEST_HEAD_REPO_NAME -eq $env:APPVEYOR_REPO_NAME))) {
	Write-Host "dotnet sonarscanner begin /k:Aguafrommars_Maui.Blazor.Authentication -o:aguacongas -d:sonar.host.url=https://sonarcloud.io -d:sonar.login=****** -d:sonar.coverageReportPaths=coverage\SonarQube.xml $prArgs -v:$env:Version"
	dotnet sonarscanner begin /k:Aguafrommars_Maui.Blazor.Authentication -o:aguafrommars -d:sonar.host.url=https://sonarcloud.io -d:sonar.login=$env:sonarqube -d:sonar.coverageReportPaths=coverage\SonarQube.xml $prArgs -v:$env:Version
}

Write-Host "dotnet build src\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.csproj -t:InstallAndroidDependencies -f net10.0-android ""-p:AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk"" -p:AcceptAndroidSDKLicenses=true"
dotnet build src\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.csproj -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk" -p:AcceptAndroidSDKLicenses=true

Write-Host "dotnet test test\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.csproj -c Release --settings coverletArgs.runsettings  -v q"

dotnet test test\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.csproj -c Release --collect:"XPlat Code Coverage" --settings coverletArgs.runsettings -v q 

if ($LASTEXITCODE -ne 0) {
	$result = $LASTEXITCODE
}

$merge = ""
Get-ChildItem -rec `
| Where-Object { $_.Name -eq "coverage.cobertura.xml" } `
| ForEach-Object { 
	$path = $_.FullName
	$merge = "$path;$merge"
}
Write-Host $merge
ReportGenerator\tools\net9.0\ReportGenerator.exe "-reports:$merge" "-targetdir:coverage" "-reporttypes:SonarQube"
	
if ($env:CI -And ((-Not $env:APPVEYOR_PULL_REQUEST_NUMBER) -Or ($env:APPVEYOR_PULL_REQUEST_HEAD_REPO_NAME -eq $env:APPVEYOR_REPO_NAME))) {
	dotnet sonarscanner end -d:sonar.login=$env:sonarqube
}
exit $result
