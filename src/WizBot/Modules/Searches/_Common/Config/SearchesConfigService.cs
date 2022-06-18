﻿using WizBot.Common.Configs;

namespace WizBot.Modules.Searches;

public class SearchesConfigService : ConfigServiceBase<SearchesConfig>
{
    private static string FILE_PATH = "data/searches.yml";
    private static readonly TypedKey<SearchesConfig> _changeKey = new("config.searches.updated");

    public override string Name
        => "searches";

    public SearchesConfigService(IConfigSeria serializer, IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
        AddParsedProp("webEngine",
            sc => sc.WebSearchEngine,
            ConfigParsers.InsensitiveEnum,
            ConfigPrinters.ToString);
        
        AddParsedProp("imgEngine",
            sc => sc.ImgSearchEngine,
            ConfigParsers.InsensitiveEnum,
            ConfigPrinters.ToString);
        
        AddParsedProp("ytProvider",
            sc => sc.YtProvider,
            ConfigParsers.InsensitiveEnum,
            ConfigPrinters.ToString);
    }
}