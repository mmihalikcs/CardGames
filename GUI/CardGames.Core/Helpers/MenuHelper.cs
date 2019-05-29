using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;

namespace M2.CardGames.Core.Helpers
{
    public static class MenuHelper
    {
        public static int DisplayGamesList(List<AssemblyLoadContext> loadedAssemblies)
        {
            for(int i = 0; i <= loadedAssemblies.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {loadedAssemblies[i].Name}");
            }
            Console.Write("Enter your selection: ");
            var input = Console.ReadLine();

            // TODO
            int.TryParse(input, out int result);
            return result;
        }
    }
}
