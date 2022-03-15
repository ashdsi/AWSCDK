using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using System.Collections.Generic;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.CertificateManager;
using System;

namespace EbuiRestSvc
{
    public class EbuiRestSvcStack : Stack
    {
        internal EbuiRestSvcStack(Construct scope, string id, IAStackProps props = null) : base(scope, id, props)
        {

            string appNameUI = "ebuirestsvc";

            //Import hosted zone
            var hostedZone = Fn.ImportValue($"EBZONE{props.StageName}");
            var hostedZoneId = Fn.ImportValue($"EBZONEID{props.StageName}");
            
            //Import ALB name
            //var albName = Fn.ImportValue($"EBALBNAME{props.StageName}");

            // Import R53 hosted zone
            var zone = HostedZone.FromHostedZoneAttributes(this, "EbUIHostedZone", new HostedZoneAttributes
            {
                ZoneName = hostedZone,
                HostedZoneId = hostedZoneId
            });

            

        //Import vpc
        var vpc = Vpc.FromLookup(this, "EbUIVpc", new VpcLookupOptions
            {
                VpcId = props.VpcId
            });

            // Import ECS Cluster
            /**
            * Import an existing cluster to the stack from the cluster ARN.
            * This does not provide access to the vpc, hasEc2Capacity, or connections -
            * use the `fromClusterAttributes` method to access those properties.
            */
            var cluster = Cluster.FromClusterAttributes(this, "EbUIEcsCluster", new ClusterAttributes
            { 
                //All Required properties
                ClusterName = $"{appNameUI}-ecs-{props.StageName}",
                Vpc = vpc,                            
                SecurityGroups = new ISecurityGroup[] { }          //Required property, But not needed for Fargate. So creating empty array
            });


            // Import ECS Task Security Group
            //var ecssg = SecurityGroup.FromLookupByName(this, "EbUIEcsSg", $"{appNameUI}-ecssg-{props.StageName}", vpc);

            // Import ALB Security Group
            //var albsg = SecurityGroup.FromLookupByName(this, "EbUIAlbSg", $"{appNameUI}-albsg-{props.StageName}", vpc);

            //Import ALB security group and listener ARN by using CfnImport
            var albArn = Fn.ImportValue($"EBALBARN{props.StageName}");
            var albzoneid = Fn.ImportValue($"EBALBZONEID{props.StageName}");
            var albname = Fn.ImportValue($"EBALBNAME{props.StageName}");
            var listenerArn = Fn.ImportValue($"EBALBLISTENER{props.StageName}");
            var albsg = Fn.ImportValue($"EBALBSG{props.StageName}");




            //string listenerArn = StringParameter.FromStringParameterAttributes(this, "ssmListenerArn", new StringParameterAttributes
            //{
            //    ParameterName = "EBLISTENERdev"
            //}).StringValue;



            //Fetch ALB security group and don't allow CDK to mutate it in this stack
            ISecurityGroup securityGroup = SecurityGroup.FromSecurityGroupId(this, "sg", albsg, new SecurityGroupImportOptions
            {
                Mutable = false
            });

            //Lookup alb by using arn

            var alb = ApplicationLoadBalancer.FromApplicationLoadBalancerAttributes(this, "EbUIAlb", new ApplicationLoadBalancerAttributes
            {
                LoadBalancerArn = albArn,
                SecurityGroupId = securityGroup.SecurityGroupId,
                LoadBalancerCanonicalHostedZoneId = albzoneid,
                LoadBalancerDnsName = albname

            });


            // Filter Listener using Load Balancer ARN
            var listener = ApplicationListener.FromApplicationListenerAttributes(this, "EbUIAlbListener", new ApplicationListenerAttributes
            {
                ListenerArn = listenerArn,
                SecurityGroup = securityGroup 

            });

            //Import ACM certificate 
            //TODO: Add cert
            //var cert = Certificate.FromCertificateArn(this, "EbUIAlbcert", $"arn:aws:acm:{this.Region}:{this.Account}:certificate/{props.CertId}");


            // Create Task Definition
            var taskDef = new FargateTaskDefinition(this, "EbUIEcsTaskDef", new FargateTaskDefinitionProps
            {
                MemoryLimitMiB = 512,      //Default
                Cpu = 256,                 //Default
                RuntimePlatform = new RuntimePlatform
                {
                    OperatingSystemFamily = OperatingSystemFamily.LINUX
                    
                },
                TaskRole = Role.FromRoleArn(this, "importedTaskRole", $"arn:aws:iam::{this.Account}:role/{appNameUI}-taskrole-{props.StageName}-{this.Region}")

            });

            //Fetch ECR repository
            var repository = Repository.FromRepositoryName(this, "EbUIEcr", $"{appNameUI}-ecr-{props.StageName}");

            var container = taskDef.AddContainer("EbUIEcsContainer", new ContainerDefinitionOptions
            {
                //Image = ContainerImage.FromRegistry("public.ecr.aws/nginx/nginx:latest-arm64v8"),
                Logging = new AwsLogDriver(new AwsLogDriverProps {
                    StreamPrefix = $"{props.FeatureName}", 
                    Mode = AwsLogDriverMode.NON_BLOCKING,
                    LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_DAY      //Log Retention can be set as required
                    
                }),
                Image = RepositoryImage.FromEcrRepository(repository, $"{props.FeatureName}-build{props.BuildNumber}"),
                         //TODO: Add container image from ECR
            });

            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 80

            });

            // Instantiate an Amazon ECS Service
            var ecsService = new FargateService(this, "EbUIEcsService", new FargateServiceProps
            {
                ServiceName = $"{props.FeatureName}",
                Cluster = cluster,
                TaskDefinition = taskDef,
                DesiredCount = 1
                //CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true } //For automatic rollback of failed tasks in a service
            });

            // Create a Target Group
            var targetGrouphttp = new ApplicationTargetGroup(this, "EbUITargetGroup", new ApplicationTargetGroupProps
            {
                TargetGroupName = $"{props.StageName}-{props.FeatureName}",
                TargetType = TargetType.IP,
                Protocol = ApplicationProtocol.HTTP,      //TODO: Change to HTTPS ?
                Port = 80,                                 //TODO: Change to HTTPS 
                Vpc = vpc,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/events/health",
                    Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP
                }
            });


            ecsService.AttachToApplicationTargetGroup(targetGrouphttp);

            //Currently using a random function 
            //TODO: This can be changed in future avoid random number conflicts and increment priority from last known priority for that ALB listener rule
            double random_number = new Random().Next(1, 50000);

            //Create listener rules
            var rule = new ApplicationListenerRule(this, "rule", new ApplicationListenerRuleProps
            {
                Listener = listener,
                Conditions = new[] {  ListenerCondition.HostHeaders(new[] { $"{props.FeatureName}.{zone.ZoneName}" })  },
                Priority = random_number,
                TargetGroups = new[] { targetGrouphttp }

            });

            //Create R53 Alias record
       

            var record = new 
                ARecord(this, "record", new ARecordProps
            {
                Zone = zone,
                RecordName = props.FeatureName,
                Target = RecordTarget.FromAlias(new LoadBalancerTarget(alb)),
              

            });




        }

    }
    
}

