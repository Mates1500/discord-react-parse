namespace discord_emote_parse
{
    public class Remap
    {
        public string ReactIdentifier { get; }

        public string[] Remaps { get; }

        public Remap(string reactIdentifier, string[] remaps)
        {
            ReactIdentifier = reactIdentifier;
            Remaps = remaps;
        }
    }

    public class RemapResult
    {
        public string Username { get; }

        public string[] Remaps { get; }

        public RemapResult(string username, string[] remaps)
        {
            Username = username;
            Remaps = remaps;
        }
    }
}