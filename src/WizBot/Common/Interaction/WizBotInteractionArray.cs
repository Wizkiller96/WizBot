// namespace WizBot;
//
// public class WizBotButtonInteractionArray : WizBotButtonInteraction
// {
//     private readonly ButtonBuilder[] _bbs;
//     private readonly WizBotButtonInteraction[] _inters;
//
//     public WizBotButtonInteractionArray(params WizBotButtonInteraction[] inters)
//         : base(inters[0].Client)
//     {
//         _inters = inters;
//         _bbs = inters.Map(x => x.GetButtonBuilder());
//     }
//
//     protected override string Name
//         => throw new NotSupportedException();
//     protected override IEmote Emote
//         => throw new NotSupportedException();
//
//     protected override ValueTask<bool> Validate(SocketMessageComponent smc)
//         => new(true);
//
//     public override Task ExecuteOnActionAsync(SocketMessageComponent smc)
//     {
//         for (var i = 0; i < _bbs.Length; i++)
//         {
//             if (_bbs[i].CustomId == smc.Data.CustomId)
//                 return _inters[i].ExecuteOnActionAsync(smc);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public override MessageComponent CreateComponent()
//     {
//         var comp = new ComponentBuilder();
//
//         foreach (var bb in _bbs)
//             comp.WithButton(bb);
//         
//         return comp.Build();
//     }
// }