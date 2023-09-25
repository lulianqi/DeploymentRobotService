using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsUserInfo
    {
        public string union_id { get; set; }
        public string user_id  { get; set; }
        public string open_id { get; set; }
        public string name { get; set; }
        public string en_name { get; set; }
        public string nickname { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
        public string avatar_key { get; set; }
        public string employee_no { get; set; }
        public string enterprise_email { get; set; }
        public UserStatus status { get; set; }
        public string leader_user_id { get; set; }
        public int employee_type { get; set; }
    }

    public class UserStatus
    {
        //是否暂停
        public bool is_frozen { get; set; }
        //是否离职
        public bool is_resigned { get; set; }
        //是否激活
        public bool is_activated { get; set; }
        //是否主动退出，主动退出一段时间后用户会自动转为已离职
        public bool is_exited { get; set; }
        //是否未加入，需要用户自主确认才能加入团队
        public bool is_unjoin { get; set; }
    }
}
