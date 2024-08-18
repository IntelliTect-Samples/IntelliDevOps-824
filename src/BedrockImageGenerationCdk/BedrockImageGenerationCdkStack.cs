using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using cloudFront = Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Constructs;
using System;
using System.Collections.Generic;
using Amazon.CDK.AWS.S3.Deployment;

namespace BedrockImageGenerationCdk
{
    public class BedrockImageGenerationCdkStack : Stack
    {
        public BedrockImageGenerationCdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            string environment = "DEV";

            var s3Bucket = new Bucket(this, "GenAIImages", new BucketProps
            {
                BucketName = (environment + "-GenAIImages" + Account).ToLower(),
                RemovalPolicy = RemovalPolicy.RETAIN
            });
            
            var websiteBucket = new Bucket(this, "WebsiteBucket", new BucketProps
            {
                BucketName = (environment + "-GenAIImages-Website" + Account).ToLower(),
                AccessControl = BucketAccessControl.PRIVATE,
            });

            _ = new BucketDeployment(this, "DeployWebsite", new BucketDeploymentProps
            {
                Sources = new[] { Source.Asset("./src/BedrockImageWeb/wwwroot") },
                DestinationBucket = websiteBucket,
            });

            var originAccessIdentity = new cloudFront.OriginAccessIdentity(this, "OriginAccessIdentity");
            websiteBucket.GrantRead(originAccessIdentity);

            var distribution = new cloudFront.CloudFrontWebDistribution(this, "MyDistribution", new cloudFront.CloudFrontWebDistributionProps
            {
                DefaultRootObject = "index.html",
                OriginConfigs = new[] { new cloudFront.SourceConfiguration
                {
                    S3OriginSource = new cloudFront.S3OriginConfig
                    {
                        S3BucketSource = websiteBucket,
                        OriginAccessIdentity = originAccessIdentity
                    },
                    Behaviors = new[] { new cloudFront.Behavior { IsDefaultBehavior = true } }
                } },
            });

            var stableDiffusionXLG1Handler = new Function(this, "StableDiffusionXLG1Handler", new FunctionProps
            {
                Runtime = new Runtime("dotnet8", RuntimeFamily.DOTNET_CORE), // To support DOTNET_8 runtime https://github.com/aws/aws-lambda-dotnet/issues/1611,
                FunctionName = "StableDiffusionXLG1Handler",
                //Where to get the code
                Code = Code.FromAsset("./src/TextToImageLambdaFunction/bin/Debug/net8.0"),
                Handler = "TextToImageLambdaFunction::TextToImageLambdaFunction.Function::StableDiffusionXLG1Handler",
                Environment = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = environment,
                    ["BUCKET"] = s3Bucket.BucketName
                },
                Timeout = Duration.Seconds(900)
            });

            // Assign permissions to Lambda to invoke Bedrock model
            string[] actions = {
                "bedrock:InvokeModel"
            };

            var policy = new PolicyStatement(new PolicyStatementProps()
            {
                Sid = "BedrockPermissionForLambda",
                Actions = actions,
                Effect = Effect.ALLOW,
                Resources = new string[] { "*" }
            });
            
            stableDiffusionXLG1Handler.AddToRolePolicy(policy);
            
            // assign put permission to stableDiffusionXLG1Handler lambda to write an image to S3 bucket and 
            // read permission so that generated presigned URL should not give access denied error
            s3Bucket.GrantReadWrite(stableDiffusionXLG1Handler);

            // create API in API Gateway
            var restAPI = new RestApi(this, "BedrockImageGenerationRestAPI", new RestApiProps
            {
                RestApiName = "BedrockImageGenerationRestAPI",
                Description = "This API provide endponts to interact with Bedrock for text eneration",
                Deploy = false,
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS,
                    AllowMethods = Cors.ALL_METHODS,
                    AllowHeaders = ["Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token"]
                }
            });

            var deployment = new Deployment(this, "My Deployment", new DeploymentProps { Api = restAPI });
            var stage = new Amazon.CDK.AWS.APIGateway.Stage(this, "stage name", new Amazon.CDK.AWS.APIGateway.StageProps
            {
                Deployment = deployment,
                StageName = environment,
                // enable tracing x-ray
                TracingEnabled = true,
            });

            restAPI.DeploymentStage = stage;

            var imageResource = restAPI.Root.AddResource("image");
            imageResource.AddMethod("POST", new LambdaIntegration(stableDiffusionXLG1Handler, new LambdaIntegrationOptions()
            {
                Proxy = true
            }));

            //Output results of the CDK Deployment
            _ = new CfnOutput(this, "Deployment start Time:", new CfnOutputProps() { Value = DateTime.Now.ToString() });
            _ = new CfnOutput(this, "Region:", new CfnOutputProps() { Value = this.Region });
            _ = new CfnOutput(this, "Amazon API Gateway Enpoint:", new CfnOutputProps() { Value = restAPI.Url });
            _ = new CfnOutput(this, "Amazon S3 Bucket Name:", new CfnOutputProps() { Value = s3Bucket.BucketName });
            _ = new CfnOutput(this, "Website URL:", new CfnOutputProps { Value = distribution.DistributionDomainName });
        }
    }
}
