using System.CommandLine;

namespace VectraCtl.Commands;

internal static class VectraCommandLine
{
    public static RootCommand Create(IServiceProvider serviceProvider, string[] args)
    {
        var rootCommand = new RootCommand("VectraCtl – CLI tool for Vectra (Intent-Aware Governance Gateway for Autonomous AI Agents)");

        rootCommand.Subcommands.Add(AgentsCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(HitlCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(InitCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(PoliciesCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(RunCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(StopCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(TokenCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(UninstallCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(UpdateCommand.Create(serviceProvider));

        return rootCommand;
    }
}
