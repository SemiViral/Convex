﻿#region USINGS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Convex.Event;
using Convex.Util;
using Newtonsoft.Json;

#endregion

namespace Convex.Core {
    public class Configuration : IDisposable, IConfiguration {
        private static void WriteConfig(string configString, string path) {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.WriteLine(configString);
                    writer.Flush();
                }
            }
        }

        public static void CheckCreateConfig(string path) {
            if (File.Exists(path)) {
                return;
            }

            StaticLog.Log(new LogEventArgs(Serilog.Events.LogEventLevel.Information, "Configuration file not found, creating.\n"));

            WriteConfig(DEFAULT_CONFIG, path);
        }

        #region MEMBERS

        // I know this isn't readable. Just run the program once and you'll get a much cleaner
        // representation of the default config in the generated config.json
        public const string DEFAULT_CONFIG = "{\r\n\t\"IgnoreList\": [],\r\n\t\"ApiKeys\": { \"YouTube\": \"\", \"Dictionary\": \"\" },\r\n\t\"Realname\": \"Evealyn\",\r\n\t\"Nickname\": \"Eve\",\r\n\t\"Password\": \"evepass\",\r\n\t\"DatabaseFilePath\": \"\",\r\n\t\"LogFilePath\": \"\",\r\n\t\"PluginsDirectoryPath\": \"\"\r\n}\r\n";
        public static readonly string DefaultResourceDirectory = AppContext.BaseDirectory.EndsWith(@"\") ? $@"{AppContext.BaseDirectory}\Resources" : $@"{AppContext.BaseDirectory}\Resources";
        public static readonly string DefaultConfigurationFilePath = DefaultResourceDirectory + @"\config.json";
        public static readonly string DefualtDatabaseFilePath = DefaultResourceDirectory + @"\users.sqlite";
        public static readonly string DefaultLogFilePath = DefaultResourceDirectory + @"\Logged.txt";
        public static readonly string DefaultPluginDirectoryPath = DefaultResourceDirectory + @"\Plugins";

        public List<string> IgnoreList { get; } = new List<string>();
        public Dictionary<string, string> ApiKeys { get; } = new Dictionary<string, string>();

        public string FilePath { get; set; }

        public string Realname { get; set; }
        public string Nickname { get; set; }
        public string Password { get; set; }

        public string LogFilePath {
            get => string.IsNullOrEmpty(_logFilePath) ? DefaultLogFilePath : _logFilePath;
            set => _logFilePath = value;
        }

        public string PluginDirectoryPath {
            get => string.IsNullOrEmpty(_databaseFilePath) ? DefualtDatabaseFilePath : _databaseFilePath;
            set => _databaseFilePath = value;
        }

        private string _databaseFilePath;
        private bool _disposed;
        private string _logFilePath;

        #endregion

        #region INTERFACE IMPLEMENTATION

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose) {
            if (!dispose || _disposed) {
                return;
            }

            WriteConfig(JsonConvert.SerializeObject(this), string.IsNullOrWhiteSpace(FilePath) ? DefaultConfigurationFilePath : FilePath);

            _disposed = true;
        }

        public static IConfiguration ParseConfig(string path) {
            IEnumerable<Property> properties = ReadConfig(path).Select(GenerateProperty);

            Configuration config = new Configuration();

            foreach (Property prop in properties) {
                switch (prop.Property) {
                    case "nickname":
                        config.Nickname = prop.Value;
                        break;
                    case "realname":
                        config.Realname = prop.Value;
                        break;
                    case "password":
                        config.Password = prop.Value;
                        break;
                    case "pluginsdirectory":
                        config.PluginDirectoryPath = prop.Value;
                        break;
                    case "logfile":
                        config.LogFilePath = prop.Value;
                        break;
                }
            }

        }

        private static IEnumerable<string> ReadConfig(string path) {
            using (StreamReader fstream = new StreamReader(path)) {
                yield return fstream.ReadLine();
            }
        }
        
        private static Property GenerateProperty(string rawProperty) {
            string[] splitProp = rawProperty.Split(' ', 2);
            return new Property(splitProp[0].ToLower(), splitProp[1].ToLower());
        }

        private class Property {
            public Property(string property, string value) {
                Property = property;
                Value = value;
            }

            public string Property { get; }
            public string Value { get; }
        }
        #endregion
    }
}