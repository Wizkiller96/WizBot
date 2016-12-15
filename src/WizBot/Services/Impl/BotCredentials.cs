﻿using Newtonsoft.Json;
using System;
using System.IO;
using Discord;
using System.Linq;
using NLog;
using Microsoft.Extensions.Configuration;

namespace WizBot.Services.Impl
{
    public class BotCredentials : IBotCredentials
    {
        private Logger _log;

        public ulong ClientId { get; }
        public ulong BotId { get; }

        public string GoogleApiKey { get; }

        public string MashapeKey { get; }

        public string Token { get; }

        public ulong[] OwnerIds { get; }

        public string LoLApiKey { get; }
        public string OsuApiKey { get; }
        public string SoundCloudClientId { get; }

        public DBConfig Db { get; }
        public int TotalShards { get; }
        public string CarbonKey { get; }

        public string credsFileName { get; } = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

        public BotCredentials()
        {
            _log = LogManager.GetCurrentClassLogger();

            try { File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented)); } catch { }
            if(!File.Exists(credsFileName))
                _log.Warn($"credentials.json is missing. Attempting to load creds from environment variables prefixed with 'WizBot_'. Example is in {Path.GetFullPath("./credentials_example.json")}");
            try
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddJsonFile(credsFileName, true)
                    .AddEnvironmentVariables("WizBot_");

                var data = configBuilder.Build();

                Token = data[nameof(Token)];
                if (string.IsNullOrWhiteSpace(Token))
                    throw new ArgumentNullException(nameof(Token), "Token is missing from credentials.json or Environment varibles.");
                OwnerIds = data.GetSection("OwnerIds").GetChildren().Select(c => ulong.Parse(c.Value)).ToArray();
                LoLApiKey = data[nameof(LoLApiKey)];
                GoogleApiKey = data[nameof(GoogleApiKey)];
                MashapeKey = data[nameof(MashapeKey)];
                OsuApiKey = data[nameof(OsuApiKey)];

                int ts = 1;
                int.TryParse(data[nameof(TotalShards)], out ts);
                TotalShards = ts < 1 ? 1 : ts;

                ulong clId = 0;
                ulong.TryParse(data[nameof(ClientId)], out clId);
                ClientId = clId;

                SoundCloudClientId = data[nameof(SoundCloudClientId)];
                CarbonKey = data[nameof(CarbonKey)];
                var dbSection = data.GetSection("db");
                Db = new DBConfig(string.IsNullOrWhiteSpace(dbSection["Type"]) 
                                ? "sqlite" 
                                : dbSection["Type"], 
                            string.IsNullOrWhiteSpace(dbSection["ConnectionString"]) 
                                ? "Filename=./data/WizBot.db" 
                                : dbSection["ConnectionString"]);
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.Message);
                _log.Fatal(ex);
                throw;
            }
            
        }

        private class CredentialsModel
        {
            public ulong ClientId { get; set; } = 123123123;
            public string Token { get; set; } = "";
            public ulong[] OwnerIds { get; set; } = new ulong[1];
            public string LoLApiKey { get; set; } = "";
            public string GoogleApiKey { get; set; } = "";
            public string MashapeKey { get; set; } = "";
            public string OsuApiKey { get; set; } = "";
            public string SoundCloudClientId { get; set; } = "";
            public string CarbonKey { get; set; } = "";
            public DBConfig Db { get; set; } = new DBConfig("sqlite", "Filename=./data/WizBot.db");
            public int TotalShards { get; set; } = 1;
        }

        private class DbModel
        {
            public string Type { get; set; }
            public string ConnectionString { get; set; }
        }

        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
