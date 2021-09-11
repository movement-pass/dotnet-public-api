#!/usr/bin/env sh

awsProfile="movement-pass"
app="movement-pass"
version="v1"
name="public-api"
location="dist"

rm -rf ${location}
mkdir ${location}

cd ../MovementPass.Public.Api || exit
rm -rf obj
rm -rf bin
dotnet lambda package -o ${app}_${name}_${version}.zip
cd ../MovementPass.Public.Api.Stack || exit
mv ../MovementPass.Public.Api/${app}_${name}_${version}.zip ${location}/

cd ../MovementPass.Public.Api.BackgroundJob || exit
rm -rf obj
rm -rf bin
dotnet lambda package -o ${app}_${name}-background-job_${version}.zip
cd ../MovementPass.Public.Api.Stack || exit
mv ../MovementPass.Public.Api.BackgroundJob/${app}_${name}-background-job_${version}.zip ${location}/

cdk deploy ${app}-passesloadqueue-${version} --require-approval never --profile ${awsProfile}
cdk deploy ${app}-publicapi-${version} --require-approval never --profile ${awsProfile}
cdk deploy ${app}-backgroundjob-${version} --require-approval never --profile ${awsProfile}
