#nullable disable
using NadekoBot.Common.Pokemon;
using NadekoBot.Modules.Games.Common.Trivia;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace NadekoBot.Services;

public class RedisLocalDataCache : ILocalDataCache
{
    private const string POKEMON_ABILITIES_FILE = "data/pokemon/pokemon_abilities.json";
    private const string POKEMON_LIST_FILE = "data/pokemon/pokemon_list.json";
    private const string POKEMON_MAP_PATH = "data/pokemon/name-id_map.json";
    private const string QUESTIONS_FILE = "data/trivia_questions.json";

    public IReadOnlyDictionary<string, SearchPokemon> Pokemons
    {
        get => Get<Dictionary<string, SearchPokemon>>("pokemon_list");
        private init => Set("pokemon_list", value);
    }

    public IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities
    {
        get => Get<Dictionary<string, SearchPokemonAbility>>("pokemon_abilities");
        private init => Set("pokemon_abilities", value);
    }

    public TriviaQuestion[] TriviaQuestions
    {
        get => Get<TriviaQuestion[]>("trivia_questions");
        private init => Set("trivia_questions", value);
    }

    public IReadOnlyDictionary<int, string> PokemonMap
    {
        get => Get<Dictionary<int, string>>("pokemon_map");
        private init => Set("pokemon_map", value);
    }

    private readonly ConnectionMultiplexer _con;
    private readonly IBotCredentials _creds;

    public RedisLocalDataCache(ConnectionMultiplexer con, IBotCredentials creds, DiscordSocketClient client)
    {
        _con = con;
        _creds = creds;
        var shardId = client.ShardId;

        if (shardId == 0)
        {
            if (!File.Exists(POKEMON_LIST_FILE))
                Log.Warning($"{POKEMON_LIST_FILE} is missing. Pokemon abilities not loaded");
            else
            {
                Pokemons =
                    JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(
                        File.ReadAllText(POKEMON_LIST_FILE));
            }

            if (!File.Exists(POKEMON_ABILITIES_FILE))
                Log.Warning($"{POKEMON_ABILITIES_FILE} is missing. Pokemon abilities not loaded.");
            else
            {
                PokemonAbilities =
                    JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(
                        File.ReadAllText(POKEMON_ABILITIES_FILE));
            }

            try
            {
                TriviaQuestions = JsonConvert.DeserializeObject<TriviaQuestion[]>(File.ReadAllText(QUESTIONS_FILE));
                PokemonMap = JsonConvert.DeserializeObject<PokemonNameId[]>(File.ReadAllText(POKEMON_MAP_PATH))
                                        ?.ToDictionary(x => x.Id, x => x.Name)
                             ?? new();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading local data");
                throw;
            }
        }
    }

    private T Get<T>(string key)
        where T : class
        => JsonConvert.DeserializeObject<T>(_con.GetDatabase().StringGet($"{_creds.RedisKey()}_localdata_{key}"));

    private void Set(string key, object obj)
        => _con.GetDatabase().StringSet($"{_creds.RedisKey()}_localdata_{key}", JsonConvert.SerializeObject(obj));
}