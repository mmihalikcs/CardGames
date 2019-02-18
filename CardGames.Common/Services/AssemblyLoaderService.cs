using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace M2.CardGames.Common.Services
{
    public class AssemblyLoaderService : IAssemblyLoaderService
    {
        private readonly ILogger<AssemblyLoaderService> _Logger;

        public AssemblyLoaderService(ILoggerFactory loggingFactory)
        {
            _Logger = loggingFactory.CreateLogger<AssemblyLoaderService>();
        }

        /// <summary>
        /// Verifies if a specific assembly contains a type
        /// Uses reflection only loading for security.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType)
        {
            Assembly reflectedAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);

            foreach (var type in reflectedAssembly.GetTypes())
            {
                _Logger.LogDebug($"Type '[{type}]'");
                if (interfaceType.IsAssignableFrom(type)) return true;
            }
            return false;
        }

    }
}
