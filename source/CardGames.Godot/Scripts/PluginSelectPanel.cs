using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using Godot;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Lists the plugins discovered via IAssemblyLoaderService.DiscoverPlugins and, for a plugin that
/// declares GameVariants (Poker), shows a variant sub-list before raising PluginChosen - mirrors
/// CardGames.Console's Load Game + variant submenu flow.
/// </summary>
public partial class PluginSelectPanel : Control
{
    private Label _StatusLabel = null!;
    private VBoxContainer _PluginList = null!;
    private VBoxContainer _VariantList = null!;

    public event Action<IPlugin, GameVariant?>? PluginChosen;

    public override void _Ready()
    {
        _StatusLabel = GetNode<Label>("Layout/StatusLabel");
        _PluginList = GetNode<VBoxContainer>("Layout/PluginScroll/PluginList");
        _VariantList = GetNode<VBoxContainer>("Layout/VariantList");
    }

    public void ShowPlugins(IReadOnlyList<IPlugin> plugins, string pluginDirectory)
    {
        _VariantList.Visible = false;
        ClearContainer(_VariantList);
        ClearContainer(_PluginList);

        if (plugins.Count == 0)
        {
            _StatusLabel.Text = $"No plugins found in '{pluginDirectory}'.";
            return;
        }

        _StatusLabel.Text = $"{plugins.Count} game(s) available in '{pluginDirectory}':";

        foreach (var plugin in plugins)
        {
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = $"{plugin.Name} (v{plugin.Version}) - {plugin.Description}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            var playButton = new Button { Text = "Play" };
            playButton.Pressed += () => OnPlayPressed(plugin);
            row.AddChild(label);
            row.AddChild(playButton);
            _PluginList.AddChild(row);
        }
    }

    private void OnPlayPressed(IPlugin plugin)
    {
        if (plugin.Variants.Count == 0)
        {
            PluginChosen?.Invoke(plugin, null);
            return;
        }

        ClearContainer(_VariantList);
        _VariantList.Visible = true;

        foreach (var variant in plugin.Variants)
        {
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = $"{variant.Name} - {variant.Description}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            var selectButton = new Button { Text = "Select" };
            selectButton.Pressed += () => PluginChosen?.Invoke(plugin, variant);
            row.AddChild(label);
            row.AddChild(selectButton);
            _VariantList.AddChild(row);
        }

        var backButton = new Button { Text = "Back" };
        backButton.Pressed += () => _VariantList.Visible = false;
        _VariantList.AddChild(backButton);
    }

    private static void ClearContainer(Node container)
    {
        foreach (var child in container.GetChildren())
            child.QueueFree();
    }
}
