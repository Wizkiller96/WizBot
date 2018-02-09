using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Core.Modules.Searches.Common
{
    public class UrbanResponse
    {
        public UrbanDef[] List { get; set; }
    }
    public class UrbanDef
    {
        public string Word { get; set; }
        public string Definition { get; set; }
        public string Permalink { get; set; }
    }
}