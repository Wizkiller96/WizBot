namespace Nadeko.Medusa;

public sealed record ParamData(
    Type Type,
    string Name,
    bool IsOptional,
    bool IsLeftover,
    bool IsParams
);