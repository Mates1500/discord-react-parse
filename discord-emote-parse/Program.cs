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
    class Program
    {
        private readonly DiscordSocketClient _client;
        private IConfiguration _config;
        private static ulong _requestedMessageId;
        private static Dictionary<string, string[]> _remaps;
        private static ulong[] _ignoreUserIds;
        private static ulong _channelId;

        static void Main(string[] args)
        {
            _channelId = ulong.Parse(args[0]);
            _requestedMessageId = ulong.Parse(args[1]);

            _remaps = new Dictionary<string, string[]>();

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {

            _config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddJsonFile("config.secret.json")
                .Build();

            _ignoreUserIds = _config.GetSection("IgnoreUserIds").Get<ulong[]>();

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

            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
        }

        private Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }

        private void CreateCsvFile(List<RemapResult> results, string filename)
        {
            var maxNumberOfRemaps = results.OrderBy(x => x.Remaps.Length).Select(x => x.Remaps.Length).First();
            using (var f = new StreamWriter(filename, true))
            {
                var firstLine = new StringBuilder("Name");
                for (var i = 0; i < maxNumberOfRemaps; i++)
                {
                    firstLine.Append($",Remap {i+1}");
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

            var guild = _client.GetGuild(ulong.Parse(_config["GuildId"]));
            var textChannel = guild.GetTextChannel(_channelId);
            var requestedMessage = await textChannel.GetMessageAsync(_requestedMessageId);

            var result = new List<RemapResult>();

            foreach (var r in requestedMessage.Reactions)
            {
                var users = await requestedMessage.GetReactionUsersAsync(r.Key, 100).FlattenAsync();

                var emoteKey = r.Key.ToString();

                if (!_remaps.ContainsKey(emoteKey))
                    continue;

                foreach (var u in users)
                {
                    if (!_ignoreUserIds.Contains(u.Id))
                        result.Add(new RemapResult(u.Username, _remaps[emoteKey]));
                }
            }

            CreateCsvFile(result, _config["ResultFileLocation"]);

            //Console.ReadLine();
        }

        public async Task MainAsync()
        {
            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, _config["BotSecret"]);
            await _client.StartAsync();


            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }
    }
}
