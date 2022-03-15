using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.Route53;
using Constructs;


namespace EbInitialInfra
{
    public class EbInitialInfraStack : Stack
    {
        internal EbInitialInfraStack(Construct scope, string id, IAStackProps props = null) : base(scope, id, props)
        {
            //string envName = props.EnvironmentName;
            string stageName = props.StageName;
            string appNameUI = "ebuirestsvc";


            //Reference existing VPC for DEV environment to be used for Event Broker
            var vpc = Vpc.FromLookup(this, "EbUIVpc", new VpcLookupOptions
            {
               VpcId = props.VpcId
               //VpcName = props.VpcName // Default is all AZs in region
            });

            //Create ECS cluster
            var cluster = new Cluster(this, "EbUIEcs", new ClusterProps
            {
                
                ClusterName = $"{appNameUI}-ecs-{stageName}",
                Vpc = vpc
            });


            //Create ECS Task Role and attach inline policy to DynamoDB
            var ecsTaskRole = new Role(this, "EbUIEcsTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                RoleName = $"{appNameUI}-taskrole-{stageName}-{this.Region}",    //Region is required since the role is common to a stage in a region
                Description = "ECS Task Role for Event Broker UI Rest Svc"
            });


            ecsTaskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "ssm:GetParametersByPath", "ssm:GetParameters", "ssm:GetParameter", "ssm:GetParameterHistory" },
                Resources = new[]
                {
                    Arn.Format(new ArnComponents
                    {
                        Service = "ssm",
                        Resource = "parameter",
                        ResourceName = "EventBroker/*"      //TODO: Verify parameter starts with this name
                    }, this)
                }
            }));

            //TODO: Add another Tenant role and add sts:AssumeRole for that role in ECSTaskRole

            //Create ECR Repository
            //Note:ECR repository name should be lower case
            var ecrRepo = new Repository(this, "EbUIEcrRepo", new RepositoryProps {
                RepositoryName = $"{appNameUI}-ecr-{stageName}",
                ImageScanOnPush = true,
                RemovalPolicy = RemovalPolicy.DESTROY              //Default: Retain.Removes ECR repo when stack is deleted or this resource undergoes update
            });


            ////Create a new Security Group for ALB and add ingress rule
            //var albSg = new SecurityGroup(this, "EbUIAlbSg", new SecurityGroupProps
            //{
            //    SecurityGroupName = $"{appNameUI}-albsg-{stageName}",
            //    Vpc = vpc,
            //    AllowAllOutbound = true

            //});

            ////Create a new Security Group for ECS task and add ingress rule
            //var ecsTaskSg = new SecurityGroup(this, "EbUIEcsSg", new SecurityGroupProps
            //{
            //    SecurityGroupName = $"{appNameUI}-ecssg-{stageName}",
            //    Vpc = vpc,
            //    AllowAllOutbound = true

            //});

            //albSg.Connections.AllowFrom(Connections_ Port.Tcp(80), "Allow inbound on port 80 from internet");

            //Add ALB Ingress rule
            //albSg.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow inbound on Port 80");

            //Add ECS Ingress rule
            //ecsTaskSg.Connections.AllowFrom(albSg, Port.AllTraffic(), "From ALB");


            //Create ALB 
            var alb = new ApplicationLoadBalancer(this, "EbUIAlb", new Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancerProps
            {
                LoadBalancerName = $"{appNameUI}-alb-{stageName}",
                Vpc = vpc,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                },
                InternetFacing = true,
                //SecurityGroup = albSg
            });

            //Create ALB listener
            var listener = alb.AddListener("EbUIAlbListener", new BaseApplicationListenerProps
            {
                Port = 80,      //TODO: Change to HTTPS  //This automatically creates a security group and allows the port
                DefaultAction = ListenerAction.FixedResponse(200, new FixedResponseOptions 
                { MessageBody = "Page Not Found" })
                //Certificates = new IListenerCertificate[] { ListenerCertificate.FromArn(cert.CertificateArn) } TODO: Add cert
                
            });

            //
            //listener.AddAction("default", new AddApplicationActionProps
            //{
            //    Action = ListenerAction.FixedResponse(404, new FixedResponseOptions
            //    {
            //        ContentType = "application/json",    
            //        MessageBody = "Page Not Found",

            //    }),
            //    Priority = 10,
            //    Conditions = new[] { ListenerCondition.PathPatterns(new[] { "/" }) }

            var ssmAlbArn = new StringParameter(this, "ssmAlbArn", new StringParameterProps
            {
                Description = "ARN of ALB Load Balancer for Event Broker Dev",
                ParameterName = $"EBALB{props.StageName}",
                StringValue = alb.LoadBalancerArn
            });

            var listenerArn = new StringParameter(this, "ssmListenerArn", new StringParameterProps
            {
                Description = "ARN of ALB Load Balancer Listener for Event Broker Dev",
                ParameterName = $"EBLISTENER{props.StageName}",
                StringValue = listener.ListenerArn
            });

            //CreateR53 hosted zone 
            //TODO: change to Public hosted zone
            var zone = new PrivateHostedZone(this, "HostedZone", new PrivateHostedZoneProps
            {
                ZoneName = props.Domain,
                Vpc = vpc
            });


            //}); 

            //Get the list of Security Groups automatically attached to ALB
            string[] albsg = alb.LoadBalancerSecurityGroups;
            

            new CfnOutput(this, "albArn", new CfnOutputProps
            {
                Value = alb.LoadBalancerArn,
                Description = "ARN of ALB for Event Broker Dev",
                ExportName = $"EBALBARN{props.StageName}"
            });

            new CfnOutput(this, "albName", new CfnOutputProps
            {
                Value = alb.LoadBalancerDnsName,
                Description = "Name of ALB for Event Broker Dev",
                ExportName = $"EBALBNAME{props.StageName}"
            });

            new CfnOutput(this, "albSg", new CfnOutputProps
            {
                Value =  Fn.Select(0, albsg),
                Description = "Security Group of ALB for Event Broker Dev",
                ExportName = $"EBALBSG{props.StageName}"
            });

            new CfnOutput(this, "albCanonicalId", new CfnOutputProps
            {
                Value = alb.LoadBalancerCanonicalHostedZoneId,
                Description = "Canonical Hosted Zone Id of the ALB for Event Broker Dev",
                ExportName = $"EBALBZONEID{props.StageName}"
            });

            new CfnOutput(this, "listenerArn", new CfnOutputProps
            {
                Value = listener.ListenerArn,
                Description = "ARN of ALB Listener for Event Broker Dev",
                ExportName = $"EBALBLISTENER{props.StageName}"
            });

            new CfnOutput(this, "hostedZone", new CfnOutputProps
            {
                Value = zone.ZoneName,
                Description = "Name of the R53 hosted zone",
                ExportName = $"EBZONE{props.StageName}"
            });

            new CfnOutput(this, "hostedZoneId", new CfnOutputProps
            {
                Value = zone.HostedZoneId,
                Description = "HostedZone Id of the R53 hosted zone",
                ExportName = $"EBZONEID{props.StageName}"
            });

        }
    }
}
