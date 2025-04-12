using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Homiebot.Helpers;
public static class AlwaysSunnyHelper
{
    private const string domain = "iasip.app";
    private const string urlBase = $"https://{domain}/";
    private const string titleEndpoint = "api/title/";
    private static readonly Dictionary<string, string> headers = new(){
        {"authority", domain},
        {"path", $"/{titleEndpoint}"},
        {"scheme", "https"},
        {"accept", "application/json"},
        {"accept-encoding", "gzip, deflate, br, zstd"},
        {"origin", $"https://{domain}"},
        {"priority", "u=1, i"},
        {"referer", "https://iasip.app/no-referrer"},
        {"sec-fetch-dest", "empty"},
        {"sec-fetch-mode", "cors"},
        {"sec-fetch-site","same-origin"},
        {"x-requested-with", "XMLHttpRequest"}
    };
    public static async Task<string> GenerateTitleUrl(string text)
    {
        var cookieContainer = new CookieContainer();
        string crsf = string.Empty;
        // First we need to hit the main endpoint to get our cookies
        using HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(urlBase),
        };
        var response = await client.GetAsync("/");
        if (response.IsSuccessStatusCode)
        {
            // Now we need to parse the response to get the cookie value
            var cookieValue = response.Headers.GetValues("Set-Cookie");
            foreach (var cookie in cookieValue)
            {
                var cookieDef = cookie.Split(';')[0].Split('=');
                if(cookieDef[0].Equals("CSRF-TOKEN", StringComparison.InvariantCultureIgnoreCase))
                {
                    crsf = cookieDef[1];
                }
                cookieContainer.Add(new Cookie(cookieDef[0], cookieDef[1], "/", domain));
            }
            // Now we need to send a very specific POST request.
            // lets add the default headers to the request
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            // Now we need to add the cookie to the request
            // first as the x-crsf-token header
            client.DefaultRequestHeaders.Add("x-csrf-token", crsf);
            // now from the cookie container
            client.DefaultRequestHeaders.Add("cookie", cookieContainer.GetCookieHeader(new Uri(urlBase)));
            // Now we need to send the request
            var request = new HttpRequestMessage(HttpMethod.Post, titleEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(new TitleRequest(text)), System.Text.Encoding.UTF8, "application/json")
            };
            var postResponse = await client.SendAsync(request);
            if (postResponse.IsSuccessStatusCode)
            {
                var responseBody = await postResponse.Content.ReadAsStringAsync();
                var titleResponse = JsonSerializer.Deserialize<TitleResponse>(responseBody);
                if (titleResponse != null)
                {
                    return string.IsNullOrWhiteSpace(titleResponse.key) ? string.Empty : toUrl(titleResponse.key);
                }
            }
        }
        return string.Empty;
    }
    private static string toUrl(string key)
    {
        return $"{urlBase}{key}";
    }
    private record TitleRequest(string text);
    private record TitleResponse(string key, string text);
}