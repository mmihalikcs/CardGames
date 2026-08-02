using CardGames.Common.Tests;
using CardGames.Poker.Engine;
using Xunit;

namespace CardGames.Poker.Tests.Engine;

public sealed class AiDecisionMakerTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Decide_MaxStrength_NoToCall_OnlyEverRaisesOrChecksAndRaisesAtLeastOnce()
    {
        bool sawRaise = false;
        for (int seed = 0; seed < 200; seed++)
        {
            var ai = new AiDecisionMaker(new Random(seed));
            var action = ai.Decide(strength: 1.0, toCall: 0, chipsRemaining: 500, canRaise: true);

            Assert.True(action is PokerAction.Raise or PokerAction.Check);
            if (action == PokerAction.Raise)
                sawRaise = true;
        }

        Assert.True(sawRaise, "Expected at least one Raise across 200 seeds for a maximum-strength hand.");
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Decide_MinStrength_FacingBet_NeverRaises()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var ai = new AiDecisionMaker(new Random(seed));
            var action = ai.Decide(strength: 0.0, toCall: 10, chipsRemaining: 500, canRaise: true);

            Assert.NotEqual(PokerAction.Raise, action);
        }
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Decide_CannotRaise_NeverReturnsRaise()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var ai = new AiDecisionMaker(new Random(seed));

            Assert.NotEqual(PokerAction.Raise, ai.Decide(strength: 1.0, toCall: 0, chipsRemaining: 500, canRaise: false));
            Assert.NotEqual(PokerAction.Raise, ai.Decide(strength: 1.0, toCall: 10, chipsRemaining: 500, canRaise: false));
        }
    }
}
