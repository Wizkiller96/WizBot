using Grpc.Core;

namespace WizBot.GrpcApi;

public static class SvcExtensions
{
    public static ulong GetUserId(this Metadata meta)
        => ulong.Parse(meta.FirstOrDefault(x => x.Key == "userid")!.Value);
}