#!/usr/local/bin/pwsh
param(
)

$version = $env:BUILD_BUILDNUMBER;
$target = "";

Remove-Item -ErrorAction SilentlyContinue src/Sannel.House.Users/app_data/data.db
# Pull latest images 
docker pull microsoft/dotnet:2.2-aspnetcore-runtime
docker pull microsoft/dotnet:2.2-sdk

if($null -eq $version -or $version -eq "")
{
	$version = (Get-Date -Format yyMMdd) + "-local";
}

if($IsLinux -eq $true -or $IsMacOS -eq $true)
{
	Write-Host "Building on Linux"
	$uname = uname -m
	if($uname -eq "x86_64" -or $uname -eq "i386")
	{
		$uname = "x64";
	}
	if($uname -eq "armv7l" -or $uname -eq "unknown") # for now assume unknown is arm
	{
		$uname = "arm";
	}

	$env:USER="root"
	$env:SANNEL_ARCH="linux-$uname"
	$env:SANNEL_VERSION=$version
	return docker-compose -f docker-compose.yml -f docker-compose.unix.yml build $target
}
else
{
	Write-Host "Building on Windows"
	$env:USER="administrator"
	$env:SANNEL_ARCH="win"
	$env:SANNEL_VERSION=$version
	return docker-compose.exe -f docker-compose.yml -f docker-compose.windows.yml build $target
}
