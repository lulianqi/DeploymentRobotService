using System;
using System.ComponentModel.DataAnnotations;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsSendInterativeInfo
    {
        [MaxLength(255)]
        public string uuid { get; set; }

        [MaxLength(255)]
        [Required]
        public string receive_id { get; set; }

        [Required]
        public string content { get; set; }

        public string[] urgent_users { get; set; }
    }
}
