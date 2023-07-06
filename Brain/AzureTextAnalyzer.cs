using Azure;
using Azure.AI.TextAnalytics;
using Homiebot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Homiebot.Brain;

public class AzureTextAnalyzer : ITextAnalyzer
{
    private readonly TextAnalyticsClient client;
    private readonly IConfiguration configuration;
    private readonly ILogger<HomieBot> logger;
    public AzureTextAnalyzer(ILogger<HomieBot> logger, IConfiguration config)
    {
        this.logger = logger;
        this.configuration = config;
        var azconf = configuration.GetSection("AzureTextAnalyzerConfig").Get<AzureTextAnalyzerConfig>();
        client = new TextAnalyticsClient(new Uri(azconf.Endpoint), new AzureKeyCredential(azconf.ApiKey));
    }

    public async IAsyncEnumerable<string> TLDR(string input)
    {
        logger.LogInformation("Beginning summary operation");
        var summaryOp = await client.StartExtractSummaryAsync(new string[] {input});
        await summaryOp.WaitForCompletionAsync();
        if(summaryOp.Status != TextAnalyticsOperationStatus.Succeeded){
            logger.LogError("Failed to summarize text: {}", summaryOp.Status);
            yield return "Yeah I can't follow that either, sorry";
        }else{
            await foreach (var op in summaryOp.GetValuesAsync())
            {
                logger.LogInformation("Got a result");
                foreach(var result in op){
                    foreach (var sentence in result.Sentences)
                    {
                        yield return sentence.Text;
                    }
                }
            }
        }
    } 


}