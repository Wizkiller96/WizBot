﻿using Grpc.Core;
using Grpc.Core.Interceptors;
using WizBot.Common.ModuleBehaviors;

namespace WizBot.GrpcApi;

public class GrpcApiService : INService, IReadyExecutor
{
    private Server? _app;

    private readonly DiscordSocketClient _client;
    private readonly OtherSvc _other;
    private readonly ExprsSvc _exprs;
    private readonly GreetByeSvc _greet;
    private readonly IBotCredsProvider _creds;

    public GrpcApiService(
        DiscordSocketClient client,
        OtherSvc other,
        ExprsSvc exprs,
        GreetByeSvc greet,
        IBotCredsProvider creds)
    {
        _client = client;
        _other = other;
        _exprs = exprs;
        _greet = greet;
        _creds = creds;
    }

    public Task OnReadyAsync()
    {
        var creds = _creds.GetCreds();
        if (creds.GrpcApi is null || creds.GrpcApi.Enabled)
            return Task.CompletedTask;

        try
        {
            var host = creds.GrpcApi.Host;
            var port = creds.GrpcApi.Port + _client.ShardId;

            var interceptor = new PermsInterceptor(_client);

            _app = new Server()
            {
                Services =
                {
                    GrpcOther.BindService(_other).Intercept(interceptor),
                    GrpcExprs.BindService(_exprs).Intercept(interceptor),
                    GrpcGreet.BindService(_greet).Intercept(interceptor),
                },
                Ports =
                {
                    new(host, port, ServerCredentials.Insecure),
                }
            };

            _app.Start();

            Log.Information("Grpc Api Server started on port {Host}:{Port}", host, port);
        }
        catch
        {
            _app?.ShutdownAsync().GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }
}