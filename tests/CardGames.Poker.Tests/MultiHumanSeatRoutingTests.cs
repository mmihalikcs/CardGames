using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Interaction;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests;

/// <summary>
/// Proves each human seat only ever sees its own hole cards and is prompted independently,
/// via ISeatContextGameChannel scoping (BettingRound.PromptHuman / *.ShowHoleCardsToEachHuman /
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
        var channel = new ScriptedSeatContextGameChannel(defaultResponse: "check");

        var manager = new TexasHoldemGameManager(channel, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        var aliceEvents = channel.EventsFor("Alice");
        var bobEvents = channel.EventsFor("Bob");

        Assert.Contains(aliceEvents, e => e is HoleCardsRevealed { SeatName: "Alice" });
        Assert.DoesNotContain(aliceEvents, e => e is HoleCardsRevealed { SeatName: "Bob" });
        Assert.Contains(channel.PromptsFor("Alice"), p => p is ChoicePrompt { Message: "Action" });

        Assert.Contains(bobEvents, e => e is HoleCardsRevealed { SeatName: "Bob" });
        Assert.DoesNotContain(bobEvents, e => e is HoleCardsRevealed { SeatName: "Alice" });
        Assert.Contains(channel.PromptsFor("Bob"), p => p is ChoicePrompt { Message: "Action" });

        // Public table state is broadcast (unscoped), not attributed to either seat.
        Assert.Contains(channel.BroadcastEvents, e => e is CommunityCardsRevealed);

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
        var channel = new ScriptedSeatContextGameChannel(new Dictionary<string, IEnumerable<string?>>
        {
            ["Alice"] = new[] { "check", "", "check" },
            ["Bob"] = new[] { "check", "", "check" },
        });

        var manager = new FiveCardDrawGameManager(channel, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        var aliceEvents = channel.EventsFor("Alice");
        var bobEvents = channel.EventsFor("Bob");

        Assert.Contains(aliceEvents, e => e is HoleCardsRevealed { SeatName: "Alice" });
        Assert.DoesNotContain(aliceEvents, e => e is HoleCardsRevealed { SeatName: "Bob" });

        Assert.Contains(bobEvents, e => e is HoleCardsRevealed { SeatName: "Bob" });
        Assert.DoesNotContain(bobEvents, e => e is HoleCardsRevealed { SeatName: "Alice" });

        Assert.Contains(channel.PromptsFor("Alice"), p => p is TextPrompt);
        Assert.Contains(channel.PromptsFor("Bob"), p => p is TextPrompt);

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
    }
}
