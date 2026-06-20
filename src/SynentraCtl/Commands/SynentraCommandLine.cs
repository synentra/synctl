using System.CommandLine;

namespace SynentraCtl.Commands;

internal static class SynentraCommandLine
{
    public static RootCommand Create(IServiceProvider serviceProvider, string[] args)
    {
        var rootCommand = new RootCommand("SynCtl – CLI tool for Synentra (Intent-Aware Governance Gateway for Autonomous AI Agents)");

        rootCommand.Subcommands.Add(AgentsCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(HitlCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(InitCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(PoliciesCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(ProxyCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(RunCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(StopCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(TokenCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(UninstallCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(UpdateCommand.Create(serviceProvider));

        return rootCommand;
    }
}
