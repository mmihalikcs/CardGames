using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;
using Xunit;

namespace CardGames.Poker.Tests.Engine;

public sealed class HandEvaluatorTests
{
    private static Card C(Suit suit, Rank rank) => new(suit, rank);

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_NoPairsNoStraightNoFlush_ReturnsHighCardWithDescendingTiebreakers()
    {
        var hand = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Diamonds, Rank.Five), C(Suit.Clubs, Rank.Nine),
            C(Suit.Spades, Rank.Jack), C(Suit.Hearts, Rank.Ace),
        };

        var result = HandEvaluator.EvaluateExact5(hand);

        Assert.Equal(HandCategory.HighCard, result.Category);
        Assert.Equal(new[] { (int)Rank.Ace, (int)Rank.Jack, (int)Rank.Nine, (int)Rank.Five, (int)Rank.Two }, result.Tiebreakers);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_OnePair_HigherKickerBeatsLowerKicker()
    {
        var withAceKicker = new[]
        {
            C(Suit.Hearts, Rank.King), C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.Ace),
            C(Suit.Spades, Rank.Five), C(Suit.Hearts, Rank.Two),
        };
        var withQueenKicker = new[]
        {
            C(Suit.Hearts, Rank.King), C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.Queen),
            C(Suit.Spades, Rank.Five), C(Suit.Hearts, Rank.Two),
        };

        var strong = HandEvaluator.EvaluateExact5(withAceKicker);
        var weak = HandEvaluator.EvaluateExact5(withQueenKicker);

        Assert.Equal(HandCategory.OnePair, strong.Category);
        Assert.True(strong.CompareTo(weak) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_TwoPair_HigherTopPairBeatsLowerTopPair()
    {
        var acesAndKings = new[]
        {
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Ace), C(Suit.Clubs, Rank.King),
            C(Suit.Spades, Rank.King), C(Suit.Hearts, Rank.Five),
        };
        var queensAndKings = new[]
        {
            C(Suit.Hearts, Rank.Queen), C(Suit.Diamonds, Rank.Queen), C(Suit.Clubs, Rank.King),
            C(Suit.Spades, Rank.King), C(Suit.Hearts, Rank.Five),
        };

        var better = HandEvaluator.EvaluateExact5(acesAndKings);
        var worse = HandEvaluator.EvaluateExact5(queensAndKings);

        Assert.Equal(HandCategory.TwoPair, better.Category);
        Assert.True(better.CompareTo(worse) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_ThreeOfAKind_IsRecognized()
    {
        var hand = new[]
        {
            C(Suit.Hearts, Rank.Seven), C(Suit.Diamonds, Rank.Seven), C(Suit.Clubs, Rank.Seven),
            C(Suit.Spades, Rank.Nine), C(Suit.Hearts, Rank.Two),
        };

        var result = HandEvaluator.EvaluateExact5(hand);

        Assert.Equal(HandCategory.ThreeOfAKind, result.Category);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_StandardStraight_IsRecognized()
    {
        var hand = new[]
        {
            C(Suit.Hearts, Rank.Nine), C(Suit.Diamonds, Rank.Ten), C(Suit.Clubs, Rank.Jack),
            C(Suit.Spades, Rank.Queen), C(Suit.Hearts, Rank.King),
        };

        var result = HandEvaluator.EvaluateExact5(hand);

        Assert.Equal(HandCategory.Straight, result.Category);
        Assert.Equal(new[] { (int)Rank.King }, result.Tiebreakers);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_WheelStraight_PlaysAceLowAndLosesToAStandardStraight()
    {
        var wheel = new[]
        {
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Two), C(Suit.Clubs, Rank.Three),
            C(Suit.Spades, Rank.Four), C(Suit.Hearts, Rank.Five),
        };
        var standard = new[]
        {
            C(Suit.Hearts, Rank.Nine), C(Suit.Diamonds, Rank.Ten), C(Suit.Clubs, Rank.Jack),
            C(Suit.Spades, Rank.Queen), C(Suit.Hearts, Rank.King),
        };

        var wheelResult = HandEvaluator.EvaluateExact5(wheel);
        var standardResult = HandEvaluator.EvaluateExact5(standard);

        Assert.Equal(HandCategory.Straight, wheelResult.Category);
        Assert.Equal(new[] { (int)Rank.Five }, wheelResult.Tiebreakers);
        Assert.True(standardResult.CompareTo(wheelResult) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_Flush_ComparesByDescendingKickers()
    {
        var flushWithAce = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Hearts, Rank.Five), C(Suit.Hearts, Rank.Nine),
            C(Suit.Hearts, Rank.Jack), C(Suit.Hearts, Rank.Ace),
        };
        var flushWithKing = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Hearts, Rank.Five), C(Suit.Hearts, Rank.Nine),
            C(Suit.Hearts, Rank.Jack), C(Suit.Hearts, Rank.King),
        };

        var better = HandEvaluator.EvaluateExact5(flushWithAce);
        var worse = HandEvaluator.EvaluateExact5(flushWithKing);

        Assert.Equal(HandCategory.Flush, better.Category);
        Assert.True(better.CompareTo(worse) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_FullHouse_TripRankTieBrokenByPairRank()
    {
        var kingsOverThrees = new[]
        {
            C(Suit.Hearts, Rank.King), C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.King),
            C(Suit.Spades, Rank.Three), C(Suit.Hearts, Rank.Three),
        };
        var kingsOverTwos = new[]
        {
            C(Suit.Hearts, Rank.King), C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.King),
            C(Suit.Spades, Rank.Two), C(Suit.Hearts, Rank.Two),
        };

        var better = HandEvaluator.EvaluateExact5(kingsOverThrees);
        var worse = HandEvaluator.EvaluateExact5(kingsOverTwos);

        Assert.Equal(HandCategory.FullHouse, better.Category);
        Assert.True(better.CompareTo(worse) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_FourOfAKind_HigherKickerBeatsLowerKicker()
    {
        var withAceKicker = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Diamonds, Rank.Two), C(Suit.Clubs, Rank.Two),
            C(Suit.Spades, Rank.Two), C(Suit.Hearts, Rank.Ace),
        };
        var withKingKicker = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Diamonds, Rank.Two), C(Suit.Clubs, Rank.Two),
            C(Suit.Spades, Rank.Two), C(Suit.Hearts, Rank.King),
        };

        var better = HandEvaluator.EvaluateExact5(withAceKicker);
        var worse = HandEvaluator.EvaluateExact5(withKingKicker);

        Assert.Equal(HandCategory.FourOfAKind, better.Category);
        Assert.True(better.CompareTo(worse) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_StraightFlushBeatsFourOfAKind_RegardlessOfNumericTiebreakers()
    {
        var lowStraightFlush = new[]
        {
            C(Suit.Hearts, Rank.Five), C(Suit.Hearts, Rank.Six), C(Suit.Hearts, Rank.Seven),
            C(Suit.Hearts, Rank.Eight), C(Suit.Hearts, Rank.Nine),
        };
        var highFourOfAKind = new[]
        {
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Ace), C(Suit.Clubs, Rank.Ace),
            C(Suit.Spades, Rank.Ace), C(Suit.Hearts, Rank.King),
        };

        var straightFlush = HandEvaluator.EvaluateExact5(lowStraightFlush);
        var quads = HandEvaluator.EvaluateExact5(highFourOfAKind);

        Assert.Equal(HandCategory.StraightFlush, straightFlush.Category);
        Assert.Equal(HandCategory.FourOfAKind, quads.Category);
        Assert.True(straightFlush.CompareTo(quads) > 0);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_RoyalFlush_IsJustAnAceHighStraightFlush()
    {
        var hand = new[]
        {
            C(Suit.Hearts, Rank.Ten), C(Suit.Hearts, Rank.Jack), C(Suit.Hearts, Rank.Queen),
            C(Suit.Hearts, Rank.King), C(Suit.Hearts, Rank.Ace),
        };

        var result = HandEvaluator.EvaluateExact5(hand);

        Assert.Equal(HandCategory.StraightFlush, result.Category);
        Assert.Equal((int)Rank.Ace, result.Tiebreakers[0]);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateExact5_IdenticalHands_CompareEqual()
    {
        var handA = new[]
        {
            C(Suit.Hearts, Rank.King), C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.Nine),
            C(Suit.Spades, Rank.Five), C(Suit.Hearts, Rank.Two),
        };
        var handB = new[]
        {
            C(Suit.Clubs, Rank.King), C(Suit.Spades, Rank.King), C(Suit.Diamonds, Rank.Nine),
            C(Suit.Hearts, Rank.Five), C(Suit.Diamonds, Rank.Two),
        };

        var resultA = HandEvaluator.EvaluateExact5(handA);
        var resultB = HandEvaluator.EvaluateExact5(handB);

        Assert.Equal(0, resultA.CompareTo(resultB));
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateBest_SevenCards_FindsTheBestFiveCardFlushAmongUnrelatedCards()
    {
        // Hole cards are unrelated to the flush; the flush is made from 5 of the 7 available
        // hearts spread across hole + community, proving the brute-force search isn't just
        // taking the first 5 cards.
        var sevenCards = new[]
        {
            C(Suit.Hearts, Rank.Two), C(Suit.Diamonds, Rank.Three),
            C(Suit.Hearts, Rank.Four), C(Suit.Hearts, Rank.Five), C(Suit.Hearts, Rank.Six),
            C(Suit.Hearts, Rank.Seven), C(Suit.Clubs, Rank.Eight),
        };

        var result = HandEvaluator.EvaluateBest(sevenCards);

        Assert.Equal(HandCategory.Flush, result.Category);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EvaluateBestOmaha_CannotUseThreeHoleCards_SoQuadsAreUnreachable()
    {
        // Hole has 3 aces + a deuce; community has the 4th ace + trip kings. An unconstrained
        // best-5-of-9 search would find four aces, but Omaha requires exactly 2 hole + 3
        // community cards, and no 2-hole-card selection can supply 3 of the aces needed for
        // quads - the best reachable hand is a full house (aces over kings).
        var holeCards = new[]
        {
            C(Suit.Hearts, Rank.Ace), C(Suit.Diamonds, Rank.Ace),
            C(Suit.Clubs, Rank.Ace), C(Suit.Spades, Rank.Two),
        };
        var communityCards = new[]
        {
            C(Suit.Spades, Rank.Ace), C(Suit.Hearts, Rank.King),
            C(Suit.Diamonds, Rank.King), C(Suit.Clubs, Rank.King), C(Suit.Hearts, Rank.Two),
        };

        var result = HandEvaluator.EvaluateBestOmaha(holeCards, communityCards);

        Assert.Equal(HandCategory.FullHouse, result.Category);
    }
}
