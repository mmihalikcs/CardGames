using CardGames.Common.Tests;
using CardGames.Console.Services;
using CardGames.Console.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CardGames.Console.Tests.Services;

public sealed class ConsoleRendererTests
{
    private readonly ConsoleRenderer _Renderer;

    public ConsoleRendererTests()
    {
        var logger = new LoggerFactory().CreateLogger<ConsoleRenderer>();
        _Renderer = new ConsoleRenderer(logger, new NoOpAssemblyLoaderService());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Commands_HasExpectedMenuEntriesInOrder()
    {
        var commands = _Renderer.GetCommands();

        Assert.Equal(6, commands.Count);
        Assert.Equal("Play", commands[1]);
        Assert.Equal("Load Game", commands[2]);
        Assert.Equal("Unload Game", commands[3]);
        Assert.Equal("Settings", commands[4]);
        Assert.Equal("About", commands[5]);
        Assert.Equal("Exit", commands[0]);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Commands_SettingsPrecedesAbout()
    {
        var settingsKey = _Renderer.GetCommands().First(x => x.Value == "Settings").Key;
        var aboutKey = _Renderer.GetCommands().First(x => x.Value == "About").Key;

        Assert.True(settingsKey < aboutKey);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void DisplayMenu_WritesEveryCommandAndExit()
    {
        var originalOut = System.Console.Out;
        try
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);

            _Renderer.DisplayMenu();

            var output = writer.ToString();
            Assert.Contains("1) Play", output);
            Assert.Contains("2) Load Game", output);
            Assert.Contains("3) Unload Game", output);
            Assert.Contains("4) Settings", output);
            Assert.Contains("5) About", output);
            Assert.Contains("0) Exit", output);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void DisplaySubmenu_WritesTitleOptionsAndCancelPrompt()
    {
        var originalOut = System.Console.Out;
        try
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);

            _Renderer.DisplaySubmenu("Select a Poker variant", new[] { "Texas Hold'em", "Omaha", "Five-Card Draw" });

            var output = writer.ToString();
            Assert.Contains("Select a Poker variant:", output);
            Assert.Contains("1) Texas Hold'em", output);
            Assert.Contains("2) Omaha", output);
            Assert.Contains("3) Five-Card Draw", output);
            Assert.Contains("0) Cancel", output);
            Assert.Contains("Enter a selection: ", output);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void DisplaySubmenu_EmptyOptions_WritesOnlyCancelPrompt()
    {
        var originalOut = System.Console.Out;
        try
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);

            _Renderer.DisplaySubmenu("Select a variant", Array.Empty<string>());

            var output = writer.ToString();
            Assert.DoesNotContain("1)", output);
            Assert.Contains("0) Cancel", output);
            Assert.Contains("Enter a selection: ", output);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }
}
