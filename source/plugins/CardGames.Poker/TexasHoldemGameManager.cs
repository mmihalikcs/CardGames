using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;

namespace CardGames.Poker;

internal sealed class TexasHoldemGameManager : CommunityCardGameManagerBase
{
    protected override int HoleCardCount => 2;

    public TexasHoldemGameManager(IGameIO io) : base(io)
    {
    }

    // Test seam: rig seats/deck/random and cap hands played.
    internal TexasHoldemGameManager(IGameIO io, Random random, List<Seat> seats, PokerDeck deck, int maxHands = 1)
        : base(io, random, seats, deck, maxHands)
    {
    }

    protected override HandRank EvaluateShowdown(Seat seat, IReadOnlyList<Card> community) =>
        HandEvaluator.EvaluateBest(seat.HoleCards.Concat(community).ToList());
}
