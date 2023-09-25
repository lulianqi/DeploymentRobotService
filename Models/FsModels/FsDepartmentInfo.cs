using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsDepartmentInfo
    {
        public string department_id { get; set; }
        //部门的open_id
        public string open_department_id { get; set; }
        //部门群ID
        public string chat_id { get; set; }
        //部门名称
        public string name { get; set; }
        public int member_count { get; set; }
        public string parent_department_id { get; set; }
        public DepartmentStatus status { get; set; }
        public int order { get; set; }
    }

    public class DepartmentStatus
    {
        public bool is_deleted { get; set; }
    }
}
