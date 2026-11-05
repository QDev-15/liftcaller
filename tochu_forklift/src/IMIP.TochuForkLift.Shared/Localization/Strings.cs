namespace IMIP.TochuForkLift.Shared.Localization;

/// <summary>
/// All user-facing text, in English and Japanese only (Vietnamese was dropped per explicit
/// instruction). Sourced from the project documentation wherever precedent exists: the approved
/// English prototype (https://claude.ai/artifact/YTML5Ag1GbGPu7nwRvPa5r), the Japanese/English
/// architecture docs under Documents/10.2026, and alumiLiftCall's actual shipped Japanese UI text.
/// Strings with no such precedent (confirm-dialog bodies, Login, Admin chrome — none of which
/// existed in the original WPF app or mockups) are composed to match the same tone/register as the
/// sourced ones.
/// </summary>
public static class Strings
{
    private static readonly Dictionary<string, (string En, string Ja)> Map = new()
    {
        // ---- Brand / shell ----
        ["AppTitle"] = ("Lift Call", "リフト呼出"),
        ["AdminAppTitle"] = ("Lift Call — Admin", "リフト呼出 — 管理"),
        ["Close"] = ("Close", "閉じる"),
        ["Confirm"] = ("Confirm", "確認"),

        // ---- Caller screen ----
        ["MachineLabel"] = ("Machine {0}", "{0}号機"),
        ["LiftStatusLabel"] = ("Lift Status", "リフト状態"),
        ["ResponseStatusLabel"] = ("Response Status", "応答状態"),
        ["Idle"] = ("Idle", "待機中"),
        ["Calling"] = ("Calling", "呼出中"),
        ["Responding"] = ("Responding", "応答中"),
        ["Running"] = ("Running", "運転中"),
        ["Stopped"] = ("Stopped", "停止中"),
        ["StoppedBanner"] = (
            "The lift is stopped. Calls are still accepted, but expect a longer wait.",
            "リフトは停止中です。呼出は可能ですが、お待ちいただく時間が長くなります。"),
        ["ProductPickup"] = ("Product Pickup", "製品引取"),
        ["ReturnMaterial"] = ("Return Material Pickup", "戻材引取"),
        ["Trouble"] = ("Trouble", "トラブル"),
        ["CallBadgeCalling"] = ("Calling…", "呼出中…"),
        ["CallBadgeResponding"] = ("Responding", "応答中"),
        ["CancelCall"] = ("Cancel Call", "呼出解除"),
        ["ConfirmCancelTitle"] = ("Cancel call?", "呼出を解除しますか？"),
        ["ConfirmCancelBody"] = (
            "The lift is already responding to this call. Cancelling now will stop that in progress.",
            "リフトはすでにこの呼出に応答中です。今キャンセルすると対応が中断されます。"),
        ["ChangeDevice"] = ("Change device", "端末を変更"),

        // ---- Receiver / Lift screen ----
        ["LiftToggleRunningHint"] = ("Tap to stop", "タップで停止"),
        ["LiftToggleStoppedHint"] = ("Tap to resume", "タップで運転再開"),
        ["ConfirmStopTitle"] = ("Stop the lift?", "リフトを停止しますか？"),
        ["ConfirmStopBody"] = (
            "Every Caller screen will show the lift as stopped. Calls can still be placed while it is stopped.",
            "すべてのCaller画面にリフト停止中と表示されます。この間も呼出は可能です。"),
        ["ConfirmStartTitle"] = ("Resume lift operation?", "リフトを運転再開しますか？"),
        ["ConfirmStartBody"] = (
            "Every Caller screen will show the lift as running again.",
            "すべてのCaller画面にリフト運転再開中と表示されます。"),
        ["QueueSection"] = ("Queue", "呼出キュー"),
        ["InProgressSection"] = ("In progress", "対応中"),
        ["CallsSection"] = ("Calls", "呼出一覧"),
        ["RespondButton"] = ("Respond", "応答"),
        ["CompleteButton"] = ("Complete", "対応完了"),
        ["ReleaseButton"] = ("Cancel", "キャンセル"),
        ["ConfirmReleaseTitle"] = ("Cancel this response?", "対応をキャンセルしますか？"),
        ["ConfirmReleaseBody"] = (
            "This call will go back to the queue, unclaimed, so anyone can respond to it again.",
            "この呼出は未対応の状態に戻り、再び誰でも応答できるようになります。"),
        ["EmptyQueueNote"] = ("No calls waiting.", "呼出はありません。"),
        ["LiftLockedTitle"] = ("Lift is stopped", "リフトは停止中です"),
        ["LiftLockedBody"] = ("Resume lift operation to view and respond to calls.", "呼出を表示・対応するにはリフトの運転を再開してください。"),

        // ---- Login (Caller/Receiver terminal) ----
        ["LoginNameLabel"] = ("Terminal name", "端末名"),
        ["LoginNamePlaceholder"] = ("Enter terminal name...", "端末名を入力..."),
        ["LoginPinLabel"] = ("PIN (4 digits)", "PIN（4桁）"),
        ["LoginError"] = ("Wrong name or PIN. Please try again.", "名前またはPINが違います。もう一度お試しください。"),
        ["LoginConnError"] = ("Can't reach the server. Please try again.", "サーバーに接続できません。もう一度お試しください。"),
        ["RoleCaller"] = ("Caller", "Caller"),
        ["RoleReceiver"] = ("Receiver", "Receiver"),

        // ---- Connection error overlay (Caller + Receiver) ----
        ["ConnectionErrorTitle"] = ("Connection Error", "通信エラー"),
        ["ConnectionErrorText"] = ("Connection to the server was lost.", "サーバーとの通信が切断されました。"),
        ["ConnectionErrorHint"] = (
            "Buttons are disabled so nobody assumes a call went through.",
            "呼出済みと誤解しないようボタンを無効化しています。"),

        // ---- Toasts ----
        ["ToastCallConflict"] = (
            "The lift is already handling your call — you can't call again right now.",
            "リフトはすでにこの呼出に対応中のため、再度呼び出せません。"),
        ["ToastRespondConflict"] = ("Request already taken", "この呼出はすでに対応済みです"),

        // ---- Admin shell ----
        ["AdminDashboardTitle"] = ("Dashboard", "ダッシュボード"),
        ["AdminTerminalsTitle"] = ("Terminals", "端末"),
        ["AdminHistoryTitle"] = ("History", "履歴"),
        ["AdminLoginTitle"] = ("Admin Login", "管理者ログイン"),
        ["AdminLoginButton"] = ("Log in", "ログイン"),
        ["AdminLogoutButton"] = ("Log out", "ログアウト"),
        ["AdminLoginWrongCreds"] = ("Wrong username or password.", "ユーザー名またはパスワードが違います。"),
        ["AdminUsernameLabel"] = ("Username", "ユーザー名"),
        ["AdminPasswordLabel"] = ("Password", "パスワード"),
        ["AdminConnBanner"] = ("Lost connection to the server, retrying…", "サーバーとの接続が切れました。再接続しています…"),
        ["AdminApiUnreachable"] = ("Can't reach the Api server.", "Apiサーバーに接続できません。"),
        ["ToggleTheme"] = ("Toggle light/dark theme", "ライト/ダークテーマ切替"),

        // ---- Admin: Dashboard ----
        ["StatLiftStatus"] = ("Lift Status", "リフト状態"),
        ["StatActiveCalls"] = ("Active calls", "対応中の呼出"),
        ["StatUnrespondedTrouble"] = ("Unresponded trouble", "未応答のトラブル"),
        ["StatResponding"] = ("Responding", "応答中"),
        ["ActiveCallsSection"] = ("Active calls", "対応中の呼出一覧"),
        ["ColMachine"] = ("Machine", "号機"),
        ["ColType"] = ("Type", "種別"),
        ["ColPallet"] = ("Pallet", "パレット"),
        ["ColCalledAt"] = ("Called at", "呼出時刻"),
        ["ColStatus"] = ("Status", "状態"),
        ["ColHandledBy"] = ("Handled by", "対応者"),
        ["NoActiveCalls"] = ("No active calls.", "対応中の呼出はありません。"),
        ["TroubleUnresponded"] = ("Trouble — unresponded", "トラブル — 未応答"),

        // ---- Admin: Terminals ----
        ["TerminalsSection"] = ("Callers & Receivers", "Caller・Receiver"),
        ["CreateTerminal"] = ("+ New Terminal", "+ 端末を追加"),
        ["ColName"] = ("Name", "名前"),
        ["ColRole"] = ("Role", "役割"),
        ["ColUnitNo"] = ("Unit No.", "号機番号"),
        ["NoTerminals"] = ("No terminals yet.", "端末がまだありません。"),
        ["Active"] = ("Active", "有効"),
        ["Inactive"] = ("Inactive", "無効"),
        ["Edit"] = ("Edit", "編集"),
        ["Save"] = ("Save", "保存"),
        ["CreateTerminalTitle"] = ("New Terminal", "端末を新規作成"),
        ["EditTerminalTitle"] = ("Edit Terminal", "端末を編集"),
        ["UnitNoLabel"] = ("Unit No.", "号機番号"),
        ["PinLabel"] = ("PIN (4 digits)", "PIN（4桁）"),
        ["NewPinLabel"] = ("Change PIN (leave blank to keep it)", "PINを変更（空欄で現状維持）"),
        ["CannotCreateTerminal"] = ("Couldn't create the terminal.", "端末を作成できませんでした。"),
        ["CannotSaveTerminal"] = ("Couldn't save.", "保存できませんでした。"),

        // ---- Terminal validation error codes (Api) — resolved client-side so the message follows
        // the viewer's chosen language instead of being baked into the server response. ----
        ["ErrorNameRequired"] = ("Name cannot be empty.", "名前を入力してください。"),
        ["ErrorInvalidPin"] = ("PIN must be exactly 4 digits.", "PINは4桁の数字で入力してください。"),
        ["ErrorCallerRequiresUnitNo"] = ("A Caller must have a machine number (Unit No.).", "CallerにはUnit No.（号機番号）が必要です。"),
        ["ErrorDuplicateName"] = ("The name \"{0}\" is already in use.", "名前「{0}」はすでに使用されています。"),
        ["ErrorUnitNoAssigned"] = ("Machine {0} is already assigned to another active Caller.", "号機{0}はすでに他の有効なCallerに割り当てられています。"),
        ["ErrorTerminalNotFound"] = ("Terminal not found.", "端末が見つかりません。"),

        // ---- Admin: History ----
        ["HistoryTitle"] = ("Call History", "呼出履歴"),
        ["FromDate"] = ("From", "開始日"),
        ["ToDate"] = ("To", "終了日"),
        ["TypeFilter"] = ("Type", "種別"),
        ["All"] = ("All", "すべて"),
        ["Search"] = ("Search", "検索"),
        ["ExportCsv"] = ("Export CSV", "CSV出力"),
        ["ColCompletedAt"] = ("Completed at", "完了時刻"),
        ["ColCompletedBy"] = ("Completed by", "完了者"),
        ["ColResponseWait"] = ("Response wait (s)", "応答待ち時間（秒）"),
        ["ColTotalDuration"] = ("Total duration (s)", "合計時間（秒）"),
        ["NoHistoryData"] = ("No data.", "データがありません。"),
    };

    /// <summary>Unknown key → returns the key itself, so a missing translation is obvious in the UI rather than silently blank.</summary>
    public static string Get(string key, AppLanguage lang)
    {
        if (!Map.TryGetValue(key, out var t)) return key;
        return lang == AppLanguage.Ja ? t.Ja : t.En;
    }
}
