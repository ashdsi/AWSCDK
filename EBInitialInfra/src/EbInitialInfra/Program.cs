using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EbInitialInfra
{
    sealed class Program
    {
        /*=========Define the CDK environments===========*/
        //A CDK environment is the target AWS account and region into which the stack is intended to be deployed
        //Non Prod
        //private const string ENV_NON_PROD = "NONPROD";

        ////Prod
        ////private const string ENV_PROD = "PROD";
        ///*=========END===========*/

        ///*=========Define the CDK stages in the environment===========*/
        ////Non Prod
        //private const string STAGE_DEV = "DEV";
        //private const string DEV_VPC_NAME = "EbUI-vpc";
        //private const string STAGE_QA = "QA";
        //private const string STAGE_UAT = "UAT";

        //Prod
        //private const string STAGE_PROD = "PROD";
        //private const string STAGE_SANDBOX = "SANDBOX";
        /*=========END===========*/


        public static void Main(string[] args)
        {
            var app = new App();

            //The below  CDK environment varaibles are set based on the AWS profile specified using the --profile option in the AWS CDK CLI,
            //or the default AWS profile if you don't specify one.
            Amazon.CDK.Environment setEnv(string account=null, string region=null)
            {
                return new Amazon.CDK.Environment
                {
                    Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                };
            }

            var ENV_NON_PROD = app.Node.TryGetContext("Environment");
            var STAGE_DEV = app.Node.TryGetContext("Stage");
            var DEV_VPC_ID = app.Node.TryGetContext("VpcId");
            var DOMAIN = app.Node.TryGetContext("Domain");

            /*Setting the stages for NON-PROD AWS account*/
            /*=================BEGIN==============================*/
            //Development stage

            var dev = new EbInitialInfraStack(app, $"EbInitialInfraStack-{ENV_NON_PROD}-{STAGE_DEV}", new AStackProps
            {
                Env = setEnv(),
                EnvironmentName = ENV_NON_PROD.ToString(),
                StageName = STAGE_DEV.ToString(),
                VpcId = DEV_VPC_ID.ToString(),
                Domain = DOMAIN.ToString()

            });

            Tags.Of(dev).Add("Environment", $"{ENV_NON_PROD}");
            Tags.Of(dev).Add("StageName", $"{STAGE_DEV}");
            Tags.Of(dev).Add("CreatedBy", "AWSCDK");
            Tags.Of(dev).Add("ForApplication", "EventBroker");
            Tags.Of(dev).Add("Company", "FilmTrack");
            Tags.Of(dev).Add("aml-modernized", "filmtrack-eventbroker-01");


            /*Setting the stages for PROD AWS account*/
            /*=================BEGIN==============================*/
            //Prod stage
            //var prod = new EbInitialInfraStack(app, $"EbInitialInfraStack-{ENV_PROD}-{STAGE_PROD}", new AStackProps
            //{
            //    Env = setEnv(),
            //    EnvironmentName = ENV_PROD,
            //    StageName = STAGE_PROD

            //});

            app.Synth();
        }
    }
}
