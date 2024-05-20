using Azure.ResourceManager.ContainerInstance.Models;
using System.Collections.Generic;

namespace Tyler.Greer
{
    public class CS2ServerConfig
    {
        public string SrcdsToken { get; set; } // Game Server Token from https://steamcommunity.com/dev/managegameservers
        public string ServerName { get; set; } // Set the visible name for your private server
        public string ServerPassword { get; set; } // CS2 server password
        public string Cheats { get; set; } // 0 - disable cheats, 1 - enable cheats
        public string Port { get; set; } // CS2 server listen port tcp_udp
        public string RconPort { get; set; } // Use a simple TCP proxy to have RCON listen on an alternative port
        public string MaxPlayers { get; set; } // Max players
        public string AdditionalArgs { get; set; } // Optional additional arguments to pass into cs2
        public string GameAlias { get; set; } // Game type, e.g. casual, competitive, deathmatch
        public string GameType { get; set; } // Used if CS2_GAMEALIAS not defined
        public string GameMode { get; set; } // Used if CS2_GAMEALIAS not defined
        public string MapGroup { get; set; } // Map pool
        public string StartMap { get; set; } // Start map
        public string BotDifficulty { get; set; } // 0 - easy, 1 - normal, 2 - hard, 3 - expert
        public string BotQuota { get; set; } // Number of bots
        public string BotQuotaMode { get; set; } // fill, competitive
        public string TvAutoRecord { get; set; } // Automatically records all games as CSTV demos: 0=off, 1=on
        public string TvEnable { get; set; } // Activates CSTV on server: 0=off, 1=on
        public string TvPort { get; set; } // Host SourceTV port
        public string TvPassword { get; set; } // CSTV password for clients
        public string TvRelayPassword { get; set; } // CSTV password for relay proxies
        public string TvMaxRate { get; set; } // World snapshots to broadcast per second. Affects camera tickrate
        public string TvDelay { get; set; } // Max CSTV spectator bandwidth rate allowed, 0 == unlimited
        public string Log { get; set; } // 'on'/'off'
        public string LogMoney { get; set; } // Turns money logging on/off: (0=off, 1=on)
        public string LogDetail { get; set; } // Combat damage logging: (0=disabled, 1=enemy, 2=friendly, 3=all)
        public string LogItems { get; set; } // Turns item logging on/off: (0=off, 1=on)

        public CS2ServerConfig(dynamic data)
        {

            // Possible null reference assignment.
            // This is ok because we're checking and throwing an exception if the value is null.
#pragma warning disable CS8601
            SrcdsToken = data?.SRCDS_TOKEN;

            if (string.IsNullOrEmpty(SrcdsToken))
            {
                SrcdsToken = Environment.GetEnvironmentVariable("SRCDS_TOKEN");
#pragma warning restore CS8601

                if (string.IsNullOrEmpty(SrcdsToken))
                {
                    throw new ArgumentException("SRCDS_TOKEN is required");
                }
            }


            ServerName = data?.CS2_SERVERNAME ?? "CS2_Game_Server";
            ServerPassword = data?.CS2_PW ?? string.Empty;
            Cheats = data?.CS2_CHEATS ?? "0";
            Port = data?.CS2_PORT ?? "27015";
            RconPort = data?.CS2_RCON_PORT ?? "27050";
            MaxPlayers = data?.CS2_MAXPLAYERS ?? "10";
            AdditionalArgs = data?.CS2_ADDITIONAL_ARGS ?? string.Empty;
            GameAlias = data?.CS2_GAMEALIAS ?? string.Empty;
            GameType = data?.CS2_GAMETYPE ?? "0";
            GameMode = data?.CS2_GAMEMODE ?? "1";
            MapGroup = data?.CS2_MAPGROUP ?? "mg_active";
            StartMap = data?.CS2_STARTMAP ?? "de_inferno";
            BotDifficulty = data?.CS2_BOT_DIFFICULTY ?? string.Empty;
            BotQuota = data?.CS2_BOT_QUOTA ?? string.Empty;
            BotQuotaMode = data?.CS2_BOT_QUOTA_MODE ?? string.Empty;
            TvAutoRecord = data?.TV_AUTORECORD ?? "0";
            TvEnable = data?.TV_ENABLE ?? "0";
            TvPort = data?.TV_PORT ?? "27020";
            TvPassword = data?.TV_PW ?? "changeme";
            TvRelayPassword = data?.TV_RELAY_PW ?? "changeme";
            TvMaxRate = data?.TV_MAXRATE ?? "0";
            TvDelay = data?.TV_DELAY ?? "0";
            Log = data?.CS2_LOG ?? "on";
            LogMoney = data?.CS2_LOG_MONEY ?? "0";
            LogDetail = data?.CS2_LOG_DETAIL ?? "0";
            LogItems = data?.CS2_LOG_ITEMS ?? "0";
        }

        public List<ContainerEnvironmentVariable> ToEnvironmentVariables()
        {
            return new List<ContainerEnvironmentVariable>
            {
                new("SRCDS_TOKEN") { Value = SrcdsToken },
                new("CS2_SERVERNAME") { Value = ServerName },
                new("CS2_PW") { Value = ServerPassword },
                new("CS2_CHEATS") { Value = Cheats },
                new("CS2_PORT") { Value = Port },
                new("CS2_RCON_PORT") { Value = RconPort },
                new("CS2_MAXPLAYERS") { Value = MaxPlayers },
                new("CS2_ADDITIONAL_ARGS") { Value = AdditionalArgs },
                new("CS2_GAMEALIAS") { Value = GameAlias },
                new("CS2_GAMETYPE") { Value = GameType },
                new("CS2_GAMEMODE") { Value = GameMode },
                new("CS2_MAPGROUP") { Value = MapGroup },
                new("CS2_STARTMAP") { Value = StartMap },
                new("CS2_BOT_DIFFICULTY") { Value = BotDifficulty },
                new("CS2_BOT_QUOTA") { Value = BotQuota },
                new("CS2_BOT_QUOTA_MODE") { Value = BotQuotaMode },
                new("TV_AUTORECORD") { Value = TvAutoRecord },
                new("TV_ENABLE") { Value = TvEnable },
                new("TV_PORT") { Value = TvPort },
                new("TV_PW") { Value = TvPassword },
                new("TV_RELAY_PW") { Value = TvRelayPassword },
                new("TV_MAXRATE") { Value = TvMaxRate },
                new("TV_DELAY") { Value = TvDelay },
                new("CS2_LOG") { Value = Log },
                new("CS2_LOG_MONEY") { Value = LogMoney },
                new("CS2_LOG_DETAIL") { Value = LogDetail },
                new("CS2_LOG_ITEMS") { Value = LogItems }
            };
        }
    }
}
