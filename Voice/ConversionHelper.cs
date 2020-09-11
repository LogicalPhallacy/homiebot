using System;
using System.IO;
using NAudio;
using NAudio.Wave;
using System.Collections.Generic;
using NAudio.Wave.Compression;
using Microsoft.Extensions.Logging;

namespace homiebot.voice
{
    public static class AudioConversionHelper
    {
        public static int OutSampleRate = 48000;
        public static int OutChannels = 1;
        public static void ConvertForDiscord(byte[] audiodata, int sampleRate, int channels, Stream discordTarget,ILogger logger)
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
                    logger.LogInformation("Resampling");
                    var convertedBytes = resampleStream.Convert(copyamount, out sourceBytesConverted);
                    if (sourceBytesConverted != copyamount)
                    {
                        logger.LogError("Resample didn't produce correct bytestream");
                        break;
                    }
                    discordTarget.Write(resampleStream.DestBuffer);
                    offset += copyamount;
                    remaining = (audiodata.Length - offset);
                }
            }
            else
            {
                Buffer.BlockCopy(audiodata,0,resampleStream.SourceBuffer,0,audiodata.Length);
                int sourceBytesConverted = 0;
                logger.LogInformation("Resampling");
                var convertedBytes = resampleStream.Convert(audiodata.Length, out sourceBytesConverted);
                if (sourceBytesConverted != audiodata.Length)
                {
                    logger.LogError("Resample didn't produce correct bytestream");
                }
                discordTarget.Write(resampleStream.DestBuffer);
            }
        }
    }
}