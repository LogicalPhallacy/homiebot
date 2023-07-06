using System.Collections.Generic;

namespace Homiebot.Brain;

public interface ITextAnalyzer
{
    IAsyncEnumerable<string> TLDR(string input);
}