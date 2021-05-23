﻿using WizBot.Common;
using WizBot.Modules.Games.Common.Hangman.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace WizBot.Modules.Games.Common.Hangman
{
    public class TermPool
    {
        const string termsPath = "data/hangman.json";

        public IReadOnlyDictionary<string, HangmanObject[]> Data { get; } = new Dictionary<string, HangmanObject[]>();
        public TermPool()
        {
            try
            {
                Data = JsonConvert.DeserializeObject<Dictionary<string, HangmanObject[]>>(File.ReadAllText(termsPath));
                Data = Data.ToDictionary(
                    x => x.Key.ToLowerInvariant(),
                    x => x.Value);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error loading Hangman Term pool");
            }
        }

        public HangmanObject GetTerm(string type)
        {
            type = type?.Trim().ToLowerInvariant();
            var rng = new WizBotRandom();

            if (type == "random")
            {
                type = Data.Keys.ToArray()[rng.Next(0, Data.Keys.Count())];
            }
            if (!Data.TryGetValue(type, out var termTypes) || termTypes.Length == 0)
                throw new TermNotFoundException();

            var obj = termTypes[rng.Next(0, termTypes.Length)];

            obj.Word = obj.Word.Trim().ToLowerInvariant();
            return obj;
        }
    }
}
