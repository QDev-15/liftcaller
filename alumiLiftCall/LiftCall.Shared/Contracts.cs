using System.Text.Json.Serialization;

namespace LiftCall.Shared;

/// <summary>呼出内容</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CallType
{
    ProductPickup = 1,   // 製品引取
    ReturnMaterial = 2,  // 戻材引取
    Trouble = 3          // トラブル
}

/// <summary>パレット種類（製品引取のときだけ使用）</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaletteType
{
    None = 0,
    A = 1,
    B = 2,
    C = 3
}

/// <summary>呼出 1件分</summary>
public record CallDto(
    int UnitNo,
    CallType Type,
    PaletteType Palette,
    DateTime CalledAt,
    bool IsResponding);

/// <summary>運転状態</summary>
public record LiftStateDto(bool IsRunning, DateTime UpdatedAt);

/// <summary>ポーリング用の一括取得結果（運転状態＋呼出一覧）</summary>
public record StatusDto(bool IsRunning, DateTime UpdatedAt, IReadOnlyList<CallDto> Calls);

// ---- リクエスト ----
public record SetLiftStateRequest(bool IsRunning, string? UpdatedBy = null);
public record PutCallRequest(CallType Type, PaletteType Palette = PaletteType.None);
public record SetRespondingRequest(bool IsResponding);
public record CompleteCallRequest(string? CompletedBy = null);
