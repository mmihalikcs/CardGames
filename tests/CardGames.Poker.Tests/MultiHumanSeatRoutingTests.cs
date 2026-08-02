using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests;

/// <summary>
/// Proves each human seat only ever sees its own hole cards and is prompted independently,
/// via ISeatContextGameIO scoping (BettingRound.PromptHuman / *.ShowHoleCardsToEachHuman /
/// FiveCardDrawGameManager.PromptHumanDiscards).
/// </summary>
public sealed class MultiHumanSeatRoutingTests
{
    private static Card C(Suit suit, Rank rank) => new(suit, rank);

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TexasHoldem_TwoHumans_EachOnlySeesOwnHoleCardsAndIsPromptedIndependently()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Clubs, Rank.Two),
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Seven),
            C(Suit.Clubs, Rank.King), C(Suit.Diamonds, Rank.Queen), C(Suit.Hearts, Rank.Jack),
            C(Suit.Spades, Rank.Nine),
            C(Suit.Clubs, Rank.Four),
        });
        var io = new ScriptedSeatContextGameIO(defaultResponse: "check");

        var manager = new TexasHoldemGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        var aliceOutput = io.OutputFor("Alice");
        var bobOutput = io.OutputFor("Bob");

        Assert.Contains(aliceOutput, line => line.Contains("Alice's hole cards:"));
        Assert.DoesNotContain(aliceOutput, line => line.Contains("Bob's hole cards:"));
        Assert.Contains(aliceOutput, line => line.StartsWith("Action ["));

        Assert.Contains(bobOutput, line => line.Contains("Bob's hole cards:"));
        Assert.DoesNotContain(bobOutput, line => line.Contains("Alice's hole cards:"));
        Assert.Contains(bobOutput, line => line.StartsWith("Action ["));

        // Public table state is broadcast (unscoped), not attributed to either seat.
        Assert.Contains(io.BroadcastOutput, line => line.Contains("Community cards:"));

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void FiveCardDraw_TwoHumans_DiscardPromptAndHoleCardsRoutedToCorrectSeat()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Clubs, Rank.Two),
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Seven),
            C(Suit.Clubs, Rank.King), C(Suit.Spades, Rank.Nine),
            C(Suit.Diamonds, Rank.Queen), C(Suit.Hearts, Rank.Four),
            C(Suit.Hearts, Rank.Jack), C(Suit.Diamonds, Rank.Six),
        });
        var io = new ScriptedSeatContextGameIO(new Dictionary<string, IEnumerable<string?>>
        {
            ["Alice"] = new[] { "check", "", "check" },
            ["Bob"] = new[] { "check", "", "check" },
        });

        var manager = new FiveCardDrawGameManager(io, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        var aliceOutput = io.OutputFor("Alice");
        var bobOutput = io.OutputFor("Bob");

        Assert.Contains(aliceOutput, line => line.Contains("Alice's hole cards:"));
        Assert.DoesNotContain(aliceOutput, line => line.Contains("Bob's hole cards:"));

        Assert.Contains(bobOutput, line => line.Contains("Bob's hole cards:"));
        Assert.DoesNotContain(bobOutput, line => line.Contains("Alice's hole cards:"));

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
    }
}
