if ($env:APPVEYOR -and $env:APPVEYOR_PULL_REQUEST_NUMBER) {
  exit 0
}

$fileversion = "$env:SemVer.0"
$path = (Get-Location).Path

dotnet pack src\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc\Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.csproj -c Release -o $path\artifacts\build -p:Version=$env:Version -p:FileVersion=$fileversion
