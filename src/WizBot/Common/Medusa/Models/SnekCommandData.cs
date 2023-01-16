using Microsoft.VisualBasic;
using System.Reflection;
using CommandStrings = WizBot.Snake.CommandStrings;

namespace WizBot.Medusa;

public sealed class SnekCommandData
{
    public SnekCommandData(
        IReadOnlyCollection<string> aliases,
        MethodInfo methodInfo,
        Snek module,
        FilterAttribute[] filters,
        user_permAttribute[] userPerms,
        CommandContextType contextType,
        IReadOnlyList<Type> injectedParams,
        IReadOnlyList<ParamData> parameters,
        CommandStrings strings,
        int priority)
    {
        Aliases = aliases;
        MethodInfo = methodInfo;
        Module = module;
        Filters = filters;
        UserPerms = userPerms;
        ContextType = contextType;
        InjectedParams = injectedParams;
        Parameters = parameters;
        Priority = priority;
        OptionalStrings = strings;
    }

    public user_permAttribute[] UserPerms { get; set; }
    
    public CommandStrings OptionalStrings { get; set; }

    public IReadOnlyCollection<string> Aliases { get; }
    public MethodInfo MethodInfo { get; set; }
    public Snek Module { get; set; }
    public FilterAttribute[] Filters { get; set; }
    public CommandContextType ContextType { get; }
    public IReadOnlyList<Type> InjectedParams { get; }
    public IReadOnlyList<ParamData> Parameters { get; }
    public int Priority { get; }
}