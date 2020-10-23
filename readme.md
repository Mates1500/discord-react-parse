# Discord react parse
This utility parses all reacts bound to a message in a text channel (input args) and allows the user to remap them to custom groups defined in the config. The output is a csv file remapping usernames from the reacts rebound to the custom groups.

# How to use
1. Create `config.secret.json` in the root of the CSharp project (`discord-emote-parse/`)
2. Fill it with the bot secret
```json
{
    "BotSecret": "GO TO https://discord.com/developers/applications -> Your App -> Bot -> Click to Reveal Token. Paste here"
}
```
3. Set your `GuildId`, `IgnoreUserIds` (for example a bot that posts the message), `ResultFileLocation` (output csv) and custom Emote -> Groups remaps. Remaps are an array of `string`s, and are not limited by maximum length, neither the length has to be universal. Use as many remaps as you want on any group.
4. Run with `dotnet run` or F5 in Visual Studio
