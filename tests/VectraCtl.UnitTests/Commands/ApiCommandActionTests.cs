using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.CommandLine;
using Vectra.Client.Abstractions;
using Vectra.Client.Models.Agents;
using Vectra.Client.Models.Hitl;
using Vectra.Client.Models.Policies;
using Vectra.Client.Models.Tokens;
using VectraCtl.Commands;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.UnitTests.Commands;

/// <summary>
/// Covers action handlers in the API-client commands:
/// AgentsCommand, HitlCommand, PoliciesCommand, TokenCommand.
/// </summary>
public class ApiCommandActionTests
{
    // ── Shared helpers ──────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IVectraCtlLogger>());
        services.AddSingleton(Substitute.For<IVectraClient>());
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static async Task InvokeAsync(Command command, string[] args)
        => await command.Parse(args).InvokeAsync();

    // ── AgentsCommand ────────────────────────────────────────────────────────

    [Fact]
    public async Task AgentsCommand_List_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                       .Returns(new List<AgentSummary>());
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task AgentsCommand_List_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                       .Throws(new Exception("server error"));
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task AgentsCommand_Register_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.RegisterAsync(Arg.Any<RegisterAgentRequest>(), Arg.Any<CancellationToken>())
                       .Returns(new RegisterAgentResult());
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["register", "--name", "TestAgent", "--owner", "team1", "--secret", "secret123"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>());
    }

    [Fact]
    public async Task AgentsCommand_Register_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.RegisterAsync(Arg.Any<RegisterAgentRequest>(), Arg.Any<CancellationToken>())
                       .Throws(new Exception("conflict"));
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["register", "--name", "A", "--owner", "O", "--secret", "S"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task AgentsCommand_AssignPolicy_Success_WritesSuccess()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.AssignPolicyAsync(Arg.Any<Guid>(), Arg.Any<AssignPolicyRequest>(), Arg.Any<CancellationToken>())
                       .Returns(Task.CompletedTask);
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["assign-policy", "--agent-id", Guid.NewGuid().ToString(), "--policy", "default"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("Policy assigned")));
    }

    [Fact]
    public async Task AgentsCommand_AssignPolicy_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.AssignPolicyAsync(Arg.Any<Guid>(), Arg.Any<AssignPolicyRequest>(), Arg.Any<CancellationToken>())
                       .Throws(new Exception("not found"));
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["assign-policy", "--agent-id", Guid.NewGuid().ToString(), "--policy", "default"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task AgentsCommand_Delete_Success_WritesDeleted()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                       .Returns(Task.CompletedTask);
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["delete", "--agent-id", Guid.NewGuid().ToString()]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("deleted")));
    }

    [Fact]
    public async Task AgentsCommand_Delete_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var agentClient = Substitute.For<IVectraAgentClient>();
            agentClient.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                       .Throws(new Exception("forbidden"));
            client.Agents.Returns(agentClient);
            s.AddSingleton(client);
        });

        var cmd = AgentsCommand.Create(provider);
        await InvokeAsync(cmd, ["delete", "--agent-id", Guid.NewGuid().ToString()]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── HitlCommand ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HitlCommand_List_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.GetAllPendingAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<IReadOnlyList<PendingHitlRequest>>(new List<PendingHitlRequest>()));
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task HitlCommand_List_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.GetAllPendingAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                      .Throws(new Exception("server error"));
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task HitlCommand_Status_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(new HitlStatusResponse());
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["status", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task HitlCommand_Status_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Throws(new Exception("not found"));
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["status", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task HitlCommand_Approve_Success_WritesApproved()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.ApproveAsync(Arg.Any<string>(), Arg.Any<ReviewDecisionRequest>(), Arg.Any<CancellationToken>())
                      .Returns(Task.CompletedTask);
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["approve", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("approved")));
    }

    [Fact]
    public async Task HitlCommand_Approve_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.ApproveAsync(Arg.Any<string>(), Arg.Any<ReviewDecisionRequest>(), Arg.Any<CancellationToken>())
                      .Throws(new Exception("already resolved"));
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["approve", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task HitlCommand_Deny_Success_WritesDenied()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.DenyAsync(Arg.Any<string>(), Arg.Any<ReviewDecisionRequest>(), Arg.Any<CancellationToken>())
                      .Returns(Task.CompletedTask);
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["deny", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("denied")));
    }

    [Fact]
    public async Task HitlCommand_Deny_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var hitlClient = Substitute.For<IVectraHitlClient>();
            hitlClient.DenyAsync(Arg.Any<string>(), Arg.Any<ReviewDecisionRequest>(), Arg.Any<CancellationToken>())
                      .Throws(new Exception("conflict"));
            client.Hitl.Returns(hitlClient);
            s.AddSingleton(client);
        });

        var cmd = HitlCommand.Create(provider);
        await InvokeAsync(cmd, ["deny", "--id", "req-1"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── PoliciesCommand ──────────────────────────────────────────────────────

    [Fact]
    public async Task PoliciesCommand_List_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var policyClient = Substitute.For<IVectraPolicyClient>();
            policyClient.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult<IReadOnlyList<PolicySummary>>(new List<PolicySummary>()));
            client.Policies.Returns(policyClient);
            s.AddSingleton(client);
        });

        var cmd = PoliciesCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task PoliciesCommand_List_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var policyClient = Substitute.For<IVectraPolicyClient>();
            policyClient.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                        .Throws(new Exception("server error"));
            client.Policies.Returns(policyClient);
            s.AddSingleton(client);
        });

        var cmd = PoliciesCommand.Create(provider);
        await InvokeAsync(cmd, ["list"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task PoliciesCommand_Details_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var policyClient = Substitute.For<IVectraPolicyClient>();
            policyClient.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(new PolicyDetails { Name = "my-policy" });
            client.Policies.Returns(policyClient);
            s.AddSingleton(client);
        });

        var cmd = PoliciesCommand.Create(provider);
        await InvokeAsync(cmd, ["details", "--name", "my-policy"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task PoliciesCommand_Details_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var policyClient = Substitute.For<IVectraPolicyClient>();
            policyClient.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Throws(new Exception("not found"));
            client.Policies.Returns(policyClient);
            s.AddSingleton(client);
        });

        var cmd = PoliciesCommand.Create(provider);
        await InvokeAsync(cmd, ["details", "--name", "missing"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── TokenCommand ─────────────────────────────────────────────────────────

    [Fact]
    public async Task TokenCommand_Generate_Success_WritesResult()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var tokenClient = Substitute.For<IVectraTokenClient>();
            tokenClient.GenerateAsync(Arg.Any<GenerateTokenRequest>(), Arg.Any<CancellationToken>())
                       .Returns(new GenerateTokenResult());
            client.Tokens.Returns(tokenClient);
            s.AddSingleton(client);
        });

        var cmd = TokenCommand.Create(provider);
        await InvokeAsync(cmd, ["--agent-id", Guid.NewGuid().ToString(), "--secret", "my-secret"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<object?>(), Arg.Any<OutputType>());
    }

    [Fact]
    public async Task TokenCommand_Generate_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var client = Substitute.For<IVectraClient>();
            var tokenClient = Substitute.For<IVectraTokenClient>();
            tokenClient.GenerateAsync(Arg.Any<GenerateTokenRequest>(), Arg.Any<CancellationToken>())
                       .Throws(new Exception("unauthorized"));
            client.Tokens.Returns(tokenClient);
            s.AddSingleton(client);
        });

        var cmd = TokenCommand.Create(provider);
        await InvokeAsync(cmd, ["--agent-id", Guid.NewGuid().ToString(), "--secret", "bad-secret"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }
}
