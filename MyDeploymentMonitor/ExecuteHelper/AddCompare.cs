using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public class AddCompare : IComparer<string>
    {
        public int Compare([AllowNull] string x, [AllowNull] string y)
        {
            if(x==y)
            {
                return 0;
            }
            return 1;
        }
    }
}
