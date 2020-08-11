using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
namespace homiebot 
{
    public class DiscordHelper
    {
        private string token;
        private bool connected;
        private DiscordClient discordClient;
        private ChatReplacer chatReplacer;
        public bool Connected {
            get => connected;
        }
        public DiscordHelper(string token)
        {
            this.token = token;
            connected = false;
        }

        public void ReloadConfig()
        {
            chatReplacer.Initialize();
        }

        public async Task Initialize(ChatReplacer chatReplacer)
        {
            this.chatReplacer = chatReplacer;
            discordClient = new DiscordClient( new DiscordConfiguration(){
                AutoReconnect = true,
                Token = token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
            });
            try
            {
                await discordClient.ConnectAsync();
                connected = true;
            }
            catch(Exception e)
            {
                Program.Log($"Exception trying to connect: {e.Message}");
            }
        }

        public void RegisterCommands()
        {
            discordClient.MessageCreated+= async e=> {
                if(e.Message.Content.ToLower().StartsWith("!"))
                {
                    if(e.Message.Content.ToLower() == "reload"){this.ReloadConfig(); return;}
                    string[] trimmedcommands = e.Message.Content.ToLower().TrimStart('!').Split(' ');
                    if(trimmedcommands.Length>1){
                        await e.Message.RespondAsync(chatReplacer.Replace(trimmedcommands[0],trimmedcommands.SubArray(1,trimmedcommands.Length-1)));
                    }else{
                        await e.Message.RespondAsync(chatReplacer.Replace(trimmedcommands[0]));
                    }
                }
            };
        }

        public async Task Disconnect()
        {
            if(connected)
            {
                await discordClient.DisconnectAsync();
                connected = false;
            }
        }

        public async Task<bool> ReConnect()
        {
            try
            {
                await discordClient.ReconnectAsync();
                return true;
            }
            catch(Exception e)
            {
                Program.Log($"Exception trying to reconnect: {e.Message}");
                return false;
            }
            
        }

    }
}