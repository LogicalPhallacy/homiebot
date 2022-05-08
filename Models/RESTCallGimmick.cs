using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Homiebot.Models
{
    public class RESTGimmick
    {
        public string TriggerString {get;set;}
        public string PreMessage {get;set;}
        public string PostMessage {get;set;} = "";
        public string CustomFailureMessage {get;set;} = "";
        public string Endpoint {get; set;}
        public SupportedRESTMethod Method {get;set;}
        public string? Body{get;set;} = null;
        public int TimeoutSeconds {get;set;} = 300;
        public bool JsonResponse {get;set;} = false;

        private const string responseValueToken = "@responseMessage";
        private const string responseCodeToken = "@responseCode";

        private string regexifyParameter(string parameter)
            =>$"(?<{(parameter.Replace("@",""))}>" + @"[a-z,0-9,/]+\b)";

        private bool expectsParameters;
        private Regex messageRegex;
        private Match? lastMessageMatch;
        private int? lastResponseStatus;
        private string? responseString;
        private dynamic? jsonResponseObject;
        private Regex jsonDynamicRegex;
        private IEnumerable<string> parameterList;
        private bool busy;

        public bool IsBusy => busy;

        public void Initialize()
        {
            expectsParameters = TriggerString.Contains('@');
            parameterList = expectsParameters ?
                populateParameterList() :
                    new List<string>();
            messageRegex = expectsParameters ?
                new Regex(
                    regexifiedString(),
                    (RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ) :
                null;
            busy = false;
            if(JsonResponse)
                jsonDynamicRegex = new Regex(@"(@\([a-z,\.]+\))+", (RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
        }
        public bool ShouldTriggerGimmick(string message) 
        {
            if(expectsParameters)
            {
                var match = messageRegex.Match(message);
                if(match.Success){
                    lastMessageMatch = match;
                    return true;
                }else{
                    //lastMessageMatch = null;
                    return false;
                }
            }
            return message.Contains(TriggerString);
        }
        public Uri GetDestinationEndpoint => new Uri(parameterReplaceString(Endpoint));
        public string FormatPreMessage => parameterReplaceString(PreMessage);
        public async Task<bool> RunRequest()
        {
            using HttpClient client = new HttpClient();
            busy = true;
            var resp = Method switch
            {
                SupportedRESTMethod.GET => await client.GetAsync(GetDestinationEndpoint),
                _ => null
            };
            lastResponseStatus = (int)resp.StatusCode;
            if(resp.IsSuccessStatusCode)
            {
                if(JsonResponse)
                    jsonResponseObject = await System.Text.Json.JsonSerializer.DeserializeAsync<dynamic>(await resp.Content.ReadAsStreamAsync());
                else
                    responseString = await resp.Content.ReadAsStringAsync();
                busy = false;
                return true;
            }
            busy = false;
            return false;
        }
        public string FormatPostMessage => parameterReplaceString(PostMessage);
        public string FormatCustomFailure => parameterReplaceString(CustomFailureMessage);
        public void FinishGimmick()
        {
            lastResponseStatus = null;
            lastMessageMatch = null;
            responseString = null;
            jsonResponseObject = null;
        }
        
        private string parameterReplaceString(string input)
        {
            if(expectsParameters){
                string replacement = input;
                foreach(var param in ExtractParametersFromMatch()){
                    replacement = replacement.Replace(param.Key, param.Value);
                }
                return replacement;
            }
            return input;
        }
        public string GetPostMessage
            => JsonResponse? 
                PostMessage.Replace(responseValueToken, responseString) :
                // Implement the dynamic parse and run the method
                PostMessage;
        public string GetFailMessage
            => string.IsNullOrEmpty(CustomFailureMessage) ? 
                $"Http call failed with code {(lastResponseStatus?.ToString() ?? "unknown")}" :
                CustomFailureMessage.Replace(responseCodeToken, lastResponseStatus?.ToString() ?? "");
        
        private string PopulateJsonResponse() {
            string ret = PostMessage;
            var match = jsonDynamicRegex.Match(PostMessage);
            if(match.Success){
                var dict = jsonResponseObject as Dictionary<string,object>;
                foreach(var g in match.Groups.Values)
                {
                    var sanitize = g.Value.Replace("@(","").Replace(")","");
                    ret = ret.Replace(g.Value, sanitize);
                }
            }
            return ret;
        }
        private string regexifiedString() {
            string ret = TriggerString;
            foreach(var param in parameterList){
                ret = ret.Replace(param, regexifyParameter(param));
            }
            return ret;
        }
        private IEnumerable<string> populateParameterList() 
        {
            foreach (string found in TriggerString.Split())
            {
                if(found.StartsWith('@'))
                    yield return found;
            }
        }

        private IEnumerable<KeyValuePair<string,string>> ExtractParametersFromMatch()
        {
            foreach(var param in parameterList)
            {
                yield return new KeyValuePair<string,string>(
                    param,
                    lastMessageMatch?.Groups[param.Replace("@","")].Value
                );
            }
        }

    }
    public enum SupportedRESTMethod 
    {
        GET,
        //POST,
        //PUT,
        //P ATCH
    }
}