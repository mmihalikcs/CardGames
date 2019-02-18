using M2.CardGames.Common;
using System.ComponentModel;
using System.Reflection;

namespace CardGames.GUI
{
    public static class CardExtensions
    {
        public static string DrawCard(this Card card)
        {
            return string.Format(
@"
[==============]
| {0}            |
| {1}            |
|              |
|              |
|            {1} |
|            {0} |
[==============]
", GetEnumDisplayName(card.Value), GetEnumDisplayName(card.Suit));
        }

        #region Private Helpers
        static string GetEnumDisplayName<T>(T enumType) where T : struct
        {
            MemberInfo[] memberInfo = enumType.GetType().GetMember(enumType.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    //Pull out the description value
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return string.Empty;
        }
        #endregion Private Helpers
    }
}
