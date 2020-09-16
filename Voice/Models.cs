namespace homiebot.voice 
{
    public class TextToSpeak 
    {
        public string Text {get; set;}
        public VoiceSex VoiceSex {get; set;}
    }
    public enum VoiceSex
    {
        Unspecified,
        Male,
        Female,
        Neutral
    }
    public class VoicePersona
    {
        public VoicePersona(IVoiceProvider provider, string name, int CharLimit, VoiceSex sex, int sampleRate = 16000, VoiceMood voiceMood = VoiceMood.Unavailable)
        {
            VoiceProvider = provider;
            this.VoiceName = name;
            this.CharLimit = CharLimit;
            this.VoiceSex = sex;
            this.SampleRate = sampleRate;
            SemitoneAdjust = 0;
            this.Speed = VoiceSpeed.Normal;
            this.Mood = voiceMood;
        }
        public IVoiceProvider VoiceProvider {get;}
        public string VoiceName {get;}
        public int CharLimit {get;}
        public int SemitoneAdjust {get; set;}
        public VoiceSpeed Speed {get;set;}
        public VoiceMood Mood {get;set;}
        public VoiceSex VoiceSex {get;}
        public int SampleRate {get;}
        public override string ToString()
        {
            var retstr = $"**{VoiceName}**\n"
            +$"Provider:{VoiceProvider.Name} | Character Limit: {CharLimit}\n"
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
    public enum VoiceSpeed
    {
        Normal = 10,
        xslow = 5,
        slow = 8,
        fast = 12,
        xfast = 15
    }
    public enum VoiceMood
    {
        Unavailable,
        newscastformal,
        newscastcasual,
        customerservice,
        chat,
        cheerful,
        empathetic
    }
}