/* =====================================================================
   リフト呼出システム  SQL Server DDL（全テーブル）
   - 対象DB（PLC）に対して実行してください。必要なら先頭の USE を有効にします。
   - 何度実行しても安全です（存在するテーブルは作成しません）。
   - dbo.LiftState / dbo.LiftCall / dbo.LiftCallHistory が既存テーブルと
     名前が重複しないことを確認してから実行してください。
   ===================================================================== */
-- USE PLC;
-- GO

-- ---------------------------------------------------------------------
-- 運転状態（1行だけのテーブル）  呼出先の「運転中／停車中」トグル
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.LiftState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiftState
    (
        Id         INT           NOT NULL
            CONSTRAINT PK_LiftState PRIMARY KEY
            CONSTRAINT CK_LiftState_SingleRow CHECK (Id = 1),   -- 常に1行のみ
        IsRunning  BIT           NOT NULL,                      -- 1:運転中 / 0:停車中
        UpdatedAt  DATETIME2(3)  NOT NULL
            CONSTRAINT DF_LiftState_UpdatedAt DEFAULT (SYSDATETIME()),
        UpdatedBy  NVARCHAR(50)  NULL                           -- 更新した端末名
    );

    INSERT INTO dbo.LiftState (Id, IsRunning, UpdatedAt, UpdatedBy)
    VALUES (1, 1, SYSDATETIME(), N'init');
END
GO

-- ---------------------------------------------------------------------
-- 呼出（1号機につき1件。再呼出は置き換え）
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.LiftCall', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiftCall
    (
        UnitNo        INT           NOT NULL
            CONSTRAINT PK_LiftCall PRIMARY KEY,                 -- 号機番号
        CallType      TINYINT       NOT NULL
            CONSTRAINT CK_LiftCall_CallType CHECK (CallType IN (1, 2, 3)),
                                                                -- 1:製品引取 2:戻材引取 3:トラブル
        Palette       TINYINT       NOT NULL
            CONSTRAINT DF_LiftCall_Palette DEFAULT (0)
            CONSTRAINT CK_LiftCall_Palette CHECK (Palette BETWEEN 0 AND 3),
                                                                -- 0:なし 1:A 2:B 3:C（製品引取のみ）
        CalledAt      DATETIME2(3)  NOT NULL
            CONSTRAINT DF_LiftCall_CalledAt DEFAULT (SYSDATETIME()),   -- 呼出日時
        IsResponding  BIT           NOT NULL
            CONSTRAINT DF_LiftCall_IsResponding DEFAULT (0),    -- 1:応答中 / 0:対応待
        RespondedAt   DATETIME2(3)  NULL                        -- 応答開始日時
    );

    -- 呼出順の取得用
    CREATE INDEX IX_LiftCall_CalledAt ON dbo.LiftCall (CalledAt, UnitNo);
END
GO

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
