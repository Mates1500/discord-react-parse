namespace discord_emote_parse
{
    static class Program
    {
        static void Main(string[] args)
        {
            var channelId = ulong.Parse(args[0]);
            var requestedMessageId = ulong.Parse(args[1]);

            new DiscordEmoteParser(channelId, requestedMessageId).MainAsync().GetAwaiter().GetResult();
        }
    }
}
