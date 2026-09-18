USE [DietTracker];
GO

-- ── UserSettings: per-user preferences (reminder time, timezone) ──────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserSettings'
)
BEGIN
    CREATE TABLE [dbo].[UserSettings] (
        [Id]               INT          NOT NULL IDENTITY(1,1),
        [UserId]           INT          NOT NULL,
        [ReminderTime]     NVARCHAR(5)  NOT NULL DEFAULT '21:00',  -- HH:mm local time
        [TimeZoneId]       NVARCHAR(100) NOT NULL DEFAULT 'UTC',   -- IANA or Windows TZ id
        [CreatedAt]        DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserSettings]         PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserSettings_Users]   FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_UserSettings_UserId]  UNIQUE ([UserId])
    );
    PRINT 'Table [dbo].[UserSettings] created.';
END
GO

-- ── DailyEntries: actual consumption per user/date/meal slot ──────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'DailyEntries'
)
BEGIN
    CREATE TABLE [dbo].[DailyEntries] (
        [Id]                  INT           NOT NULL IDENTITY(1,1),
        [UserId]              INT           NOT NULL,
        [EntryDate]           DATE          NOT NULL,
        [DietPlanId]          INT           NOT NULL,
        [MealSlotId]          INT           NOT NULL,
        [FollowedPlan]        BIT           NOT NULL DEFAULT 0,
        [ActualTime]          NVARCHAR(5)   NULL,       -- HH:mm, nullable (not recorded yet)
        [FoodOptionId]        INT           NULL,       -- NULL when OtherText is used
        [OtherText]           NVARCHAR(500) NULL,       -- free-text when "Other" selected
        [CreatedAt]           DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_DailyEntries]               PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DailyEntries_Users]         FOREIGN KEY ([UserId])       REFERENCES [dbo].[Users]([Id])       ON DELETE CASCADE,
        CONSTRAINT [FK_DailyEntries_DietPlans]     FOREIGN KEY ([DietPlanId])   REFERENCES [dbo].[DietPlans]([Id])   ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyEntries_MealSlots]     FOREIGN KEY ([MealSlotId])   REFERENCES [dbo].[MealSlots]([Id])   ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyEntries_FoodOptions]   FOREIGN KEY ([FoodOptionId]) REFERENCES [dbo].[FoodOptions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_DailyEntries_UserDateSlot]  UNIQUE ([UserId], [EntryDate], [MealSlotId])
    );
    PRINT 'Table [dbo].[DailyEntries] created.';
END
GO

-- ── ReminderLogs: prevent duplicate daily reminders ───────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ReminderLogs'
)
BEGIN
    CREATE TABLE [dbo].[ReminderLogs] (
        [Id]          INT          NOT NULL IDENTITY(1,1),
        [UserId]      INT          NOT NULL,
        [ReminderDate] DATE        NOT NULL,
        [SentAt]      DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReminderLogs]             PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ReminderLogs_Users]       FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_ReminderLogs_UserDate]    UNIQUE ([UserId], [ReminderDate])
    );
    PRINT 'Table [dbo].[ReminderLogs] created.';
END
GO

-- ── Indexes ───────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyEntries_UserId_EntryDate' AND object_id = OBJECT_ID('dbo.DailyEntries'))
    CREATE NONCLUSTERED INDEX [IX_DailyEntries_UserId_EntryDate]
        ON [dbo].[DailyEntries] ([UserId], [EntryDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyEntries_MealSlotId' AND object_id = OBJECT_ID('dbo.DailyEntries'))
    CREATE NONCLUSTERED INDEX [IX_DailyEntries_MealSlotId]
        ON [dbo].[DailyEntries] ([MealSlotId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReminderLogs_UserId_ReminderDate' AND object_id = OBJECT_ID('dbo.ReminderLogs'))
    CREATE NONCLUSTERED INDEX [IX_ReminderLogs_UserId_ReminderDate]
        ON [dbo].[ReminderLogs] ([UserId], [ReminderDate]);
GO

PRINT 'Daily tracking tables setup complete.';
