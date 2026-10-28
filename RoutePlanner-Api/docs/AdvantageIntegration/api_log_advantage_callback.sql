-- VRP: guard so Advantage easygo-update is PUT once per RunID.
-- Insert a row only after HTTP 200 from Advantage. No row = callback not sent; retry is allowed.
-- Unique(RunID) blocks a second successful PUT if two integrate-all clicks race.
--
-- Target: VRP
-- Company 20 = Advantage

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

IF OBJECT_ID(N'dbo.api_log_advantage_callback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.api_log_advantage_callback
    (
        LogID           BIGINT IDENTITY(1, 1) NOT NULL,
        RunID           VARCHAR(50) NOT NULL,
        CompanyID       INT NOT NULL,
        HttpStatus      INT NOT NULL,
        Message         VARCHAR(500) NULL,
        TotalRouted     INT NOT NULL
            CONSTRAINT DF_api_log_advantage_callback_TotalRouted DEFAULT (0),
        TotalUnrouted   INT NOT NULL
            CONSTRAINT DF_api_log_advantage_callback_TotalUnrouted DEFAULT (0),
        DtmCrt          DATETIME NOT NULL
            CONSTRAINT DF_api_log_advantage_callback_DtmCrt DEFAULT (GETDATE()),
        UsrUpd          VARCHAR(50) NULL,
        CONSTRAINT PK_api_log_advantage_callback PRIMARY KEY CLUSTERED (LogID),
        CONSTRAINT UQ_api_log_advantage_callback_RunID UNIQUE (RunID)
    );

    CREATE INDEX IX_api_log_advantage_callback_CompanyID
        ON dbo.api_log_advantage_callback (CompanyID, DtmCrt DESC);

    PRINT N'Created api_log_advantage_callback';
END
ELSE
    PRINT N'Skip: api_log_advantage_callback already exists';

COMMIT TRAN;
