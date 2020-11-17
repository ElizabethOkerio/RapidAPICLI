using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Remote.Models
{
    public class RemoteDeployment
    {
        public RemoteProject Project { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public string DeploymentName { get; set; }
    }
}
