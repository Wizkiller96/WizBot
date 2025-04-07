using Grpc.Core;

namespace WizBot.GrpcApi;

public interface IGrpcSvc
{
    ServerServiceDefinition Bind();
}