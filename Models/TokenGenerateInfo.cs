using System;
using System.ComponentModel.DataAnnotations;

namespace DeploymentRobotService.Models
{
    public class TokenGenerateInfo
    {
        [MaxLength(255)]
        [Required]
        public string UserIdentification { get; set; }

        [Range(1, 99999999, ErrorMessage = "Expire beyond the maximum")]
        public int Expire { get; set; }

        public string UserName
        {
            get
            {
                if(!string.IsNullOrEmpty(UserIdentification) && UserIdentification.Contains("-"))
                {
                    return UserIdentification.Substring(0, UserIdentification.IndexOf('-'));
                }
                else
                {
                    return null;
                }

            }
        }
    }
}
