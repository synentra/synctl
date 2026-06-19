using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synentra.Client.Abstractions;
using Synentra.Client.Models.Tokens;
using System.CommandLine;
using System.Text;

namespace SynentraCtl.Commands;

internal static class ProxyCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var methodOption = new Option<string>("--method")
        {
            Description = "HTTP method",
            DefaultValueFactory = (result) => "GET"
        };

        var urlOption = new Option<string>("--path")
        {
            Description = "Request path (appended to gateway base URL)",
            Required = true
        };

        var bodyOption = new Option<string>("--body")
        {
            Description = "Request body (raw string)",
            Required = true
        };

        var headerOption = new Option<string[]?>("--header")
        {
            Description = "Request headers in format Key:Value",
            Required = true
        };

        var command = new Command("proxy", "Forwards an HTTP request to the Synentra gateway")
        {
            methodOption,
            urlOption,
            bodyOption,
            headerOption
        };

        command.SetAction((parseResult, ct) => CommandHelpers.ExecuteAsync(serviceProvider, async (logger, sp) =>
        {
            var client = sp.GetRequiredService<ISynentraClient>();

            var method = parseResult.GetValue(methodOption) ?? "GET";

            var path = parseResult.GetValue(urlOption);
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("--path is required.");

            var body = parseResult.GetValue(bodyOption);
            var headersRaw = parseResult.GetValue(headerOption);

            var headers = ToHeaderDictionary(headersRaw);

            var response = await client.Proxy.ExecuteAsync(path, method, body, headers, ct);
            logger.Write(response?.ToJsonString() ?? "null");
        }));

        return command;
    }

    private static Dictionary<string, string> ToHeaderDictionary(string[]? headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (headers == null || headers.Length == 0)
            return result;

        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header))
                continue;

            var parts = header.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                continue;

            result[parts[0]] = parts[1];
        }

        return result;
    }
}