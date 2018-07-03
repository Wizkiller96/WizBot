using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Games.Common.ChatterBot
{
    public class CleverbotResponse
    {
        public string Cs { get; set; }
        public string Output { get; set; }
    }

    public class CleverbotIOCreateResponse
    {
        public string Status { get; set; }
        public string Nick { get; set; }
    }

    public class CleverbotIOAskResponse
    {
        public string Status { get; set; }
        public string Response { get; set; }
    }
}