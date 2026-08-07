using CardGames.Domain.Interaction;
using Godot;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Generic in-game screen: an event log that appends GameEvent.Describe() lines (events stay
/// plugin-opaque, same as TextGameChannel), and a prompt area rebuilt per GamePrompt with a widget
/// matching its concrete type - ConfirmPrompt/ChoicePrompt/TextPrompt are generic across every
/// plugin, so this needs no per-plugin knowledge to render them richly (buttons instead of raw text
/// input), unlike GameEvent's plugin-internal leaf types.
/// </summary>
public partial class GameSessionPanel : Control
{
    private HBoxContainer _HandDisplay = null!;
    private HBoxContainer _CommunityDisplay = null!;
    private RichTextLabel _EventLog = null!;
    private VBoxContainer _PromptArea = null!;
    private VBoxContainer _GameOverPanel = null!;

    private GodotGameChannel? _Channel;

    public event Action? BackToMenuRequested;

    public override void _Ready()
    {
        _HandDisplay = GetNode<HBoxContainer>("Layout/Table/HandBottom/HandDisplay");
        _CommunityDisplay = GetNode<HBoxContainer>("Layout/Table/CommunityCenter/CommunityDisplay");
        _EventLog = GetNode<RichTextLabel>("Layout/EventLog");
        _PromptArea = GetNode<VBoxContainer>("Layout/PromptArea");
        _GameOverPanel = GetNode<VBoxContainer>("Layout/GameOverPanel");

        var backButton = _GameOverPanel.GetNode<Button>("BackButton");
        backButton.Pressed += () => BackToMenuRequested?.Invoke();
    }

    public void AttachChannel(GodotGameChannel channel) => _Channel = channel;

    public void ResetForNewSession()
    {
        ClearCardDisplay();
        _EventLog.Clear();
        ClearPromptArea();
        _GameOverPanel.Visible = false;
    }

    public void AppendEvent(GameEvent gameEvent)
    {
        _EventLog.AppendText(gameEvent.Describe() + "\n");
        if (gameEvent.CardGroups.Count > 0)
            ShowCardGroups(gameEvent.CardGroups);
    }

    // Shows the most recently revealed cards as real graphics (CardView) in one of two persistent
    // table regions, keyed by CardGroup.Role - "Own" cards bottom-middle, "Community" cards centered
    // on the table - rather than a single strip that every event fully replaces. Only the region a
    // given event actually has groups for gets rebuilt, so e.g. Poker's hole-card reveal doesn't wipe
    // out the community board an event earlier just published. The text log still gets Describe()'s
    // prose; only the ASCII art moved out of it.
    private void ShowCardGroups(IReadOnlyList<CardGroup> groups)
    {
        var community = groups.Where(g => g.Role == CardGroupRole.Community).ToList();
        var own = groups.Where(g => g.Role == CardGroupRole.Own).ToList();

        if (community.Count > 0)
            RebuildCardDisplay(_CommunityDisplay, community);
        if (own.Count > 0)
            RebuildCardDisplay(_HandDisplay, own);
    }

    private static void RebuildCardDisplay(HBoxContainer container, IReadOnlyList<CardGroup> groups)
    {
        foreach (var child in container.GetChildren())
            child.QueueFree();

        foreach (var group in groups)
        {
            var column = new VBoxContainer();
            if (!string.IsNullOrEmpty(group.Label))
                column.AddChild(new Label { Text = group.Label, HorizontalAlignment = HorizontalAlignment.Center });

            var row = new HBoxContainer();
            foreach (var card in group.Cards)
            {
                var cardView = new CardView();
                row.AddChild(cardView);
                cardView.SetCard(card);
            }
            column.AddChild(row);
            container.AddChild(column);
        }
    }

    private void ClearCardDisplay()
    {
        foreach (var child in _HandDisplay.GetChildren())
            child.QueueFree();
        foreach (var child in _CommunityDisplay.GetChildren())
            child.QueueFree();
    }

    public void ShowPrompt(GamePrompt prompt)
    {
        ClearPromptArea();

        switch (prompt)
        {
            case ConfirmPrompt confirm:
                AddPromptMessage(confirm.Message);
                var continueButton = new Button { Text = "Continue" };
                continueButton.Pressed += () => Respond(new Ack());
                _PromptArea.AddChild(continueButton);
                break;

            case ChoicePrompt choice:
                AddPromptMessage(choice.Message);
                var optionsRow = new HFlowContainer();
                foreach (var option in choice.ValidOptions)
                {
                    var optionButton = new Button { Text = option };
                    optionButton.Pressed += () => Respond(new ChoiceResponse(option));
                    optionsRow.AddChild(optionButton);
                }
                _PromptArea.AddChild(optionsRow);
                break;

            case TextPrompt text:
                AddPromptMessage(text.Message);
                var lineEdit = new LineEdit();
                var submitButton = new Button { Text = "Submit" };
                void Submit() => Respond(new TextResponse(lineEdit.Text));
                lineEdit.TextSubmitted += _ => Submit();
                submitButton.Pressed += Submit;
                var row = new HBoxContainer();
                row.AddChild(lineEdit);
                row.AddChild(submitButton);
                _PromptArea.AddChild(row);
                break;

            default:
                throw new NotSupportedException($"Unknown GamePrompt kind '{prompt.GetType().Name}'.");
        }
    }

    public void ShowGameOver()
    {
        ClearPromptArea();
        _GameOverPanel.Visible = true;
    }

    // Clearing the prompt area immediately (rather than after the response round-trips) prevents a
    // double press of a since-answered widget from submitting a second response.
    private void Respond(PromptResponse response)
    {
        ClearPromptArea();
        _Channel?.SubmitPromptResponse(response);
    }

    private void AddPromptMessage(string message) =>
        _PromptArea.AddChild(new Label { Text = message });

    private void ClearPromptArea()
    {
        foreach (var child in _PromptArea.GetChildren())
            child.QueueFree();
    }
}
