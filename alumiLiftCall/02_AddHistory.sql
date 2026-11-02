/* =====================================================================
   リフト呼出システム  履歴テーブルの追加（01_CreateTables.sql を実行済みのDB用）
   - 何度実行しても安全です。
   ===================================================================== */
-- USE PLC;
-- GO

-- ---------------------------------------------------------------------
-- 対応完了の履歴（呼出から対応完了までの時間を記録）
--   ・「対応完了」ボタンで LiftCall から移されます
--   ・キャンセルされた呼出は記録しません（LiftCall から削除のみ）
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.LiftCallHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiftCallHistory
    (
        HistoryId     BIGINT        IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_LiftCallHistory PRIMARY KEY,
        UnitNo        INT           NOT NULL,                   -- 号機番号
        CallType      TINYINT       NOT NULL,                   -- 1:製品引取 2:戻材引取 3:トラブル
        Palette       TINYINT       NOT NULL
            CONSTRAINT DF_LiftCallHistory_Palette DEFAULT (0),  -- 0:なし 1:A 2:B 3:C
        CalledAt      DATETIME2(3)  NOT NULL,                   -- 呼出日時
        RespondedAt   DATETIME2(3)  NULL,                       -- 応答開始日時（応答せず完了なら NULL）
        CompletedAt   DATETIME2(3)  NOT NULL,                   -- 対応完了日時
        CompletedBy   NVARCHAR(50)  NULL,                       -- 完了操作した端末名

        -- 呼出 → 対応完了 の所要時間（秒）
        DurationSeconds     AS DATEDIFF(SECOND, CalledAt, CompletedAt) PERSISTED,
        -- 呼出 → 応答開始 の待ち時間（秒）
        ResponseWaitSeconds AS DATEDIFF(SECOND, CalledAt, RespondedAt) PERSISTED
    );

    CREATE INDEX IX_LiftCallHistory_CompletedAt ON dbo.LiftCallHistory (CompletedAt);
    CREATE INDEX IX_LiftCallHistory_UnitNo      ON dbo.LiftCallHistory (UnitNo, CompletedAt);
END
GO

/* ---- 確認用クエリ（例） ----------------------------------------------

-- 直近の履歴
SELECT TOP (100) * FROM dbo.LiftCallHistory ORDER BY CompletedAt DESC;

-- 号機・呼出内容ごとの平均所要時間（秒）
SELECT UnitNo, CallType,
       COUNT(*)                AS Cnt,
       AVG(DurationSeconds)    AS AvgDurationSec,
       MAX(DurationSeconds)    AS MaxDurationSec,
       AVG(ResponseWaitSeconds) AS AvgResponseWaitSec
  FROM dbo.LiftCallHistory
 GROUP BY UnitNo, CallType
 ORDER BY UnitNo, CallType;

----------------------------------------------------------------------- */
