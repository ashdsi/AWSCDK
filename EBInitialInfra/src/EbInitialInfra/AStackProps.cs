using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CDK;


namespace EbInitialInfra
{
    internal class AStackProps : StackProps , IAStackProps
    {
        public string EnvironmentName { get; set; }
        public string StageName { get; set; }

        public string VpcId { get; set; }

        public string Domain { get; set; }
    }
}
