using NadekoBot.Services.Database.Models;
using NadekoBot.Common;

namespace NadekoBot.Modules.CustomReactions
{
    public class ExportedExpr
    {
        public string Res { get; set; }
        public string Id { get; set; }
        public bool Ad { get; set; }
        public bool Dm { get; set; }
        public bool At { get; set; }
        public bool Ca { get; set; }
        public string[] React;

        public static ExportedExpr FromModel(CustomReaction cr)
            => new ExportedExpr()
            {
                Res = cr.Response,
                Id = ((kwum)cr.Id).ToString(),
                Ad = cr.AutoDeleteTrigger,
                At = cr.AllowTarget,
                Ca = cr.ContainsAnywhere,
                Dm = cr.DmResponse,
                React = string.IsNullOrWhiteSpace(cr.Reactions)
                    ? null
                    : cr.GetReactions(),
            };
    }
}