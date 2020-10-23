using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace discord_emote_parse
{
    public class DiscordEmoteParser
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly ulong _requestedMessageId;
        private readonly Dictionary<string, string[]> _remaps;
        private readonly ulong[] _ignoreUserIds;
        private readonly ulong _channelId;
        private readonly SemaphoreSlim _endProgram;


        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddJsonFile("config.secret.json")
                .Build();
        }

        private void BindRemaps()
        {
            var remaps = _config.GetSection("ReactRemaps")
                .GetChildren()
                .ToList()
                .Select(x => new Remap(
                    x.GetValue<string>("ReactId"),
                    x.GetSection("Remaps").Get<string[]>()));

            foreach (var r in remaps)
            {
                _remaps.Add(r.ReactIdentifier, r.Remaps);
            }
        }

        public DiscordEmoteParser(ulong channelId, ulong requestedMessageId)
        {
            _channelId = channelId;
            _requestedMessageId = requestedMessageId;

            _config = BuildConfig();
            _endProgram = new SemaphoreSlim(0, 1);

            _ignoreUserIds = _config.GetSection("IgnoreUserIds").Get<ulong[]>();


            _remaps = new Dictionary<string, string[]>();
            BindRemaps();

            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
        }

        private static Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }

        private async Task<List<RemapResult>> GetReactionRemapsForMessage(ulong guildId, ulong channelId, ulong messageId)
        {
            var guild = _client.GetGuild(guildId);
            var textChannel = guild.GetTextChannel(channelId);
            var requestedMessage = await textChannel.GetMessageAsync(messageId);

            var result = new List<RemapResult>();

            foreach (var r in requestedMessage.Reactions)
            {
                var users = await requestedMessage.GetReactionUsersAsync(r.Key, 100).FlattenAsync();

                var emoteKey = r.Key.ToString();

                if (!_remaps.ContainsKey(emoteKey))
                    continue;

                result.AddRange(from u in users where !_ignoreUserIds.Contains(u.Id) select new RemapResult(u.Username, _remaps[emoteKey]));
            }

            return result;
        }

        private static void CreateCsvFile(List<RemapResult> results, string filename)
        {
            var maxNumberOfRemaps = results.OrderByDescending(x => x.Remaps.Length).Select(x => x.Remaps.Length).First();
            using (var f = new StreamWriter(filename, true))
            {
                var firstLine = new StringBuilder("Name");
                for (var i = 0; i < maxNumberOfRemaps; i++)
                {
                    firstLine.Append($",Remap {i + 1}");
                }
                f.WriteLine(firstLine);
                foreach (var r in results)
                {
                    var currentLine = new StringBuilder(r.Username);
                    foreach (var remap in r.Remaps)
                    {
                        currentLine.Append($",{remap}");
                    }
                    f.WriteLine(currentLine);
                }
            }
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine("Connected!");

            try
            {
                var result = await GetReactionRemapsForMessage(ulong.Parse(_config["GuildId"]), _channelId,
                    _requestedMessageId);

                CreateCsvFile(result, _config["ResultFileLocation"]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} - {e.StackTrace}");
            }

            _endProgram.Release();
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config["BotSecret"]);
            await _client.StartAsync();

            await _endProgram.WaitAsync();
        }
    }
}