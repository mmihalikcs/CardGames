using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;

namespace CardGames.Poker;

internal sealed class OmahaGameManager : CommunityCardGameManagerBase
{
    protected override int HoleCardCount => 4;

    public OmahaGameManager(IGameIO io) : base(io)
    {
    }

    // Test seam: rig seats/deck/random and cap hands played.
    internal OmahaGameManager(IGameIO io, Random random, List<Seat> seats, PokerDeck deck, int maxHands = 1)
        : base(io, random, seats, deck, maxHands)
    {
    }

    protected override HandRank EvaluateShowdown(Seat seat, IReadOnlyList<Card> community) =>
        HandEvaluator.EvaluateBestOmaha(seat.HoleCards, community);
}
