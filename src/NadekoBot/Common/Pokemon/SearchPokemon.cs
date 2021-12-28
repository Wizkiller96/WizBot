#nullable disable
using Newtonsoft.Json;

namespace NadekoBot.Common.Pokemon;

public class SearchPokemon
{
    public class GenderRatioClass
    {
        public float M { get; set; }
        public float F { get; set; }
    }

    public class BaseStatsClass
    {
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spa { get; set; }
        public int Spd { get; set; }
        public int Spe { get; set; }

        public override string ToString()
            => $@"💚**HP:**  {Hp,-4} ⚔**ATK:** {Atk,-4} 🛡**DEF:** {Def,-4}
✨**SPA:** {Spa,-4} 🎇**SPD:** {Spd,-4} 💨**SPE:** {Spe,-4}";
    }

    [JsonProperty("num")]
    public int Id { get; set; }

    public string Species { get; set; }
    public string[] Types { get; set; }
    public GenderRatioClass GenderRatio { get; set; }
    public BaseStatsClass BaseStats { get; set; }
    public Dictionary<string, string> Abilities { get; set; }
    public float HeightM { get; set; }
    public float WeightKg { get; set; }
    public string Color { get; set; }
    public string[] Evos { get; set; }
    public string[] EggGroups { get; set; }
}
