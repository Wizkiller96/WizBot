using WizBot.Modules.Gambling.Bank;
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Services;

namespace WizBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Bank")]
    [Group("bank")]
    public partial class BankCommands : GamblingModule<IBankService>
    {
        private readonly IBankService _bank;
        private readonly DiscordSocketClient _client;

        public BankCommands(GamblingConfigService gcs,
            IBankService bank,
            DiscordSocketClient client) : base(gcs)
        {
            _bank = bank;
            _client = client;
        }

        [Cmd]
        public async Task BankDeposit(ShmartNumber amount)
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
        public async Task BankWithdraw(ShmartBankAmount amount)
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
        public async Task BankBalance()
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
                await ReplyErrorLocalizedAsync(strs.cant_dm);
            }
        }
        
        private async Task BankTakeInternalAsync(long amount, ulong userId)
        {
            if (await _bank.TakeAsync(userId, amount))
            {
                await ReplyErrorLocalizedAsync(strs.take_fail(N(amount),
                    _client.GetUser(userId)?.ToString()
                    ?? userId.ToString(),
                    CurrencySign));
                return;
            }
            
            await ctx.OkAsync();
        }
        
        private async Task BankAwardInternalAsync(long amount, ulong userId)
        {
            if (await _bank.AwardAsync(userId, amount))
            {
                await ReplyErrorLocalizedAsync(strs.take_fail(N(amount),
                    _client.GetUser(userId)?.ToString()
                    ?? userId.ToString(),
                    CurrencySign));
                return;
            }
            
            await ctx.OkAsync();
        }

        [Cmd]
        [AdminOnly]
        [Priority(1)]
        public async Task BankTake(long amount, [Leftover] IUser user)
            => await BankTakeInternalAsync(amount, user.Id);
        
        [Cmd]
        [AdminOnly]
        [Priority(0)]
        public async Task BankTake(long amount, ulong userId)
            => await BankTakeInternalAsync(amount, userId);
        
        [Cmd]
        [AdminOnly]
        public async Task BankAward(long amount, [Leftover] IUser user)
            => await BankAwardInternalAsync(amount, user.Id);
    }
}