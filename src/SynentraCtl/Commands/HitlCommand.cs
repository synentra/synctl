using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Synentra.Client.Abstractions;
using Synentra.Client.Models.Hitl;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class HitlCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("hitl", "Manage Human-in-the-Loop (HITL) requests");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateStatusCommand(serviceProvider));
        command.Subcommands.Add(CreateApproveCommand(serviceProvider));
        command.Subcommands.Add(CreateDenyCommand(serviceProvider));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var pageOption = CommandHelpers.CreatePageOption();
        var pageSizeOption = CommandHelpers.CreatePageSizeOption();
        var outputOption = CommandHelpers.CreateOutputOption();

        var cmd = new Command("list", "List all pending HITL requests")
        {
            pageOption,
            pageSizeOption,
            outputOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var items = await client.Hitl.GetAllPendingAsync(
                parseResult.GetValue(pageOption),
                parseResult.GetValue(pageSizeOption), ct);
            logger.Write(items, parseResult.GetValue(outputOption));
        }));

        return cmd;
    }

    private static Command CreateStatusCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var outputOption = CommandHelpers.CreateOutputOption();

        var cmd = new Command("status", "Get the status of a HITL request")
        {
            idOption,
            outputOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var status = await client.Hitl.GetStatusAsync(parseResult.GetValue(idOption)!, ct);
            logger.Write(status, parseResult.GetValue(outputOption));
        }));

        return cmd;
    }

    private static Command CreateApproveCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var commentOption = new Option<string?>("--comment") { Description = "Optional reviewer comment" };

        var cmd = new Command("approve", "Approve a pending HITL request")
        {
            idOption,
            commentOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            await client.Hitl.ApproveAsync(
                parseResult.GetValue(idOption)!,
                new ReviewDecisionRequest { Comment = parseResult.GetValue(commentOption) },
                ct);
            logger.Write("HITL request approved.");
        }));

        return cmd;
    }

    private static Command CreateDenyCommand(IServiceProvider serviceProvider)
    {
        var idOption = new Option<string>("--id") { Description = "HITL request ID", Required = true };
        var commentOption = new Option<string?>("--comment") { Description = "Optional reviewer comment" };

        var cmd = new Command("deny", "Deny a pending HITL request")
        {
            idOption,
            commentOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            await client.Hitl.DenyAsync(
                parseResult.GetValue(idOption)!,
                new ReviewDecisionRequest { Comment = parseResult.GetValue(commentOption) },
                ct);
            logger.Write("HITL request denied.");
        }));

        return cmd;
    }
}

