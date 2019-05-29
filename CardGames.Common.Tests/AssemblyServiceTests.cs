using M2.CardGames.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace M2.CardGames.Common.Tests
{
    [TestClass]
    public class AssemblyServiceTests
    {
        IAssemblyLoaderService _LoaderService;
        ILoggerFactory _LoggerFactory;

        [TestInitialize]
        public void Init()
        {
            _LoggerFactory = new LoggerFactory();
            _LoaderService = new AssemblyLoaderService(_LoggerFactory);
        }

        [TestMethod]
        public void LoadAssembly()
        {
            _LoaderService.LoadPluginAssembly(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netcoreapp3.0\M2.CardGames.War.dll");
        }

        [TestMethod]
        public void UnloadAssembly()
        {
            Assert.IsTrue(_LoaderService.LoadPluginAssembly(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netcoreapp3.0\M2.CardGames.War.dll"));
            Assert.IsTrue(_LoaderService.UnloadPluginAssembly("M2.CardGames.War"));
        }
    }
}
