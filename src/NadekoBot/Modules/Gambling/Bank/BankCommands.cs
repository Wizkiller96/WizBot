using NadekoBot.Modules.Gambling.Bank;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Bank")]
    [Group("bank")]
    public partial class BankCommands : GamblingModule<IBankService>
    {
        private readonly IBankService _bank;

        public BankCommands(GamblingConfigService gcs, IBankService bank) : base(gcs)
        {
            _bank = bank;
        }

        [Cmd]
        public async partial Task BankDeposit(ShmartNumber amount)
        {
            if (amount <= 0)
                return;
            
            if (await _bank.DepositAsync(ctx.User.Id, amount))
            {
                await ReplyConfirmLocalizedAsync(strs.bank_deposited(N(amount)));
            }
            else
            {
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
            }
        }
        
        [Cmd]
        public async partial Task BankWithdraw(ShmartNumber amount)
        {
            if (amount <= 0)
                return;
            
            if (await _bank.WithdrawAsync(ctx.User.Id, amount))
            {
                await ReplyConfirmLocalizedAsync(strs.bank_withdrew(N(amount)));
            }
            else
            {
                await ReplyErrorLocalizedAsync(strs.bank_withdraw_insuff(CurrencySign));
            }
        }
        
        [Cmd]
        public async partial Task BankBalance()
        {
            var bal = await _bank.GetBalanceAsync(ctx.User.Id);

            var eb = _eb.Create(ctx)
                        .WithOkColor()
                        .WithDescription(GetText(strs.bank_balance(N(bal))));

            try
            {
                await ctx.User.EmbedAsync(eb);
                await ctx.OkAsync();
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.unable_to_dm_user);
            }
        }
    }
}