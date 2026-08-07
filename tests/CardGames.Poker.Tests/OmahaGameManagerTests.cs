using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests;

public sealed class OmahaGameManagerTests
{
    private static Card C(Suit suit, Rank rank) => new(suit, rank);

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void StartGame_FullHandToShowdown_RespectsExactlyTwoHoleCardsAndAwardsBestHand()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        // Alice[AS,AH,2C,3D], Bob[4C,5D,6H,7S]; community KC,QD,JH,9S,8C.
        // Alice's best uses exactly 2 hole (AS,AH) + 3 community (K,Q,J) for a pair of Aces.
        // Bob has no straight available under the exactly-2-hole-card constraint - best is high card.
        var deck = new PokerDeck(new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Clubs, Rank.Four),
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Five),
            C(Suit.Clubs, Rank.Two), C(Suit.Hearts, Rank.Six),
            C(Suit.Diamonds, Rank.Three), C(Suit.Spades, Rank.Seven),
            C(Suit.Clubs, Rank.King), C(Suit.Diamonds, Rank.Queen), C(Suit.Hearts, Rank.Jack),
            C(Suit.Spades, Rank.Nine),
            C(Suit.Clubs, Rank.Eight),
        });
        var channel = new ScriptedGameChannel(defaultResponse: "check");

        var manager = new OmahaGameManager(channel, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.Equal(510, alice.Chips);
        Assert.Equal(490, bob.Chips);
        Assert.Contains(channel.Published, e => e is PotAwarded { WinnerNames: ["Alice"], Total: 20 });
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
            C(Suit.Clubs, Rank.Six), C(Suit.Hearts, Rank.Seven),
            C(Suit.Diamonds, Rank.Eight), C(Suit.Spades, Rank.Nine),
        });
        var channel = new ScriptedGameChannel(new[] { "fold" });

        var manager = new OmahaGameManager(channel, new Random(1), new List<Seat> { alice, bob }, deck, maxHands: 1);
        manager.StartGame();

        Assert.True(alice.HasFolded);
        Assert.Equal(490, alice.Chips);
        Assert.Equal(510, bob.Chips);
        Assert.Contains(channel.Published, e => e is UncontestedPotAwarded { WinnerName: "Bob", Total: 20 });
        Assert.DoesNotContain(channel.Published, e => e is CommunityCardsRevealed);
    }
}
