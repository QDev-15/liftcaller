using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using LiftCall.Shared;

namespace LiftCall.Receiver;

/// <summary>呼出リスト 1行分の表示用データ（APIの CallDto から作成）</summary>
public class CallItem : INotifyPropertyChanged
{
    private static Brush Make(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static readonly Brush GreenBrush  = Make(0x16, 0xA3, 0x4A);
    private static readonly Brush OrangeBrush = Make(0xF2, 0x8C, 0x0F);
    private static readonly Brush RedBrush    = Make(0xE5, 0x39, 0x35);

    public CallItem(CallDto dto, int no)
    {
        UnitNo = dto.UnitNo;
        Type = dto.Type;
        Palette = dto.Palette;
        CalledAt = dto.CalledAt;
        _no = no;
        _isResponding = dto.IsResponding;
    }

    public int UnitNo { get; }
    public CallType Type { get; }
    public PaletteType Palette { get; }
    public DateTime CalledAt { get; }

    public string UnitName => $"{UnitNo}号機";

    public string TypeText => Type switch
    {
        CallType.ProductPickup  => "製品引取",
        CallType.ReturnMaterial => "戻材引取",
        _                       => "トラブル"
    };

    /// <summary>パレット種類を表示するか（製品引取でパレット指定があるとき）</summary>
    public bool HasPalette => Type == CallType.ProductPickup && Palette != PaletteType.None;

    public string PaletteText => $"パレット{Palette}";

    public string Icon => Type switch
    {
        CallType.ProductPickup  => "📦",
        CallType.ReturnMaterial => "🔄",
        _                       => "⚠️"
    };

    public Brush AccentBrush => Type switch
    {
        CallType.ProductPickup  => GreenBrush,
        CallType.ReturnMaterial => OrangeBrush,
        _                       => RedBrush
    };

    private int _no;
    /// <summary>呼出順（1始まり）</summary>
    public int No
    {
        get => _no;
        private set { if (_no != value) { _no = value; OnPropertyChanged(); } }
    }

    private bool _isResponding;
    /// <summary>true: 応答中 / false: 対応待</summary>
    public bool IsResponding
    {
        get => _isResponding;
        private set { if (_isResponding != value) { _isResponding = value; OnPropertyChanged(); } }
    }

    /// <summary>同じ呼出の最新状態を反映（行を作り直さないのでちらつかない）</summary>
    public void Update(CallDto dto, int no)
    {
        No = no;
        IsResponding = dto.IsResponding;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
