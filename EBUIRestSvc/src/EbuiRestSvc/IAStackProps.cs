using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CDK;

namespace EbuiRestSvc
{
    internal interface IAStackProps: IStackProps
    {
        public string EnvironmentName { get; set; }

        public string StageName { get; set; }

        public string EcrRepo { get; set; }

        public string FeatureName { get; set; }

        public string BuildNumber { get; set; }

        public string Domain { get; set; }

        public string VpcId { get; set; }

        public string CertId { get; set; }
    }
}
