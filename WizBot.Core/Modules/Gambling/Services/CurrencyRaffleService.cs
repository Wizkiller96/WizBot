using System.Threading.Tasks;
using WizBot.Core.Services;
using System.Collections.Concurrent;
using WizBot.Core.Modules.Gambling.Common;
using System.Threading;

namespace WizBot.Core.Modules.Gambling.Services
{
    public class CurrencyRaffleService : INService
    {
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly DbService _db;

        public ConcurrentDictionary<ulong, CurrencyRaffleGame> Games { get; }

        public CurrencyRaffleService(DbService db)
        {
            _db = db;
        }

        public async Task JoinOrCreateGame(ulong channelId, string username,
            ulong userId)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                using (var uow = _db.UnitOfWork)
                {
                    //remove money
                    if (!await _cs.RemoveAsync(userId, "Currency Raffle Join", _amount).ConfigureAwait(false))
                        return false;
                }

                //add to to list
                if (_users.Add((username, userId)))
                    return false;
                return true;
            }
            finally
            {
                _locker.Release();
            }
        }
    }
}
