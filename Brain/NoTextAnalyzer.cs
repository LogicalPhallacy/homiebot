using System.Collections.Generic;

namespace Homiebot.Brain;

public class NoTextAnalyzer : ITextAnalyzer {
    public NoTextAnalyzer() {}

    public async IAsyncEnumerable<string> TLDR(string input)
    {
        yield return "IDK homie I'm not reading that shit";
    }
}