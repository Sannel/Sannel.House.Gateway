#!/usr/local/bin/pwsh
param(
)

$version = $env:BUILD_BUILDNUMBER;

if($null -eq $version -or $version -eq "")
{
	$version = (Get-Date -Format yyMMdd) + "-local";
}

if($null -ne $env:docker_user)
{
	docker login -u $env:docker_user -p $env:docker_password
}

if($IsLinux -eq $true -or $IsMacOS -eq $true)
{
	$uname = uname -m
	if($uname -eq "x86_64")
	{
		$uname = "x64";
	}
	if($uname -eq "armv7l") # for now assume unknown is arm
	{
		$uname = "arm";
	}

	$env:SANNEL_ARCH="linux-$uname"
	$env:SANNEL_VERSION=$version
	return docker-compose -f docker-compose.yml -f docker-compose.unix.yml push $target
}
else
{
	$env:SANNEL_ARCH="win"
	$env:SANNEL_VERSION=$version
	return docker-compose -f docker-compose.yml -f docker-compose.windows.yml push $target
}
