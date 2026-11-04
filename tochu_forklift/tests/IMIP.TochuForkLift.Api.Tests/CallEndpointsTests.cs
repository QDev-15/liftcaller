using System.Net;
using System.Net.Http.Json;
using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;
using Xunit;

namespace IMIP.TochuForkLift.Api.Tests;

public class CallEndpointsTests(ForkLiftApiFactory factory) : IClassFixture<ForkLiftApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PutCall_WhileAlreadyResponding_ReturnsConflict_InsteadOfSilentlyOverwriting()
    {
        // Regression test for alumiLiftCall bug 9.1 — the old MERGE statement reset
        // IsResponding=0 unconditionally on re-call, losing the Receiver's in-progress claim.
        const int unitNo = 101;

        var first = await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.ProductPickup });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var responded = await _client.PutAsJsonAsync($"api/calls/{unitNo}/response",
            new SetRespondingRequest { IsResponding = true, RespondedBy = "Receiver-A" });
        Assert.Equal(HttpStatusCode.NoContent, responded.StatusCode);

        // Machine 101 presses a call button again while the Receiver is already handling it.
        var reCall = await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.Trouble });
        Assert.Equal(HttpStatusCode.Conflict, reCall.StatusCode);

        var state = await _client.GetFromJsonAsync<StateSnapshotDto>("api/state");
        var call = state!.Calls.Single(c => c.UnitNo == unitNo);
        Assert.True(call.IsResponding);
        Assert.Equal(CallType.ProductPickup, call.CallType); // unchanged — the re-call was rejected, not applied
    }

    [Fact]
    public async Task PutCall_WhenNotYetResponded_OverwritesCallType()
    {
        const int unitNo = 102;

        await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.ProductPickup });
        var overwrite = await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.ReturnMaterial });
        Assert.Equal(HttpStatusCode.NoContent, overwrite.StatusCode);

        var state = await _client.GetFromJsonAsync<StateSnapshotDto>("api/state");
        Assert.Equal(CallType.ReturnMaterial, state!.Calls.Single(c => c.UnitNo == unitNo).CallType);
    }

    [Fact]
    public async Task TwoReceivers_RespondingToSameCall_OnlyOneWins()
    {
        // plan.md §6.4 — conditional UPDATE WHERE IsResponding = 0 guarantees exactly one winner.
        const int unitNo = 103;
        await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.Trouble });

        var responseA = await _client.PutAsJsonAsync($"api/calls/{unitNo}/response",
            new SetRespondingRequest { IsResponding = true, RespondedBy = "Receiver-A" });
        var responseB = await _client.PutAsJsonAsync($"api/calls/{unitNo}/response",
            new SetRespondingRequest { IsResponding = true, RespondedBy = "Receiver-B" });

        var outcomes = new[] { responseA.StatusCode, responseB.StatusCode };
        Assert.Contains(HttpStatusCode.NoContent, outcomes);
        Assert.Contains(HttpStatusCode.Conflict, outcomes);

        var state = await _client.GetFromJsonAsync<StateSnapshotDto>("api/state");
        var call = state!.Calls.Single(c => c.UnitNo == unitNo);
        Assert.True(call.IsResponding);
        Assert.Equal(responseA.StatusCode == HttpStatusCode.NoContent ? "Receiver-A" : "Receiver-B", call.RespondedBy);
    }

    [Fact]
    public async Task TroubleCalls_AreAlwaysOrderedBeforeOtherTypes_RegardlessOfCallOrder()
    {
        await _client.PutAsJsonAsync("api/calls/201", new PutCallRequest { CallType = CallType.ProductPickup });
        await _client.PutAsJsonAsync("api/calls/202", new PutCallRequest { CallType = CallType.ReturnMaterial });
        await _client.PutAsJsonAsync("api/calls/203", new PutCallRequest { CallType = CallType.Trouble });

        var state = await _client.GetFromJsonAsync<StateSnapshotDto>("api/state");
        var ordered = state!.Calls.Where(c => c.UnitNo is 201 or 202 or 203).ToList();

        Assert.Equal(CallType.Trouble, ordered[0].CallType);
    }

    [Fact]
    public async Task CompleteCall_MovesItFromActiveToHistory()
    {
        const int unitNo = 104;
        await _client.PutAsJsonAsync($"api/calls/{unitNo}", new PutCallRequest { CallType = CallType.ProductPickup });
        await _client.PutAsJsonAsync($"api/calls/{unitNo}/response", new SetRespondingRequest { IsResponding = true, RespondedBy = "Receiver-A" });

        var complete = await _client.PostAsJsonAsync($"api/calls/{unitNo}/complete", new CompleteCallRequest { CompletedBy = "Receiver-A" });
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        var state = await _client.GetFromJsonAsync<StateSnapshotDto>("api/state");
        Assert.DoesNotContain(state!.Calls, c => c.UnitNo == unitNo);

        var history = await _client.GetFromJsonAsync<List<HistoryEntryDto>>($"api/history?unitNo={unitNo}");
        Assert.Contains(history!, h => h.UnitNo == unitNo && h.CompletedBy == "Receiver-A");
    }
}
