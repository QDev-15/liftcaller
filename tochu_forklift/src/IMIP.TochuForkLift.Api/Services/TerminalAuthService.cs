using System.Text.RegularExpressions;
using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Api.Data.Entities;
using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Services;

public partial class TerminalAuthService(ForkLiftDbContext db, ILogger<TerminalAuthService> logger) : ITerminalAuthService
{
    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex PinFormatRegex();

    public async Task<LoginResponse?> LoginAsync(string name, string pin, CancellationToken ct = default)
    {
        var terminal = await db.Terminals.SingleOrDefaultAsync(t => t.Name == name && t.IsActive, ct);
        if (terminal is null)
        {
            logger.LogInformation("Login failed — unknown or inactive terminal name {Name}", name);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(pin, terminal.PinHash))
        {
            logger.LogInformation("Login failed — wrong PIN for terminal {Name}", name);
            return null;
        }

        return new LoginResponse
        {
            TerminalId = terminal.Id,
            Name = terminal.Name,
            Role = (TerminalRole)terminal.Role,
            UnitNo = terminal.UnitNo
        };
    }

    public async Task<TerminalDto?> ValidateAsync(int terminalId, CancellationToken ct = default)
    {
        var terminal = await db.Terminals.AsNoTracking().SingleOrDefaultAsync(t => t.Id == terminalId && t.IsActive, ct);
        return terminal is null ? null : ToDto(terminal);
    }

    public async Task<List<TerminalSuggestionDto>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        return await db.Terminals
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TerminalSuggestionDto { Name = t.Name, Role = (TerminalRole)t.Role })
            .ToListAsync(ct);
    }

    public async Task<List<TerminalDto>> GetTerminalsAsync(CancellationToken ct = default)
    {
        return await db.Terminals
            .AsNoTracking()
            .OrderBy(t => t.Role).ThenBy(t => t.Name)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<TerminalOperationResult> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return TerminalOperationResult.Fail("ErrorNameRequired");

        if (!PinFormatRegex().IsMatch(request.Pin))
            return TerminalOperationResult.Fail("ErrorInvalidPin");

        if (request.Role == TerminalRole.Caller && request.UnitNo is null)
            return TerminalOperationResult.Fail("ErrorCallerRequiresUnitNo");

        if (await db.Terminals.AnyAsync(t => t.Name == name, ct))
            return TerminalOperationResult.Fail("ErrorDuplicateName", name);

        if (request.Role == TerminalRole.Caller &&
            await db.Terminals.AnyAsync(t => t.UnitNo == request.UnitNo && t.Role == (byte)TerminalRole.Caller && t.IsActive, ct))
            return TerminalOperationResult.Fail("ErrorUnitNoAssigned", request.UnitNo);

        var terminal = new Terminal
        {
            Name = name,
            Role = (byte)request.Role,
            UnitNo = request.Role == TerminalRole.Caller ? request.UnitNo : null,
            PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Terminals.Add(terminal);
        await db.SaveChangesAsync(ct);

        return TerminalOperationResult.Ok(ToDto(terminal));
    }

    public async Task<TerminalOperationResult> UpdateTerminalAsync(int id, UpdateTerminalRequest request, CancellationToken ct = default)
    {
        var terminal = await db.Terminals.SingleOrDefaultAsync(t => t.Id == id, ct);
        if (terminal is null) return TerminalOperationResult.Fail("ErrorTerminalNotFound");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return TerminalOperationResult.Fail("ErrorNameRequired");

        if (terminal.Role == (byte)TerminalRole.Caller && request.UnitNo is null)
            return TerminalOperationResult.Fail("ErrorCallerRequiresUnitNo");

        if (await db.Terminals.AnyAsync(t => t.Id != id && t.Name == name, ct))
            return TerminalOperationResult.Fail("ErrorDuplicateName", name);

        if (terminal.Role == (byte)TerminalRole.Caller && request.IsActive &&
            await db.Terminals.AnyAsync(t => t.Id != id && t.UnitNo == request.UnitNo && t.Role == (byte)TerminalRole.Caller && t.IsActive, ct))
            return TerminalOperationResult.Fail("ErrorUnitNoAssigned", request.UnitNo);

        if (!string.IsNullOrEmpty(request.NewPin))
        {
            if (!PinFormatRegex().IsMatch(request.NewPin))
                return TerminalOperationResult.Fail("ErrorInvalidPin");
            terminal.PinHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin);
        }

        terminal.Name = name;
        terminal.UnitNo = terminal.Role == (byte)TerminalRole.Caller ? request.UnitNo : null;
        terminal.IsActive = request.IsActive;
        terminal.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return TerminalOperationResult.Ok(ToDto(terminal));
    }

    private static TerminalDto ToDto(Terminal t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Role = (TerminalRole)t.Role,
        UnitNo = t.UnitNo,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
