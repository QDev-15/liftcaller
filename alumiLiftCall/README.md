# alumiLiftCall（リフト呼出システム）

ポーリング（2秒）方式の REST API サンプルです。

```
alumiLiftCall.sln
├─ LiftCall.Shared    … 共通（DTO・APIクライアント・設定読込）
├─ LiftCall.Api       … REST API（ASP.NET Core / IIS）
├─ LiftCall.Caller    … 呼出元 WPF（端末A）
├─ LiftCall.Receiver  … 呼出先 WPF（端末B）
└─ Sql/01_CreateTables.sql … SQL Server 用 DDL（全テーブル）
   Sql/02_AddHistory.sql    … 履歴テーブルだけ追加したい場合（01実行済みのDB用）
```

## 動かし方（開発PC 1台で確認）
1. ソリューションを開き、スタートアッププロジェクトを複数選択（Api → Caller → Receiver の順）。
2. Api が `http://localhost:5080`（Swagger UI 付き）で起動。
3. Receiver の「運転中／停車中」ボタンを押す → 最大2秒後に Caller の「リフト状態」へ反映。
4. Caller で パレットA/B/C → 製品引取、戻材引取、トラブル → Receiver の一覧に表示。
   Receiver の「対応待／応答中」を押す → Caller の「応答状態」へ反映。キャンセルで呼出が消える。

別の号機を試す: `LiftCall.Caller.exe 2`（引数＝号機番号）。

## 設定ファイル
- Caller/Receiver の `appsettings.json` … `ApiBaseUrl`（APIのURL）、`PollingSeconds`（既定2）、Caller は `UnitNo`
- Api の `appsettings.json` … `Storage`（`Memory` / `SqlServer`）、`ConnectionStrings:LiftCall`

## SQL Server（既定の保存先）
1. 対象DB（PLC）に `Sql/01_CreateTables.sql` を実行（すでに旧版を実行済みなら `02_AddHistory.sql` のみ）。
2. Api の `appsettings.json` の `Storage` は `SqlServer`、接続文字列は `ConnectionStrings:LiftCall`。
   - パスワードを含むため、Git等に載せないでください。本番は環境変数 `ConnectionStrings__LiftCall` や
     IISの環境変数で上書きする方法が安全です。

## 対応完了と履歴
- Receiver の「対応完了」→ `POST api/calls/{unitNo}/complete`
  → `LiftCallHistory` に記録（呼出日時・応答日時・完了日時・所要時間）し、`LiftCall` から削除（同一トランザクション）。
- 呼出元は次のポーリングで「待機中」に戻ります。
- キャンセルは履歴に残さず削除のみ。

## IIS へ配置
1. サーバーに **ASP.NET Core Hosting Bundle（.NET 8）** をインストール。
2. `LiftCall.Api` を発行（Visual Studio「発行」→ フォルダ）し、IISのサイト／アプリのフォルダへ配置。
3. アプリケーションプールは **「マネージドコードなし」**。
4. ファイアウォールでサイトのポートを開放し、クライアントの `ApiBaseUrl` を `http://サーバー名/…/` に変更。
5. `http://サーバー名/…/swagger` で動作確認。

※ 今回はポーリングのみのため、WebSocket（SignalR）の設定は不要です。

## 表示灯（192.168.55.7）
`LiftCall.Caller/LedController.cs` の `Enabled` を `true` にすると送信します（既定は false）。
