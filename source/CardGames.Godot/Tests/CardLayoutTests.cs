using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Interaction;
using CardGames.Domain.Models;
using CardGames.Godot.Scripts;

using GdUnit4;

using Godot;

using static GdUnit4.Assertions;

namespace CardGames.Godot.Tests;

/// <summary>
/// Regression coverage for the Own/Community card-layout split in GameSessionPanel.ShowCardGroups:
/// before CardGroup gained a Role, every event's card groups landed in one shared, fully-cleared
/// container, so Poker's hole-cards reveal (published right after each community-card reveal - see
/// CommunityCardGameManagerBase.ShowHoleCardsToEachHuman) silently wiped out the community board the
/// player had just been shown. These tests replay that real event sequence against a live scene.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class CardLayoutTests
{
    private static readonly IReadOnlyList<Card> HoleCards = [new Card(Suit.Spades, Rank.Ace), new Card(Suit.Hearts, Rank.King)];
    private static readonly IReadOnlyList<Card> CommunityCards = [new Card(Suit.Clubs, Rank.Two), new Card(Suit.Diamonds, Rank.Three), new Card(Suit.Spades, Rank.Four)];

    [TestCase]
    [TestCategory(TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public async Task CommunityCardsSurviveASubsequentHoleCardsEvent()
    {
        var runner = ISceneRunner.Load("res://Scenes/GameSessionPanel.tscn", true);
        var panel = (GameSessionPanel)runner.Scene();

        panel.AppendEvent(new HoleCardsRevealedTestEvent("You", HoleCards));
        await runner.SimulateFrames(1);
        AssertThat(HandChildCount(panel)).IsEqual(1);
        AssertThat(CommunityChildCount(panel)).IsEqual(0);

        panel.AppendEvent(new CommunityCardsRevealedTestEvent(CommunityCards));
        await runner.SimulateFrames(1);
        AssertThat(HandChildCount(panel)).IsEqual(1);
        AssertThat(CommunityChildCount(panel)).IsEqual(1);

        // This is the actual bug: a second hole-cards event used to clear the community board too.
        panel.AppendEvent(new HoleCardsRevealedTestEvent("You", HoleCards));
        await runner.SimulateFrames(1);
        AssertThat(HandChildCount(panel)).IsEqual(1);
        AssertThat(CommunityChildCount(panel)).IsEqual(1);
    }

    private static int HandChildCount(GameSessionPanel panel) =>
        panel.GetNode<HBoxContainer>("Layout/Table/HandBottom/HandDisplay").GetChildCount();

    private static int CommunityChildCount(GameSessionPanel panel) =>
        panel.GetNode<HBoxContainer>("Layout/Table/CommunityCenter/CommunityDisplay").GetChildCount();

    // Mirrors Poker's actual internal event shapes (CardGames.Poker.Engine.GameEvents) without
    // depending on them directly - those records are internal to the Poker plugin assembly, and
    // GameSessionPanel only ever depends on the public GameEvent/CardGroup/CardGroupRole contract.
    private sealed record HoleCardsRevealedTestEvent(string SeatName, IReadOnlyList<Card> Cards) : GameEvent
    {
        public override string Describe() => $"{SeatName}'s hole cards:";
        public override IReadOnlyList<CardGroup> CardGroups => [new CardGroup(SeatName, Cards)];
    }

    private sealed record CommunityCardsRevealedTestEvent(IReadOnlyList<Card> Cards) : GameEvent
    {
        public override string Describe() => "Community cards:";
        public override IReadOnlyList<CardGroup> CardGroups => [new CardGroup("Community", Cards, CardGroupRole.Community)];
    }
}
