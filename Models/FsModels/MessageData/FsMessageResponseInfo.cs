using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsMessageResponseInfo
    {
        public string message_id { get; set; }
        public string root_id { get; set; }
        public string parent_id { get; set; }
        public string msg_type { get; set; }
        public string create_time { get; set; }
        public string update_time { get; set; }
        public bool deleted { get; set; }
        public bool updated { get; set; }
        public string chat_id { get; set; }
        public Sender sender { get; set; }
        public Body body { get; set; }
        public Mention[] mentions { get; set; }
        public string upper_message_id { get; set; }
    }

    public class Sender
    {
        public string id { get; set; }
        public string id_type { get; set; }
        public string sender_type { get; set; }
        public string tenant_key { get; set; }
    }

    public class Body
    {
        public string content { get; set; }
    }

    public class Mention
    {
        public string key { get; set; }
        public string id { get; set; }
        public string id_type { get; set; }
        public string name { get; set; }
        public string tenant_key { get; set; }
    }

}
