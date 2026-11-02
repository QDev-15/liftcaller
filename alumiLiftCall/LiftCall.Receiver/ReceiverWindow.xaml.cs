using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using LiftCall.Shared;

namespace LiftCall.Receiver;

/// <summary>呼出先画面（呼出一覧）</summary>
public partial class ReceiverWindow : Window
{
    private const int MaxRows = 6;   // 1画面の最大表示行数

    private readonly ObservableCollection<CallItem> _visible = new();
    private readonly LiftCallApiClient _api;
    private readonly DispatcherTimer _timer;
    private readonly string _baseTitle;

    private bool _polling;
    private int _commandsInFlight;   // 操作（PUT）の実行中件数
    private int _version;            // 操作のたびに増やし、古いポーリング結果を捨てる

    private bool _initialized;       // 起動時の「停車中」設定が完了したか


    public ReceiverWindow(ClientSettings settings)
    {
        InitializeComponent();

        _baseTitle = Title;
        _api = new LiftCallApiClient(settings.ApiBaseUrl);
        CallList.ItemsSource = _visible;

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
        int versionAtStart = _version;
        try
        {
            var status = await _api.GetStatusAsync();
            if (status == null) throw new InvalidOperationException("応答が空です。");

            // 起動後の最初の通信成功時に、既定値「停車中」をDBへ反映（失敗時は次回ポーリングで再試行）
            if (!_initialized)
            {
                await _api.SetLiftStateAsync(false, Environment.MachineName);
                status = await _api.GetStatusAsync() ?? status;
                _initialized = true;
            }

            Title = _baseTitle;

            // 取得中に操作が入った場合は、古い結果なので反映しない
            if (_commandsInFlight == 0 && versionAtStart == _version)
                Apply(status);
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

    private void Apply(StatusDto status)
    {
        RunToggle.IsChecked = status.IsRunning;

        // 呼出順に先頭から最大6件だけ表示
        var target = status.Calls.Take(MaxRows).ToList();

        bool sameRows = target.Count == _visible.Count;
        for (int i = 0; sameRows && i < target.Count; i++)
        {
            sameRows = target[i].UnitNo == _visible[i].UnitNo
                    && target[i].Type == _visible[i].Type
                    && target[i].Palette == _visible[i].Palette
                    && target[i].CalledAt == _visible[i].CalledAt;
        }

        if (sameRows)
        {
            // 行の顔ぶれが同じなら、中身だけ更新
            for (int i = 0; i < target.Count; i++)
                _visible[i].Update(target[i], i + 1);
        }
        else
        {
            _visible.Clear();
            for (int i = 0; i < target.Count; i++)
                _visible.Add(new CallItem(target[i], i + 1));
        }

        int extra = status.Calls.Count - MaxRows;
        OverflowText.Text = $"他 {extra} 件";
        OverflowText.Visibility = extra > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Visibility = _visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ------------------------------------------------------------
    // 操作
    // ------------------------------------------------------------

    /// <summary>運転中／停車中の切り替え → API → 呼出元へ反映</summary>
    private async void RunToggle_Click(object sender, RoutedEventArgs e)
    {
        bool desired = RunToggle.IsChecked == true;
        await RunCommandAsync(() => _api.SetLiftStateAsync(desired, Environment.MachineName));
    }

    /// <summary>応答中／対応待の切り替え</summary>
    private async void ResponseButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CallItem item)
        {
            bool desired = !item.IsResponding;
            await RunCommandAsync(() => _api.SetRespondingAsync(item.UnitNo, desired));
        }
    }

    /// <summary>対応完了：履歴へ記録して一覧から削除（呼出元は次のポーリングで待機に戻る）</summary>
    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CallItem item)
            await RunCommandAsync(() => _api.CompleteCallAsync(item.UnitNo, Environment.MachineName));
    }

    private async Task RunCommandAsync(Func<Task> command)
    {
        _commandsInFlight++;
        _version++;
        try
        {
            await command();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"サーバーと通信できませんでした。\n{ex.Message}", _baseTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _commandsInFlight--;
            _version++;
        }

        // すぐ最新状態を取り直す（失敗時はトグル等がサーバーの値に戻る）
        await PollAsync();
    }
}
