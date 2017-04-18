using System.Collections.Generic;

namespace WizBot.Modules.Utility.Commands.Models
{
    public class MeasurementUnit
    {
        public List<string> Triggers { get; set; }
        public string UnitType { get; set; }
        public decimal Modifier { get; set; }
    }
}
