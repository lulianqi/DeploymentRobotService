using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public interface IRobotConnector
    {
        public string AppChannel { get; }
        public int MaxMessageLeng { get; }
        public Task<bool> PushContent(string toUser, string yourContent);
        public string AddActionForGetProjectResult(string Projects);
        public string GetOauthRedirectUrl(string state = null);

    }
}
