param()

$version = $env:BUILD_BUILDNUMBER;

if($null -eq $version -or $version -eq "")
{
	$version = (Get-Date -Format yyMMdd) + "-local";
}

$combinedTag="${env:DOCKER_REGISTRY}sannelhousegateway:beta-$version"
$arm="${env:DOCKER_REGISTRY}sannelhousegateway:build-$version-linux-arm"
$x64="${env:DOCKER_REGISTRY}sannelhousegateway:build-$version-linux-x64"
$win="${env:DOCKER_REGISTRY}sannelhousegateway:build-$version-win"

docker manifest create $combinedTag $arm $x64 $win
docker manifest push $combinedTag

$combinedTag="${env:DOCKER_REGISTRY}sannelhousegateway:beta"
docker manifest create $combinedTag $arm $x64 $win
docker manifest push $combinedTag
