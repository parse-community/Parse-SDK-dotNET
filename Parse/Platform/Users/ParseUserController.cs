using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Utilities;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Data;
using System.Net.Http;
using System;

namespace Parse.Platform.Users;


public class ParseUserController : IParseUserController
{
    private IParseCommandRunner CommandRunner { get; }
    private IParseDataDecoder Decoder { get; }

    public bool RevocableSessionEnabled { get; set; } = false; // Use a simple property

    public ParseUserController(IParseCommandRunner commandRunner, IParseDataDecoder decoder)
    {
        CommandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
        Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    public async Task<IObjectState> SignUpAsync(
        IObjectState state,
        IDictionary<string, IParseFieldOperation> operations,
        IServiceHub serviceHub,
        CancellationToken cancellationToken = default)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (operations == null)
            throw new ArgumentNullException(nameof(operations));
        if (serviceHub == null)
            throw new ArgumentNullException(nameof(serviceHub));

        var command = new ParseCommand(
            "classes/_User",
            HttpMethod.Post.ToString(),
            data: serviceHub.GenerateJSONObjectForSaving(operations));

        var result = await CommandRunner.RunCommandAsync(command).ConfigureAwait(false);
        return ParseObjectCoder.Instance
            .Decode(result.Item2, Decoder, serviceHub)
            .MutatedClone(mutableClone => mutableClone.IsNew = true);
    }

    public async Task<IObjectState> LogInAsync(
        string username,
        string password,
        IServiceHub serviceHub,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        if (serviceHub == null)
            throw new ArgumentNullException(nameof(serviceHub));

        // Use POST for login with credentials in the body to improve security
        var command = new ParseCommand(
            "login",
            HttpMethod.Post.ToString(),
            data: new Dictionary<string, object> { ["username"] = username, ["password"] = password });

        var result = await CommandRunner.RunCommandAsync(command).ConfigureAwait(false);
        return ParseObjectCoder.Instance
            .Decode(result.Item2, Decoder, serviceHub)
            .MutatedClone(mutableClone => mutableClone.IsNew = result.Item1 == System.Net.HttpStatusCode.Created);
    }

    public async Task<IObjectState> LogInAsync(
        string authType,
        IDictionary<string, object> data,
        IServiceHub serviceHub,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authType))
            throw new ArgumentException("AuthType cannot be null or empty.", nameof(authType));
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (serviceHub == null)
            throw new ArgumentNullException(nameof(serviceHub));

        var authData = new Dictionary<string, object> { [authType] = data };

        var command = new ParseCommand("users",HttpMethod.Post.ToString(),data: new Dictionary<string, object> { ["authData"] = authData });

        var result = await CommandRunner.RunCommandAsync(command).ConfigureAwait(false);
        return ParseObjectCoder.Instance
            .Decode(result.Item2, Decoder, serviceHub)
            .MutatedClone(mutableClone => mutableClone.IsNew = result.Item1 == System.Net.HttpStatusCode.Created);
    }

    public async Task<IObjectState> GetUserAsync(
        string sessionToken,
        IServiceHub serviceHub,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            throw new ArgumentException("Session token cannot be null or empty.", nameof(sessionToken));
        if (serviceHub == null)
            throw new ArgumentNullException(nameof(serviceHub));

        var command = new ParseCommand("users/me",HttpMethod.Get.ToString(),sessionToken: sessionToken, null, null);
        var result = await CommandRunner.RunCommandAsync(command).ConfigureAwait(false);
        return ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub);
    }

    public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        var command = new ParseCommand(
            "requestPasswordReset",
            HttpMethod.Post.ToString(),
            data: new Dictionary<string, object> { ["email"] = email });

        return CommandRunner.RunCommandAsync(command);
    }
}