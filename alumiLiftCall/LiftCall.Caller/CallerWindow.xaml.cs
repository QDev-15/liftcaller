using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using LiftCall.Shared;

namespace LiftCall.Caller;

/// <summary>呼出元画面</summary>
public partial class CallerWindow : Window
{
    private enum ResponseState { Unknown, Idle, Waiting, Responding }

    private readonly int _unitNo;
    private readonly string _baseTitle;
    private readonly LiftCallApiClient _api;
    private readonly DispatcherTimer _timer;

    private bool _polling;
    private PaletteType _palette = PaletteType.None;
    private ResponseState _lastState = ResponseState.Unknown;

    public CallerWindow(int unitNo, ClientSettings settings)
    {
        InitializeComponent();

        _unitNo = unitNo;
        _api = new LiftCallApiClient(settings.ApiBaseUrl);

        LiftNameText.Text = $"{unitNo}号機";
        _baseTitle = $"リフト呼び出し（{unitNo}号機）";
        Title = _baseTitle;

        UpdatePaletteUi();

        // ポーリング（既定 2秒）
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(settings.PollingSeconds > 0 ? settings.PollingSeconds : 2)
        };
        _timer.Tick += async (_, _) => await PollAsync();

        Loaded += async (_, _) =>
        {
            await PollAsync();
            _timer.Start();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _api.Dispose();
        };
    }

    // ------------------------------------------------------------
    // ポーリング
    // ------------------------------------------------------------
    private async Task PollAsync()
    {
        if (_polling) return;
        _polling = true;
        try
        {
            var status = await _api.GetStatusAsync();
            if (status == null) throw new InvalidOperationException("応答が空です。");

            Title = _baseTitle;
            ApplyStatus(status);
        }
        catch
        {
            Title = _baseTitle + "（通信エラー）";
        }
        finally
        {
            _polling = false;
        }
    }

    /// <summary>リフト状態・応答状態の表示を更新</summary>
    private void ApplyStatus(StatusDto status)
    {
        // リフト状態（呼出先のトグルと連動）
        var liftBrush = (Brush)FindResource(status.IsRunning ? "GreenBrush" : "OrangeBrush");
        LiftStateText.Text = status.IsRunning ? "運転中" : "停車中";
        LiftStateText.Foreground = liftBrush;
        LiftStateDot.Fill = liftBrush;

        // 応答状態（この号機の呼出）
        var call = status.Calls.FirstOrDefault(c => c.UnitNo == _unitNo);
        ResponseState state;
        string text;
        Brush brush;
        if (call == null)
        {
            state = ResponseState.Idle;
            text = "待機中";
            brush = (Brush)FindResource("GrayBrush");
        }
        else if (call.IsResponding)
        {
            state = ResponseState.Responding;
            text = "応答中";
            brush = (Brush)FindResource("BlueBrush");
        }
        else
        {
            state = ResponseState.Waiting;
            text = "対応待";
            brush = (Brush)FindResource("OrangeBrush");
        }

        ResponseStateText.Text = text;
        ResponseStateText.Foreground = brush;
        ResponseStateDot.Fill = brush;

        // 表示灯は状態が変わったときだけ送信（2秒ごとには送らない）
        if (state != _lastState)
        {
            if (state == ResponseState.Waiting)
                _ = LedController.SendAsync(LedController.Waiting);
            else if (state == ResponseState.Responding)
                _ = LedController.SendAsync(LedController.Responding);
            else if (state == ResponseState.Idle &&
                     (_lastState == ResponseState.Waiting || _lastState == ResponseState.Responding))
                _ = LedController.SendAsync(LedController.Off);   // 呼出先で対応完了になった
            _lastState = state;
        }
    }

    // ------------------------------------------------------------
    // パレット選択（A/B/C のうち1つ。もう一度押すと解除）
    // ------------------------------------------------------------
    private void Palette_Click(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        var selected = Enum.Parse<PaletteType>((string)button.Tag);
        _palette = button.IsChecked == true ? selected : PaletteType.None;
        UpdatePaletteUi();
    }

    private void UpdatePaletteUi()
    {
        PaletteA.IsChecked = _palette == PaletteType.A;
        PaletteB.IsChecked = _palette == PaletteType.B;
        PaletteC.IsChecked = _palette == PaletteType.C;

        // 製品引取は、パレットを選ぶまで押せない
        ProductButton.IsEnabled = _palette != PaletteType.None;
    }

    // ------------------------------------------------------------
    // ボタン操作
    // ------------------------------------------------------------
    private async void ProductPickup_Click(object sender, RoutedEventArgs e)
    {
        if (_palette == PaletteType.None) return;

        if (await SendCallAsync(CallType.ProductPickup, _palette, LedController.Product))
        {
            // 呼出後はパレット選択をクリア（次回も選び直し）
            _palette = PaletteType.None;
            UpdatePaletteUi();
        }
    }

    // 戻材引取・トラブルはパレット不要（単独で動作）
    private async void ReturnMaterial_Click(object sender, RoutedEventArgs e)
        => await SendCallAsync(CallType.ReturnMaterial, PaletteType.None, LedController.Return);

    private async void Trouble_Click(object sender, RoutedEventArgs e)
        => await SendCallAsync(CallType.Trouble, PaletteType.None, LedController.Trouble);

    private async void CancelCall_Click(object sender, RoutedEventArgs e)
    {
        _palette = PaletteType.None;
        UpdatePaletteUi();

        try
        {
            await _api.CancelCallAsync(_unitNo);
            _ = LedController.SendAsync(LedController.Off);
            await PollAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task<bool> SendCallAsync(CallType type, PaletteType palette, string ledPattern)
    {
        try
        {
            await _api.PutCallAsync(_unitNo, type, palette);
            _ = LedController.SendAsync(ledPattern);
            await PollAsync();
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
    }

    private void ShowError(Exception ex)
        => MessageBox.Show($"サーバーと通信できませんでした。\n{ex.Message}", _baseTitle,
            MessageBoxButton.OK, MessageBoxImage.Warning);
}
