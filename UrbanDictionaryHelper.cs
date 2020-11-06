using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.Entities;
namespace homiebot
{
    public class UrbanDictionaryHelper
    {
        private static Uri baseUri = new Uri("http://api.urbandictionary.com/v0/");
        private static String getDefineString(string term)
        {
            return $"define?term={term}";
        }
        public static async Task<DiscordEmbed> GetDefinition(string term)
        {
            using(var client = new HttpClient())
            {
                client.BaseAddress = baseUri;
                var respstring = await client.GetStringAsync(getDefineString(term));
                var resp = JsonSerializer.Deserialize<UrbanDictionaryResponse>(respstring);
                if(resp.list != null && resp.list.Count() > 0)
                {
                    var pick = resp.list.OrderByDescending(d=> d.thumbs_up).FirstOrDefault();
                    return embedDef(pick); 
                }else{
                    throw new Exception($"No definition found for term: {term}");
                }
            }
            
        }
        private static string formatDefinition(UrbanDictionaryDefinition definition)
        {
            string outstr = $"**{definition.word}**\n";
            outstr += $"Definition: {definition.definition.Replace("[","").Replace("]","")}\n";
            outstr += $"{definition.author} - {definition.written_on}\n";
            outstr += $"[Link]({definition.permalink}) - {definition.thumbs_up}👍/{definition.thumbs_down}👎";
            return outstr;
        }

        private static DiscordEmbed embedDef(UrbanDictionaryDefinition definition)
        {
            var embedbuilder = new DiscordEmbedBuilder();
            embedbuilder.WithTitle(definition.word)
            .WithAuthor($"{definition.word} - Urban Dictionary",definition.permalink)
            .WithDescription(definition.definition.Replace("[","").Replace("]",""));
            embedbuilder.AddField("Written on",definition.written_on.ToShortDateString(),true);
            embedbuilder.AddField("Written by", definition.author);
            embedbuilder.AddField("Ratio",$"{definition.thumbs_up}👍/{definition.thumbs_down}👎",true);
            return embedbuilder.Build();
        }
    }
    class UrbanDictionaryDefinition
    {
        public string definition{get;set;}
        public string permalink{get;set;}
        public int thumbs_up{get;set;}
        public IEnumerable<string> sound_urls{get;set;}
        public string author{get;set;}
        public string word{get;set;}
        public int defid{get;set;}
        public DateTime written_on{get;set;}
        public string example{get;set;}
        public int thumbs_down{get;set;}

    }
    class UrbanDictionaryResponse
    {
        public IEnumerable<UrbanDictionaryDefinition> list{get;set;}
    }
}