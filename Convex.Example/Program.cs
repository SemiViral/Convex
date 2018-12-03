﻿#region USINGS

using System;
using System.Threading.Tasks;
using Convex.Core;
using Serilog;

#endregion

namespace Convex.Example {
    public static class Program {
        #region MEMBERS

        private static IrcBot Bot { get; set; }
        private static IConfiguration Config { get; set; }

        #endregion

        private static async Task DebugRun() {
            do {
                await Bot.Execute();
            } while (Bot.Executing);

            Bot.Dispose();
        }

        private static async Task InitialiseAndExecute() {
            Config = Configuration.ParseConfig("config");

            using (Bot = new IrcBot()) {
                Log.Logger = new LoggerConfiguration().WriteTo.RollingFile(Bot.Config.LogFilePath).WriteTo.LiterateConsole().CreateLogger();

                await Bot.Initialise();
                await DebugRun();
            }
        }

        public static async Task Main() {
            await InitialiseAndExecute();

            Console.Write("Program terminated. Press any key to continue.");
            Console.ReadKey();
        }
    }
}