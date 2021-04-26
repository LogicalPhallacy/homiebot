using System;
using System.IO;
using NAudio;
using NAudio.Wave;
using System.Collections.Generic;
using NAudio.Wave.Compression;
using Microsoft.Extensions.Logging;
using DSharpPlus.VoiceNext;
using System.Threading.Tasks;
using Homiebot.Discord.Voice.Models;

namespace Homiebot.Discord.Voice
{
    public static class SSMLConversionHelper
    {
        public const string langstring = "en";
        public const string langcodestring = "en-US";

        public static string CreateSSMLString(VoicePersona voice, string text)
        {
            return WrapWholeElement(voice,WrapMood(voice,WrapProsody(voice,EscapeXML(text))));
        }

        private static string WrapWholeElement(VoicePersona voice, string text)
        {
            string template = $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"{langcodestring}\">"
                +$"<voice name=\"{voice.VoiceName}\">";
            template += text;
            template += "</voice>"+"</speak>";
            return template;
        }
        private static string WrapMood(VoicePersona voice, string text)
        {
            if(voice.Mood != VoiceMood.Unavailable)
            {
                string template = $"<mstts:express-as style=\"{voice.Mood}\">";
                template += text;
                template +="</mstts:express-as>";
                return template;
            }
            return text;
        }
        private static string WrapProsody(VoicePersona voice, string text)
        {
            if(voice.SemitoneAdjust == 0 && voice.Speed == VoiceSpeed.Normal){
                return text;
            }
            string template = "<prosody";
            if(voice.SemitoneAdjust != 0){
                template +=$" pitch=\"{voice.SemitoneAdjust}st\"";    
            }
            if(voice.Speed != VoiceSpeed.Normal)
            {
                string voicestring = voice.Speed.ToString();
                voicestring = voicestring.Replace("x","x-");
                template += $" rate=\"{voicestring}\"";
            }
            template+=">";
            template += text;
            template += "</prosody>";
            return template;
        }
        private static string EscapeXML(string text){
            return System.Security.SecurityElement.Escape(text);
        }
    }
    public static class AudioConversionHelper
    {
        public static int OutSampleRate = 48000;
        public static int OutChannels = 1;
        public static async Task ConvertForDiscord(byte[] audiodata, int sampleRate, int channels, VoiceTransmitSink discordTarget,ILogger logger)
        {
            var resampleStream = new AcmStream(new WaveFormat(sampleRate, 16, 1), new WaveFormat(OutSampleRate, 16, 1));
            
            if(audiodata.Length > resampleStream.SourceBuffer.Length)
            {
                int offset = 0;
                logger.LogInformation("Large audio returned, the copy will need to be streamed in");
                int remaining = (audiodata.Length - offset);
                while (remaining > 0)
                {
                    Array.Clear(resampleStream.SourceBuffer,0,resampleStream.SourceBuffer.Length);
                    Array.Clear(resampleStream.DestBuffer,0,resampleStream.DestBuffer.Length);
                    int copyamount = remaining > resampleStream.SourceBuffer.Length ? resampleStream.SourceBuffer.Length : remaining;
                    Buffer.BlockCopy(audiodata,offset,resampleStream.SourceBuffer,0,copyamount);
                    int sourceBytesConverted = 0;
                    // logger.LogInformation("Resampling");
                    var convertedBytes = resampleStream.Convert(copyamount, out sourceBytesConverted);
                    if (sourceBytesConverted != copyamount)
                    {
                        logger.LogError("Resample didn't produce correct bytestream");
                        break;
                    }
                    await discordTarget.WriteAsync(resampleStream.DestBuffer);
                    offset += copyamount;
                    remaining = (audiodata.Length - offset);
                }
            }
            else
            {
                Buffer.BlockCopy(audiodata,0,resampleStream.SourceBuffer,0,audiodata.Length);
                int sourceBytesConverted = 0;
                // logger.LogInformation("Resampling");
                var convertedBytes = resampleStream.Convert(audiodata.Length, out sourceBytesConverted);
                if (sourceBytesConverted != audiodata.Length)
                {
                    logger.LogError("Resample didn't produce correct bytestream");
                }
                await discordTarget.WriteAsync(resampleStream.DestBuffer);
            }
        }
    }
}