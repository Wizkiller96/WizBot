using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility.Common.Exceptions
{
    public class StreamRolePermissionException : Exception
    {
        public StreamRolePermissionException() : base("Stream role was unable to be applied.")
        {
        }
    }
}