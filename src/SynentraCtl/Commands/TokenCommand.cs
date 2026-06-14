using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Synentra.Client.Abstractions;
using Synentra.Client.Models.Tokens;
using SynentraCtl.Core.Services.Logger;

namespace SynentraCtl.Commands;

internal static class TokenCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id")
        {
            Description = "Agent ID (GUID)",
            Required = true,
        };

        var secretOption = new Option<string>("--secret")
        {
            Description = "Agent client secret",
            Required = true
        };

        var outputOption = CommandHelpers.CreateOutputOption();

        var command = new Command("token", "Exchange agent credentials for a JWT bearer token")
        {
            agentIdOption,
            secretOption,
            outputOption
        };

        command.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var result = await client.Tokens.GenerateAsync(new GenerateTokenRequest
            {
                AgentId = parseResult.GetValue(agentIdOption),
                ClientSecret = parseResult.GetValue(secretOption)!
            }, ct);
            logger.Write(result, parseResult.GetValue(outputOption));
        }));

        return command;
    }
}

