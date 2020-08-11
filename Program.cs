using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace homiebot
{
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            return new ArraySegment<T>(array, offset, length)
                        .ToArray();
        }
    }
    public class BotConfig
    {
        public string DiscordToken {get; set;}
    }
    class Program
    {
        public static Random random;
        public static ChatReplacer chatReplacer;
        private static DiscordHelper discord;
        private static BotConfig config;
        static async Task<int> Main(string[] args)
        {
            Log("Homiebot starting up");
            config = JsonSerializer.Deserialize<BotConfig>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"BotConfig.json")));
            Log("Parsing BotConfig");
            discord = new DiscordHelper(config.DiscordToken);
            Log("Adding exit handler");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);
            Log("Initializing RNG");
            random = new Random();
            Log("Loading Chat Replacer");
            chatReplacer = new ChatReplacer();
            chatReplacer.Initialize();
            Log("Initializing Discord");
            await discord.Initialize(chatReplacer);
            discord.RegisterCommands();
            Log("Discord Initialized. Bot Running. Ctrl-C to Exit");
            await Task.Delay(-1);
            Log("Exiting");
            return await Exit();
        }

        protected static void Exit(object sender, ConsoleCancelEventArgs args)
        {
            Log("Kill signal recieved, signing off and shutting down");
            Exit().Wait();
        }

        protected static async Task<int> Exit()
        {
            if(discord.Connected)
            {
                Log("Logging off discord");
                await discord.Disconnect();
            }
            return 0;
        }

        public static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()}-{DateTime.Now.ToShortTimeString()}: {message}");
        }
    }
}
