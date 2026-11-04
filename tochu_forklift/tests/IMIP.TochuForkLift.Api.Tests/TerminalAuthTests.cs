using System.Net;
using System.Net.Http.Json;
using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;
using Xunit;

namespace IMIP.TochuForkLift.Api.Tests;

public class TerminalAuthTests(ForkLiftApiFactory factory) : IClassFixture<ForkLiftApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateTerminal_ThenLogin_WithCorrectPin_Succeeds()
    {
        var create = await _client.PostAsJsonAsync("api/admin/terminals", new CreateTerminalRequest
        {
            Name = "Máy 9 — test",
            Role = TerminalRole.Caller,
            UnitNo = 9,
            Pin = "1234"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var login = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest { Name = "Máy 9 — test", Pin = "1234" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var result = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(TerminalRole.Caller, result!.Role);
        Assert.Equal(9, result.UnitNo);
    }

    [Fact]
    public async Task Login_WithWrongPin_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("api/admin/terminals", new CreateTerminalRequest
        {
            Name = "Lift A — test",
            Role = TerminalRole.Receiver,
            Pin = "5555"
        });

        var login = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest { Name = "Lift A — test", Pin = "0000" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task CreateTerminal_WithNonFourDigitPin_IsRejected()
    {
        var create = await _client.PostAsJsonAsync("api/admin/terminals", new CreateTerminalRequest
        {
            Name = "Máy 10 — test",
            Role = TerminalRole.Caller,
            UnitNo = 10,
            Pin = "12"
        });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task CreateTerminal_DuplicateName_IsRejected()
    {
        await _client.PostAsJsonAsync("api/admin/terminals", new CreateTerminalRequest
        {
            Name = "Máy 11 — test", Role = TerminalRole.Caller, UnitNo = 11, Pin = "1111"
        });
        var duplicate = await _client.PostAsJsonAsync("api/admin/terminals", new CreateTerminalRequest
        {
            Name = "Máy 11 — test", Role = TerminalRole.Caller, UnitNo = 12, Pin = "2222"
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }
}
