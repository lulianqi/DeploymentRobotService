using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models
{
    public class BuildKubesInfo
    {
        [MaxLength(255)]
        [Required]
        public string devop { get; set; }

        [MaxLength(255)]
        [Required]
        public string pipeline { get; set; }

        [MaxLength(255)]
        public string workloads { get; set; }

        [MaxLength(255)]
        public string workloadfix { get; set; }

        [MaxLength(255)]
        public string wxrobot { get; set; }

        [MaxLength(255)]
        public string fsrobot { get; set; }

        [MaxLength(255)]
        public string fschatid { get; set; }

        public bool isPushStartMessage { get; set; }

        public Dictionary<string, string> configs { get; set; }

    }
}
