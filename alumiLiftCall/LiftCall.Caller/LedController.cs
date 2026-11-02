using System.Net.Http;

namespace LiftCall.Caller;

/// <summary>
/// 表示灯（192.168.55.7）の制御。
/// 実機に接続するときは Enabled を true にしてください（開発中は何も送信しません）。
/// </summary>
public static class LedController
{
    public static bool Enabled { get; set; } = false;

    // パターン（これまで各所に書かれていた led= の値）
    public const string Off = "00000";         // キャンセル
    public const string Trouble = "10000";     // トラブル
    public const string Return = "01000";      // 戻材引取
    public const string Product = "00100";     // 製品引取
    public const string Waiting = "11100";     // 対応待
    public const string Responding = "22200";  // 応答中

    private const string BaseUrl = "http://192.168.55.7/api/control?led=";
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(2) };

    public static async Task SendAsync(string pattern)
    {
        if (!Enabled) return;
        try
        {
            using var _ = await _client.GetAsync(BaseUrl + pattern);
        }
        catch
        {
            // 表示灯の通信失敗で画面操作を止めない
        }
    }
}
