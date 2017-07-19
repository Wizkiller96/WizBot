using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility.Common.Exceptions
{
    public class StreamRoleNotFoundException : Exception
    {
        public StreamRoleNotFoundException() : base("Stream role wasn't found.")
        {
        }
    }
}