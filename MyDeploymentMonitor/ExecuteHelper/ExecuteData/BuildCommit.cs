using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public class BuildCommit
    {
        public class CommitContentCache
        {
            public int LastCommitCount { get; set; } = 0;
            public string ContentString { get; set; } = null;
        }

        public List<KeyValuePair<string, string>> CommitList { get; set; }
        public string Branch { get; set; }

        public CommitContentCache ShowCommitContentCache { get; set; } = new CommitContentCache();

        public string GetCommitStr(bool isMarkdown = false)
        {
            if(CommitList?.Count>0)
            {
                StringBuilder sbCommit = new StringBuilder();
                foreach (var commit in CommitList)
                {
                    sbCommit.Append(string.Format(isMarkdown ? "●**{0}** > " : "●{0} > ", commit.Key));
                    sbCommit.AppendLine(commit.Value);
                }
                //移除结尾newline
                if (CommitList?.Count > 0)
                {
                    sbCommit.Remove(sbCommit.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                }
                return sbCommit.Length > 0 ? sbCommit.ToString() : null;
            }
            return null;
        }

        public List<string> GetCommitUsers()
        {
            List<string> users = new List<string>();
            if (CommitList?.Count > 0)
            {
                foreach (var commit in CommitList)
                {
                    if (!users.Contains(commit.Key))
                    {
                        users.Add(commit.Key);
                    }
                }
            }
            return users;
        }

        
    }

}
