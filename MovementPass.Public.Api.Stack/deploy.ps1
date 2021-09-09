$awsProfile = "movement-pass"
$app = "movement-pass"
$version = "v1"
$name = "public-api"
$location = "dist"

New-Item -Name $location -ItemType directory -Force

Set-Location -Path "../MovementPass.Public.Api"
Remove-Item "obj" -Recurse
Remove-Item "bin" -Recurse
dotnet lambda package -o "$($app)_$($name)_$($version).zip"
Set-Location -Path "../MovementPass.Public.Api.Stack"
Move-Item "../MovementPass.Public.Api/$($app)_$($name)_$($version).zip" $location -Force

Set-Location -Path "../MovementPass.Public.Api.BackgroundJob"
Remove-Item "obj" -Recurse
Remove-Item "bin" -Recurse
dotnet lambda package -o "$($app)_$($name)-background-job_$($version).zip"
Set-Location -Path "../MovementPass.Public.Api.Stack"
Move-Item "../MovementPass.Public.Api.BackgroundJob/$($app)_$($name)-background-job_$($version).zip" $location -Force

cdk deploy $app-passesstream-$version --require-approval never --profile ${awsProfile}
cdk deploy $app-publicapi-$version --require-approval never --profile ${awsProfile}
cdk deploy $app-backgroundjob-$version --require-approval never --profile ${awsProfile}
