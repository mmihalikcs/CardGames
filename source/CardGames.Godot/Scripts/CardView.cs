using CardGames.Domain.Extensions;
using CardGames.Domain.Models;
using Godot;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Procedurally-drawn playing card face - no external image assets. Rank in the top-left/bottom-
/// right corners, a large centered suit glyph, red for Hearts/Diamonds and black for Clubs/Spades,
/// reusing the same Suit/Rank extensions (VisualizationExtensions) the console's ASCII rendering
/// (Card.DisplayCard()) draws from, so the two stay visually consistent without duplicating the
/// rank/suit-to-text mapping.
/// </summary>
public partial class CardView : Control
{
    private const float CardWidth = 70f;
    private const float CardHeight = 100f;
    private static readonly Color RedSuitColor = new(0.75f, 0.1f, 0.1f);

    private Card? _Card;

    public override void _Ready() => CustomMinimumSize = new Vector2(CardWidth, CardHeight);

    public void SetCard(Card card)
    {
        _Card = card;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_Card is null)
            return;

        var rect = new Rect2(Vector2.Zero, new Vector2(CardWidth, CardHeight));
        DrawRect(rect, Colors.White);
        DrawRect(rect, Colors.Black, filled: false, width: 2f);

        var suitColor = _Card.Suit.IsRedSuit() ? RedSuitColor : Colors.Black;
        var rankText = _Card.Rank.GetEnumDescription();
        var suitGlyph = _Card.Suit.GetSuitGlyph();
        var font = ThemeDB.FallbackFont;

        DrawString(font, new Vector2(6, 18), rankText, HorizontalAlignment.Left, -1, 16, suitColor);
        DrawString(font, new Vector2(6, 34), suitGlyph, HorizontalAlignment.Left, -1, 14, suitColor);

        DrawString(font, new Vector2(0, CardHeight - 26), rankText, HorizontalAlignment.Right, CardWidth - 6, 16, suitColor);
        DrawString(font, new Vector2(0, CardHeight - 10), suitGlyph, HorizontalAlignment.Right, CardWidth - 6, 14, suitColor);

        DrawString(font, new Vector2(0, CardHeight / 2 + 14), suitGlyph, HorizontalAlignment.Center, CardWidth, 34, suitColor);
    }
}
