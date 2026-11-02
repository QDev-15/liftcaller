using System.Text.Json;

namespace LiftCall.Shared;

/// <summary>クライアント（WPF）共通の設定。実行フォルダの appsettings.json から読み込みます。</summary>
public class ClientSettings
{
    /// <summary>APIのベースURL（例: http://server/LiftCallApi/）</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5080/";

    /// <summary>号機番号（呼出元で使用）</summary>
    public int UnitNo { get; set; } = 1;

    /// <summary>ポーリング間隔（秒）</summary>
    public int PollingSeconds { get; set; } = 2;

    public static ClientSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new ClientSettings();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(path), options)
               ?? new ClientSettings();
    }
}
