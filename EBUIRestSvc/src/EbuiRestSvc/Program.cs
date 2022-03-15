using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EbuiRestSvc
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            Amazon.CDK.Environment setEnv(string account = null, string region = null)
            {
                return new Amazon.CDK.Environment
                {
                    Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                };
            }

            var ENV_NON_PROD = app.Node.TryGetContext("Environment");
            var STAGE_DEV = app.Node.TryGetContext("Stage");
            var DEV_ECRREPO_NAME = app.Node.TryGetContext("EcrRepo");
            var FEATURE_NAME = app.Node.TryGetContext("FeatureName");
            var BUILD_NUMBER = app.Node.TryGetContext("BuildNumber");
            //var DOMAIN = app.Node.TryGetContext("Domain");      //TODO: Uncomment
            var DEV_VPC_ID = app.Node.TryGetContext("VpcId");
            //var CERT_ID = app.Node.TryGetContext("CertId");     //TODO: Uncomment


            var dev = new EbuiRestSvcStack(app, $"EbuiRestSvcStack-{ENV_NON_PROD}-{STAGE_DEV}-{FEATURE_NAME}-{BUILD_NUMBER}", new AStackProps
            {
                Env = setEnv(),
                EnvironmentName = ENV_NON_PROD.ToString(),
                StageName = STAGE_DEV.ToString(),
                EcrRepo = DEV_ECRREPO_NAME.ToString(),
                FeatureName = FEATURE_NAME.ToString(),
                BuildNumber = BUILD_NUMBER.ToString(),
                //Domain = DOMAIN.ToString(),                //TODO: Uncomment
                VpcId = DEV_VPC_ID.ToString(),
                //CertId = CERT_ID.ToString()                //TODO: Uncomment

            });

            Tags.Of(dev).Add("Environment", $"{ENV_NON_PROD}");
            Tags.Of(dev).Add("StageName", $"{STAGE_DEV}");
            Tags.Of(dev).Add("CreatedBy", "AWSCDK");
            Tags.Of(dev).Add("ForApplication", "EventBroker");
            Tags.Of(dev).Add("Company", "FilmTrack");
            Tags.Of(dev).Add("aml-modernized", "filmtrack-eventbroker-01");

            app.Synth();
        }
    }
}
