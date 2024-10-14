using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WizBot.Db.Models;
using WizBot.Modules.WizBotExpressions;

namespace WizBot.GrpcApi;

public class ExprsSvc : GrpcExprs.GrpcExprsBase, INService
{
    private readonly WizBotExpressionsService _svc;

    public ExprsSvc(WizBotExpressionsService svc)
    {
        _svc = svc;
    }
    
    public override async Task<AddExprReply> AddExpr(AddExprRequest request, ServerCallContext context)
    {
        WizBotExpression expr;
        if (!string.IsNullOrWhiteSpace(request.Expr.Id))
        {
            expr = await _svc.EditAsync(request.GuildId,
                new kwum(request.Expr.Id),
                request.Expr.Response,
                request.Expr.Ca,
                request.Expr.Ad,
                request.Expr.Dm);
        }
        else
        {
            expr = await _svc.AddAsync(request.GuildId,
                request.Expr.Trigger,
                request.Expr.Response,
                request.Expr.Ca,
                request.Expr.Ad,
                request.Expr.Dm);
        }


        return new AddExprReply()
        {
            Id = new kwum(expr.Id).ToString(),
            Success = true,
        };
    }
    
    public override async Task<GetExprsReply> GetExprs(GetExprsRequest request, ServerCallContext context)
    {
        var (exprs, totalCount) = await _svc.FindExpressionsAsync(request.GuildId, request.Query, request.Page);

        var reply = new GetExprsReply();
        reply.TotalCount = totalCount;
        reply.Expressions.AddRange(exprs.Select(x => new ExprDto()
        {
            Ad = x.AutoDeleteTrigger,
            At = x.AllowTarget,
            Ca = x.ContainsAnywhere,
            Dm = x.DmResponse,
            Response = x.Response,
            Id = new kwum(x.Id).ToString(),
            Trigger = x.Trigger,
        }));

        return reply;
    }
    
    public override async Task<Empty> DeleteExpr(DeleteExprRequest request, ServerCallContext context)
    {
        await _svc.DeleteAsync(request.GuildId, new kwum(request.Id));

        return new Empty();
    }
}