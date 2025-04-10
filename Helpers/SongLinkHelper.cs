using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Homiebot.Models;

namespace Homiebot.Helpers;
public class SongLinkHelper
{
    public const string apiEndPoint = "https://api.song.link/v1-alpha.1/links?url=";
    private static JsonSerializerOptions options = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    public static List<string> SupportedUrlBases = new() {
        "https://open.spotify.com/",
        "https://music.apple.com/",
        "https://www.youtube.com/",
        "https://music.youtube.com/",
        "https://play.google.com/music/",
        "https://www.pandora.com/",
        "https://www.deezer.com/",
        "https://deezer.page.link/",
        "https://music.amazon.com/",
        "https://listen.tidal.com/",
        "http://napster.com/",
        "https://music.yandex.ru/",
        "https://music.apple.com/",
        "https://play.google.com/store/music",
        "https://www.amazon.com/Kitchen/"
    };
    public static bool TestMessageContainsKnownProviderLink(string message)
    {
        if(message.Contains("https://")){
            foreach(string supported in SupportedUrlBases)
            {
                if(message.Contains(supported, System.StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }
        return false;
    }
    public static string GetLinkFromMessage(string message)
    {
        foreach(var substr in message.Split()){
            if(substr.StartsWith("https", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                foreach(string supported in SupportedUrlBases)
                {
                    if(substr.StartsWith(supported, true, System.Globalization.CultureInfo.InvariantCulture)){
                        return substr;
                    }
                }
            }
        }
        return string.Empty;
    }
    public static async Task<SongLink> GetSongLink(string url)
    {
        var myLink = apiEndPoint + url;
        using var client = new HttpClient();
        var resp = await client.GetAsync(new System.Uri(myLink));
        resp.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync<SongLink>(await resp.Content.ReadAsStreamAsync(), options);
    } 
}