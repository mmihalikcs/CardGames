using System;
using System.Collections.Generic;
using System.Text;

namespace M2.CardGames.Common.Services
{
    public interface IAssemblyLoaderService
    {
        bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType);
        bool LoadPluginAssembly(string assemblyPath);
        bool UnloadPluginAssembly(string contextName);
    }
}
