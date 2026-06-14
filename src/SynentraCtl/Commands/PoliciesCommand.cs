using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Synentra.Client.Abstractions;
using SynentraCtl.Core.Services.Logger;

namespace SynentraCtl.Commands;

internal static class PoliciesCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("policies", "Browse Synentra governance policies");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateDetailsCommand(serviceProvider));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var pageOption = CommandHelpers.CreatePageOption();
        var pageSizeOption = CommandHelpers.CreatePageSizeOption();
        var outputOption = CommandHelpers.CreateOutputOption();

        var cmd = new Command("list", "List all governance policies")
        {
            pageOption,
            pageSizeOption,
            outputOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var policies = await client.Policies.ListAsync(
                parseResult.GetValue(pageOption),
                parseResult.GetValue(pageSizeOption), ct);
            logger.Write(policies, parseResult.GetValue(outputOption));
        }));

        return cmd;
    }

    private static Command CreateDetailsCommand(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string>("--name") { Description = "Policy name", Required = true };
        var outputOption = CommandHelpers.CreateOutputOption();

        var cmd = new Command("details", "Show full details of a specific policy")
        {
            nameOption,
            outputOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var policy = await client.Policies.GetAsync(parseResult.GetValue(nameOption)!, ct);
            logger.Write(policy, parseResult.GetValue(outputOption));
        }));

        return cmd;
    }
}
