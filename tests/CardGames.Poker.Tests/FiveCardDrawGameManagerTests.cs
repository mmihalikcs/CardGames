using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests;

public sealed class FiveCardDrawGameManagerTests
{
    private static Card C(Suit suit, Rank rank) => new(suit, rank);

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_BothStandPat_DeterministicWinnerTakesThePot()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        // Deal order: Alice[AS,AH,KC,QD,JH] (pair of Aces), Bob[2C,7D,9S,4H,6D] (high card only).
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Clubs, Rank.Two),
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Seven),
            C(Suit.Clubs, Rank.King), C(Suit.Spades, Rank.Nine),
            C(Suit.Diamonds, Rank.Queen), C(Suit.Hearts, Rank.Four),
            C(Suit.Hearts, Rank.Jack), C(Suit.Diamonds, Rank.Six),
        });
        // Betting round 1 (check, check), draw phase (stand pat, stand pat), betting round 2 (check, check).
        var io = new ScriptedGameIO(new[] { "check", "check", "", "", "check", "check" });

        var manager = new FiveCardDrawGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
        Assert.Contains("Alice stands pat.", io.AllOutput);
        Assert.Contains("Bob stands pat.", io.AllOutput);
        Assert.Contains("Alice wins the pot of 20!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_HumanFoldsFirstBettingRound_OpponentWinsWithoutDrawOrShowdown()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Two), C(Suit.Clubs, Rank.Three),
            C(Suit.Hearts, Rank.Four), C(Suit.Diamonds, Rank.Five),
            C(Suit.Clubs, Rank.Six), C(Suit.Spades, Rank.Seven),
            C(Suit.Diamonds, Rank.Eight), C(Suit.Hearts, Rank.Nine),
            C(Suit.Hearts, Rank.Ten), C(Suit.Diamonds, Rank.Jack),
        });
        var io = new ScriptedGameIO(new[] { "fold" });

        var manager = new FiveCardDrawGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.True(alice.HasFolded);
        Assert.Equal(490, alice.Chips);
        Assert.Equal(510, bob.Chips);
        Assert.Contains("Bob wins the pot of 20 uncontested!", io.AllOutput);
        Assert.DoesNotContain("Draw Phase", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_DrawPhaseImprovesHand_ChangesTheShowdownOutcome()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        // Deal order: Alice[AS,AH,2C,3D,4H] (pair of Aces - loses to a flush pre-draw);
        // Bob[2H,5H,9H,JH,KH] (a flush - he stands pat).
        // Alice discards her 2C,3D,4H (positions 3,4,5) and draws AC,KC,KD, upgrading to a
        // full house (Aces over Kings), which beats Bob's flush - the draw decides the hand.
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Hearts, Rank.Two),
            C(Suit.Hearts, Rank.Ace), C(Suit.Hearts, Rank.Five),
            C(Suit.Clubs, Rank.Two), C(Suit.Hearts, Rank.Nine),
            C(Suit.Diamonds, Rank.Three), C(Suit.Hearts, Rank.Jack),
            C(Suit.Hearts, Rank.Four), C(Suit.Hearts, Rank.King),
            C(Suit.Clubs, Rank.Ace), C(Suit.Clubs, Rank.King), C(Suit.Diamonds, Rank.King),
        });
        var io = new ScriptedGameIO(new[] { "check", "check", "3 4 5", "", "check", "check" });

        var manager = new FiveCardDrawGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.Equal(new[] { Rank.Ace, Rank.Ace, Rank.Ace, Rank.King, Rank.King }, alice.HoleCards.Select(c => c.Rank));
        Assert.Contains("Alice draws 3 card(s).", io.AllOutput);
        Assert.Contains("Bob stands pat.", io.AllOutput);
        Assert.Contains("Alice wins the pot of 20!", io.AllOutput);
        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
    }
}
