using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests;

public sealed class TexasHoldemGameManagerTests
{
    private static Card C(Suit suit, Rank rank) => new(suit, rank);

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_FullHandToShowdown_DeterministicWinnerTakesThePot()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        // Deal order: Alice[AS,AH], Bob[2C,7D]; flop KC,QD,JH; turn 9S; river 4C.
        // Alice makes a pair of Aces; Bob's best is just King-high - Alice wins clearly.
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Clubs, Rank.Two),
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Seven),
            C(Suit.Clubs, Rank.King), C(Suit.Diamonds, Rank.Queen), C(Suit.Hearts, Rank.Jack),
            C(Suit.Spades, Rank.Nine),
            C(Suit.Clubs, Rank.Four),
        });
        var io = new ScriptedGameIO(defaultResponse: "check");

        var manager = new TexasHoldemGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
        Assert.Contains("Alice wins the pot of 20!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_HumanFoldsPreflop_OpponentWinsWithoutShowdown()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Two), C(Suit.Clubs, Rank.Three),
            C(Suit.Hearts, Rank.Four), C(Suit.Diamonds, Rank.Five),
        });
        var io = new ScriptedGameIO(new[] { "fold" });

        var manager = new TexasHoldemGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.True(alice.HasFolded);
        Assert.Equal(490, alice.Chips);
        Assert.Equal(510, bob.Chips);
        Assert.Contains("Bob wins the pot of 20 uncontested!", io.AllOutput);
        Assert.DoesNotContain("Community cards:", io.AllOutput);
    }
}
