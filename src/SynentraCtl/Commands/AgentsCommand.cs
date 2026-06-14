using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Synentra.Client.Abstractions;
using Synentra.Client.Models.Agents;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class AgentsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("agents", "Manage AI agents registered in Vectra");

        command.Subcommands.Add(CreateListCommand(serviceProvider));
        command.Subcommands.Add(CreateRegisterCommand(serviceProvider));
        command.Subcommands.Add(CreateAssignPolicyCommand(serviceProvider));
        command.Subcommands.Add(CreateLiftQuarantineCommand(serviceProvider));
        command.Subcommands.Add(CreateDeleteCommand(serviceProvider));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var pageOption = CommandHelpers.CreatePageOption();
        var pageSizeOption = CommandHelpers.CreatePageSizeOption();
        var outputOption = CommandHelpers.CreateOutputOption();

        var cmd = new Command("list", "List all registered AI agents")
        {
            pageOption,
            pageSizeOption,
            outputOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var agents = await client.Agents.ListAsync(
                parseResult.GetValue(pageOption),
                parseResult.GetValue(pageSizeOption), ct);
            logger.Write(agents, parseResult.GetValue(outputOption));
        }));

        return cmd;
    }

    private static Command CreateLiftQuarantineCommand(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id") { Description = "Agent ID (GUID)", Required = true };

        var cmd = new Command("lift-quarantine", "Lift quarantine mode for an AI agent")
        {
            agentIdOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            await client.Agents.LiftQuarantineAsync(parseResult.GetValue(agentIdOption)!, ct);
            logger.Write("Quarantine lifted successfully.");
        }));

        return cmd;
    }

    private static Command CreateRegisterCommand(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string>("--name") { Description = "Agent name", Required = true };
        var ownerOption = new Option<string>("--owner") { Description = "Owner / team identifier", Required = true };
        var secretOption = new Option<string>("--secret") { Description = "Client secret for the agent", Required = true };

        var cmd = new Command("register", "Register a new AI agent")
        {
            nameOption,
            ownerOption,
            secretOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            var result = await client.Agents.RegisterAsync(new RegisterAgentRequest
            {
                Name = parseResult.GetValue(nameOption)!,
                OwnerId = parseResult.GetValue(ownerOption)!,
                ClientSecret = parseResult.GetValue(secretOption)!
            }, ct);
            logger.Write(result);
        }));

        return cmd;
    }

    private static Command CreateAssignPolicyCommand(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id") { Description = "Agent ID (GUID)", Required = true };
        var policyOption = new Option<string>("--policy") { Description = "Policy name to assign", Required = true };

        var cmd = new Command("assign-policy", "Assign a policy to an agent")
        {
            agentIdOption,
            policyOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            await client.Agents.AssignPolicyAsync(
                parseResult.GetValue(agentIdOption),
                new AssignPolicyRequest { PolicyName = parseResult.GetValue(policyOption)! },
                ct);
            logger.Write("Policy assigned successfully.");
        }));

        return cmd;
    }

    private static Command CreateDeleteCommand(IServiceProvider serviceProvider)
    {
        var agentIdOption = new Option<Guid>("--agent-id") { Description = "Agent ID (GUID)", Required = true };

        var cmd = new Command("delete", "Delete an AI agent")
        {
            agentIdOption
        };

        cmd.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();
            await client.Agents.DeleteAsync(parseResult.GetValue(agentIdOption), ct);
            logger.Write("Agent deleted.");
        }));

        return cmd;
    }
}
