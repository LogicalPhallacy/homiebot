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
        public VoicePersona(IVoiceProvider provider, string name, int CharLimit, VoiceSex sex, int sampleRate = 16000)
        {
            VoiceProvider = provider;
            this.VoiceName = name;
            this.CharLimit = CharLimit;
            this.VoiceSex = sex;
            this.SampleRate = sampleRate;
        }
        public IVoiceProvider VoiceProvider {get;}
        public string VoiceName {get;}
        public int CharLimit {get;}
        public VoiceSex VoiceSex {get;}
        public int SampleRate {get;}
        public override string ToString()
        {
            return $"**{VoiceName}**\n"
            +$"Provider:{VoiceProvider.Name} Character Limit: {CharLimit}\n"
            +$"Presents as: {VoiceSex} at {SampleRate}hz";
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