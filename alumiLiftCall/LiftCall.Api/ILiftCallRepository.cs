using LiftCall.Shared;

namespace LiftCall.Api;

/// <summary>運転状態・呼出リストの保存先。メモリ版とSQL Server版があります。</summary>
public interface ILiftCallRepository
{
    Task<LiftStateDto> GetLiftStateAsync();
    Task SetLiftStateAsync(bool isRunning, string? updatedBy);

    /// <summary>呼出リスト（呼び出された順）</summary>
    Task<IReadOnlyList<CallDto>> GetCallsAsync();

    /// <summary>呼出を登録。同じ号機があれば置き換え（応答状態は「対応待」に戻る）</summary>
    Task UpsertCallAsync(int unitNo, CallType type, PaletteType palette);

    Task DeleteCallAsync(int unitNo);

    /// <summary>応答状態を更新。対象の呼出が無ければ false</summary>
    Task<bool> SetRespondingAsync(int unitNo, bool isResponding);

    /// <summary>
    /// 対応完了。履歴（呼出→完了の所要時間つき）に記録し、呼出リストから削除する。
    /// 対象の呼出が無ければ false。キャンセル（DeleteCallAsync）は履歴に残さない。
    /// </summary>
    Task<bool> CompleteCallAsync(int unitNo, string? completedBy);
}
