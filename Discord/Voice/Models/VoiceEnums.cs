namespace Homiebot.Discord.Voice.Models
{
    public enum VoiceSex
    {
        Unspecified,
        Male,
        Female,
        Neutral
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