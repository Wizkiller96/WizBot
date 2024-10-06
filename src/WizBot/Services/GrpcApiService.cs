using Grpc;
using Grpc.Core;
using WizBot.Common.ModuleBehaviors;

namespace WizBot.GrpcApi;

public class GrpcApiService : INService, IReadyExecutor
{
    private Server? _app;

    private static readonly bool _isEnabled = true;
    private readonly string _host = "localhost";
    private readonly int _port = 5030;
    private readonly ServerCredentials _creds = ServerCredentials.Insecure;

    private readonly OtherSvc _other;
    private readonly ExprsSvc _exprs;
    private readonly ServerInfoSvc _info;
    private readonly GreetByeSvc _greet;

    public GrpcApiService(
        OtherSvc other,
        ExprsSvc exprs,
        ServerInfoSvc info,
        GreetByeSvc greet)
    {
        _other = other;
        _exprs = exprs;
        _info = info;
        _greet = greet;
    }

    public async Task OnReadyAsync()
    {
        if (!_isEnabled)
            return;
        
        try
        {
            _app = new()
            {
                Services =
                {
                    GrpcOther.BindService(_other),
                    GrpcExprs.BindService(_exprs),
                    GrpcInfo.BindService(_info),
                    GrpcGreet.BindService(_greet)
                },
                Ports =
                {
                    new(_host, _port, _creds),
                }
            };
            _app.Start();
        }
        finally
        {
            _app?.ShutdownAsync().GetAwaiter().GetResult();
        }

        Log.Information("Grpc Api Server started on port {Host}:{Port}", _host, _port);
    }
}