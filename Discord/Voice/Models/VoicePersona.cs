using Homiebot.Discord.Voice.Providers;

namespace Homiebot.Discord.Voice.Models
{
    public class VoicePersona
    {
        public VoicePersona(IVoiceProvider provider, string voiceId, int CharLimit, VoiceSex sex, int sampleRate = 16000, VoiceMood voiceMood = VoiceMood.Unavailable, string description = "")
        {
            VoiceProvider = provider;
            this.VoiceName = voiceId;
            this.VoiceDescription = description;
            this.CharLimit = CharLimit;
            this.VoiceSex = sex;
            this.SampleRate = sampleRate;
            SemitoneAdjust = 0;
            this.Speed = VoiceSpeed.Normal;
            this.Mood = voiceMood;
        }
        public IVoiceProvider VoiceProvider {get;}
        public string VoiceName {get;}
        public string VoiceDescription {get;}
        public int CharLimit {get;}
        public int SemitoneAdjust {get; set;}
        public VoiceSpeed Speed {get;set;}
        public VoiceMood Mood {get;set;}
        public VoiceSex VoiceSex {get;}
        public int SampleRate {get;}
        public string DisplayName => $"{VoiceName}{(string.IsNullOrWhiteSpace(VoiceDescription) ? "" : $" - {VoiceDescription}")}";
        public string SearchDisplayName => $"{VoiceProvider.Name} - {DisplayName}";
        public string SearchName => $"{VoiceName} {VoiceDescription}";
        public override string ToString()
        {
            var retstr = $"**{VoiceName}**\n";
            if(!string.IsNullOrWhiteSpace(VoiceDescription))
                retstr += $"{VoiceDescription}\n";
            retstr+=$"Provider:{VoiceProvider.Name} | Character Limit: {CharLimit}\n"
            +$"Presents as: {VoiceSex} at {SampleRate}hz | Pitch Adjust: {SemitoneAdjust} | Speed: {Speed}";
            if(Mood != VoiceMood.Unavailable){
                retstr+= $"\nMood: {Mood}";
            }
            return retstr;
        }
        public override bool Equals(object obj)
        {
            if(obj.GetType() == typeof(string))
            {
                return this.VoiceName.ToLower() == ((string)obj).ToLower();
            }
            return ((VoicePersona)obj).VoiceName.ToLower() == this.VoiceName.ToLower();
        }
    }
}