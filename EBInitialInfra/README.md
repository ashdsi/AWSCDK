# Welcome to your CDK C# project!

This is a blank project for C# development with CDK.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `dotnet build src` compile this app
* `cdk deploy`       deploy this stack to your default AWS account/region
* `cdk diff`         compare deployed stack with current state
* `cdk synth`        emits the synthesized CloudFormation template

## Prerequisites 

AWS CDK 
AWS credentials and AWS region as environment variabes or default profile 


cdk bootstrap aws://<AccountID>/<AWS_DEFAULT_REGION>      (Once per account/region pair)


* `cdk deploy -c Environment=nonprod -c Stage=dev -c VpcId=<VPC ID> -c Domain=<R53 hosted zone> --require-approval never`
* `cdk destroy -c Environment=nonprod -c Stage=dev -c VpcId=<VPC ID> -c Domain=<R53 hosted zone> --force`

