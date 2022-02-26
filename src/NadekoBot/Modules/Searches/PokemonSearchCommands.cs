#nullable disable
using NadekoBot.Common.Pokemon;
using NadekoBot.Modules.Searches.Services;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class PokemonSearchCommands : NadekoModule<SearchesService>
    {
        public IReadOnlyDictionary<string, SearchPokemon> Pokemons
            => _cache.LocalData.Pokemons;

        public IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities
            => _cache.LocalData.PokemonAbilities;

        private readonly IDataCache _cache;

        public PokemonSearchCommands(IDataCache cache)
            => _cache = cache;

        [Cmd]
        public async partial Task Pokemon([Leftover] string pokemon = null)
        {
            pokemon = pokemon?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(pokemon))
                return;

            foreach (var kvp in Pokemons)
            {
                if (kvp.Key.ToUpperInvariant() == pokemon.ToUpperInvariant())
                {
                    var p = kvp.Value;
                    await ctx.Channel.EmbedAsync(_eb.Create()
                                                    .WithOkColor()
                                                    .WithTitle(kvp.Key.ToTitleCase())
                                                    .WithDescription(p.BaseStats.ToString())
                                                    .WithThumbnailUrl(
                                                        $"https://assets.pokemon.com/assets/cms2/img/pokedex/detail/{p.Id.ToString("000")}.png")
                                                    .AddField(GetText(strs.types), string.Join("\n", p.Types), true)
                                                    .AddField(GetText(strs.height_weight),
                                                        GetText(strs.height_weight_val(p.HeightM, p.WeightKg)),
                                                        true)
                                                    .AddField(GetText(strs.abilities),
                                                        string.Join("\n", p.Abilities.Select(a => a.Value)),
                                                        true));
                    return;
                }
            }

            await ReplyErrorLocalizedAsync(strs.pokemon_none);
        }

        [Cmd]
        public async partial Task PokemonAbility([Leftover] string ability = null)
        {
            ability = ability?.Trim().ToUpperInvariant().Replace(" ", "", StringComparison.InvariantCulture);
            if (string.IsNullOrWhiteSpace(ability))
                return;
            foreach (var kvp in PokemonAbilities)
            {
                if (kvp.Key.ToUpperInvariant() == ability)
                {
                    await ctx.Channel.EmbedAsync(_eb.Create()
                                                    .WithOkColor()
                                                    .WithTitle(kvp.Value.Name)
                                                    .WithDescription(string.IsNullOrWhiteSpace(kvp.Value.Desc)
                                                        ? kvp.Value.ShortDesc
                                                        : kvp.Value.Desc)
                                                    .AddField(GetText(strs.rating),
                                                        kvp.Value.Rating.ToString(Culture),
                                                        true));
                    return;
                }
            }

            await ReplyErrorLocalizedAsync(strs.pokemon_ability_none);
        }
    }
}