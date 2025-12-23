using CardGames.Domain.Models;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace CardGames.Presentation.Extensions;

public static class VisualizationExtensions
{
    extension(Enum enumeration)
    {
        public string GetEnumDescription()
        {
            FieldInfo fi = enumeration.GetType().GetField(enumeration?.ToString());
            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }
            return enumeration.ToString();
        }
    }

    extension(Card card) 
    {
        public string DisplayCard()
        {
            // Map common enum names to printable ranks
            var suitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hearts"] = "♥",
                ["Heart"] = "♥",
                ["Diamonds"] = "♦",
                ["Diamond"] = "♦",
                ["Clubs"] = "♣",
                ["Club"] = "♣",
                ["Spades"] = "♠",
                ["Spade"] = "♠"
            };

            string rankDescription = card.Rank.GetEnumDescription() ?? string.Empty;
            string suitDescription = card.Suit.GetEnumDescription() ?? string.Empty;
            // Use the mapping for suit unicode chars
            string suit = suitMap.TryGetValue(suitDescription, out var s) ? s : (suitDescription.Length > 0 ? suitDescription.Substring(0, 1) : "?");

            // card box is 11 chars wide including borders, inner width = 9
            var sb = new StringBuilder();
            sb.AppendLine("┌─────────┐");
            // Top rank (left-aligned). If rank is two chars ("10") it still fits.
            sb.AppendLine($"│{rankDescription.PadRight(3)}      │");
            // Empty/padding lines with suit roughly centered
            sb.AppendLine($"│    {suit}    │");
            sb.AppendLine($"│         │");
            sb.AppendLine($"│    {suit}    │");
            // Bottom rank (right-aligned)
            sb.AppendLine($"│      {rankDescription.PadLeft(3)}│");
            sb.AppendLine("└─────────┘");
            return sb.ToString();
        }
    }
}
