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
/// Scaffold for the "test-suite" category (dotnet test --filter TestCategory=test-suite) - slower or
/// more extensive Godot-client coverage that isn't part of the CI post-build gate. This first test
/// proves the routing in GameSessionPanel.ShowCardGroups is generic across plugins, not just Poker's
/// hole-cards/community split: WAR's CardsRevealed publishes two groups ("You" and "Computer") that
/// both default to CardGroupRole.Own, so both should land together in the hand container.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class DualOwnGroupLayoutTests
{
    [TestCase]
    [TestCategory(TestCaseConstants.EXTENDED_TEST_TRAIT_VALUE)]
    public async Task TwoDefaultOwnGroupsBothRenderInTheHandContainer()
    {
        var runner = ISceneRunner.Load("res://Scenes/GameSessionPanel.tscn", true);
        var panel = (GameSessionPanel)runner.Scene();

        var playerCard = new Card(Suit.Spades, Rank.Ace);
        var computerCard = new Card(Suit.Hearts, Rank.King);
        panel.AppendEvent(new CardsRevealedTestEvent(playerCard, computerCard));
        await runner.SimulateFrames(1);

        var hand = panel.GetNode<HBoxContainer>("Layout/Table/HandBottom/HandDisplay");
        var community = panel.GetNode<HBoxContainer>("Layout/Table/CommunityCenter/CommunityDisplay");

        AssertThat(hand.GetChildCount()).IsEqual(2);
        AssertThat(community.GetChildCount()).IsEqual(0);
    }

    // Mirrors CardGames.WAR's internal CardsRevealed shape (both groups default to CardGroupRole.Own).
    private sealed record CardsRevealedTestEvent(Card PlayerCard, Card ComputerCard) : GameEvent
    {
        public override string Describe() => $"You played {PlayerCard}. Computer played {ComputerCard}.";

        public override IReadOnlyList<CardGroup> CardGroups =>
            [new CardGroup("You", [PlayerCard]), new CardGroup("Computer", [ComputerCard])];
    }
}
