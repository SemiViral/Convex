﻿#region USINGS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Convex.Client.Component;
using Convex.Util;

#endregion

namespace Convex.IRC.Net {
    public class ServerMessage : Message {
        public ServerMessage(string rawData, Func<ServerMessage, string> formatter) : base(rawData) {
            if (rawData.StartsWith("ERROR")) {
                Command = Commands.ERROR;
                Args = rawData.Substring(rawData.IndexOf(' ') + 1);
                return;
            }

            Parse();

            _formatter = formatter;
        }

        #region METHODS

        /// <summary>
        ///     For parsing IRCv3 message tags
        /// </summary>
        private void ParseTagsPrefix() {
            if (!RawMessage.StartsWith("@")) {
                return;
            }

            IsIrCv3Message = true;

            string fullTagsPrefix = RawMessage.Substring(0, RawMessage.IndexOf(' '));
            string[] primitiveTagsCollection = RawMessage.Split(';');

            foreach (string[] splitPrimitiveTag in primitiveTagsCollection.Select(primitiveTag => primitiveTag.Split('='))) {
                Tags.Add(splitPrimitiveTag[0], splitPrimitiveTag[1] ?? string.Empty);
            }
        }

        public void Parse() {
            if (!_MessageRegex.IsMatch(RawMessage)) {
                return;
            }

            ParseTagsPrefix();

            Timestamp = DateTime.Now;

            // begin parsing message into sections
            Match mVal = _MessageRegex.Match(RawMessage);
            Match sMatch = _SenderRegex.Match(mVal.Groups["Sender"].Value);

            // class property setting
            Nickname = mVal.Groups["Sender"].Value;
            Realname = mVal.Groups["Sender"].Value.ToLower();
            Hostname = mVal.Groups["Sender"].Value;
            Command = mVal.Groups["Type"].Value;
            Origin = mVal.Groups["Recipient"].Value.StartsWith(":") ? mVal.Groups["Recipient"].Value.Substring(1) : mVal.Groups["Recipient"].Value;

            Args = mVal.Groups["Args"].Value;

            // splits the first 5 sections of the message for parsing
            SplitArgs = Args.Split(new[] { ' ' }, 4).Select(arg => arg.Trim()).ToList();

            if (!sMatch.Success) {
                return;
            }

            string realname = sMatch.Groups["Realname"].Value;
            Nickname = sMatch.Groups["Nickname"].Value;
            Realname = realname.StartsWith("~") ? realname.Substring(1) : realname;
            Hostname = sMatch.Groups["Hostname"].Value;
        }

        public override string ToString() {
            return RawMessage;
        }

        #endregion

        #region MEMBERS

        // Regex for parsing RawMessage messages
        private static readonly Regex _MessageRegex = new Regex(@"^:(?<Sender>[^\s]+)\s(?<Type>[^\s]+)\s(?<Recipient>[^\s]+)\s?:?(?<Args>.*)", RegexOptions.Compiled);
        private static readonly Regex _SenderRegex = new Regex(@"^(?<Nickname>[^\s]+)!(?<Realname>[^\s]+)@(?<Hostname>[^\s]+)", RegexOptions.Compiled);
        private readonly Func<ServerMessage, string> _formatter;

        public new string Formatted => _formatter.Invoke(this);

        public bool IsIrCv3Message { get; private set; }

        public string Realname { get; set; }
        public string Hostname { get; set; }
        public string Command { get; set; }
        public string Args { get; set; }
        public List<string> SplitArgs { get; set; }

        public string InputCommand { get; set; } = string.Empty;

        public readonly Dictionary<string, string> Tags = new Dictionary<string, string>();

        #endregion
    }
}