using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsItemData<T>
    {
        public bool has_more { get; set; }
        public List<T> items { get; set; }
        public string page_token { get; set; }
    }
}
