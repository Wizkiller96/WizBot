#nullable disable
using Nadeko.Econ.Gambling;
using OneOf;

namespace NadekoBot.Modules.Gambling;

public interface IGamblingService
{
    Task<OneOf<WofResult, GamblingError>> WofAsync(ulong userId, long amount);
    Task<OneOf<BetrollResult, GamblingError>> BetRollAsync(ulong userId, long amount);
    Task<OneOf<BetflipResult, GamblingError>> BetFlipAsync(ulong userId, long amount, int guess);
    Task<OneOf<SlotResult, GamblingError>> SlotAsync(ulong userId, long amount);
    Task<FlipResult[]> FlipAsync(int count);
}