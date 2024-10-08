using Grpc.Core;
using Grpc.Core.Interceptors;

namespace WizBot.GrpcApi;

public sealed partial class GrpcApiPermsInterceptor : Interceptor
{
    private readonly DiscordSocketClient _client;

    public GrpcApiPermsInterceptor(DiscordSocketClient client)
    {
        _client = client;
        Log.Information("interceptor created");
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            Log.Information("Starting receiving call. Type/Method: {Type} / {Method}",
                MethodType.Unary,
                context.Method);

            // get metadata
            var metadata = context
                           .RequestHeaders
                           .ToDictionary(x => x.Key, x => x.Value);

            if(!metadata.ContainsKey("userid"))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "userid has to be specified."));

            var method = context.Method[(context.Method.LastIndexOf('/') + 1)..];

            if (perms.TryGetValue(method, out var perm))
            {
                Log.Information("Required permission for {Method} is {Perm}",
                    method,
                    perm);

                var userId = ulong.Parse(metadata["userid"]);
                var guildId = ulong.Parse(metadata["guildid"]);

                IGuild guild = _client.GetGuild(guildId);
                var user = guild is null ? null : await guild.GetUserAsync(userId);

                if (user is null)
                    throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

                if (!user.GuildPermissions.Has(perm))
                    throw new RpcException(new Status(StatusCode.PermissionDenied,
                        $"You need {perm} permission to use this method"));
            }
            else
            {
                Log.Information("No permission required for {Method}", method);
            }

            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error thrown by {ContextMethod}", context.Method);
            throw;
        }
    }
}