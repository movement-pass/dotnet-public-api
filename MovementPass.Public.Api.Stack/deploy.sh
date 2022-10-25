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

cdk deploy ${app}-publicapi-${version} --require-approval never --profile ${awsProfile}
