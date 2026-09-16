-- ============================================================
-- Script   : 001_CreateDietPlanTables.sql
-- Purpose  : Create DietPlans, MealSlots, and FoodOptions tables
-- Run after: 2026-09-14/004_AlterUsers_AddNameFields.sql
-- Date     : 2026-09-16
-- ============================================================

USE [DietTracker];
GO

-- ── DietPlans ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'DietPlans'
)
BEGIN
    CREATE TABLE [dbo].[DietPlans] (
        [Id]        INT            NOT NULL IDENTITY(1,1),
        [UserId]    INT            NOT NULL,
        [Name]      NVARCHAR(200)  NOT NULL,
        [IsActive]  BIT            NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_DietPlans] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DietPlans_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
            ON DELETE CASCADE
    );
    PRINT 'Table [dbo].[DietPlans] created.';
END
GO

-- ── MealSlots ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MealSlots'
)
BEGIN
    CREATE TABLE [dbo].[MealSlots] (
        [Id]           INT            NOT NULL IDENTITY(1,1),
        [DietPlanId]   INT            NOT NULL,
        -- HH:mm stored as NVARCHAR(5) — simple, TZ-free, matches UI time pickers
        [StartTime]    NVARCHAR(5)    NOT NULL,
        [EndTime]      NVARCHAR(5)    NOT NULL,
        [MealCategory] NVARCHAR(100)  NOT NULL,
        -- Display order within the plan
        [SortOrder]    INT            NOT NULL DEFAULT 0,
        [CreatedAt]    DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_MealSlots] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_MealSlots_DietPlans]
            FOREIGN KEY ([DietPlanId]) REFERENCES [dbo].[DietPlans]([Id])
            ON DELETE CASCADE
    );
    PRINT 'Table [dbo].[MealSlots] created.';
END
GO

-- ── FoodOptions ───────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'FoodOptions'
)
BEGIN
    CREATE TABLE [dbo].[FoodOptions] (
        [Id]          INT            NOT NULL IDENTITY(1,1),
        [MealSlotId]  INT            NOT NULL,
        [Name]        NVARCHAR(300)  NOT NULL,
        [SortOrder]   INT            NOT NULL DEFAULT 0,
        [CreatedAt]   DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_FoodOptions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_FoodOptions_MealSlots]
            FOREIGN KEY ([MealSlotId]) REFERENCES [dbo].[MealSlots]([Id])
            ON DELETE CASCADE
    );
    PRINT 'Table [dbo].[FoodOptions] created.';
END
GO

-- ── Indexes ───────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DietPlans_UserId' AND object_id = OBJECT_ID('dbo.DietPlans'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DietPlans_UserId]
        ON [dbo].[DietPlans] ([UserId] ASC);
    PRINT 'Index [IX_DietPlans_UserId] created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MealSlots_DietPlanId' AND object_id = OBJECT_ID('dbo.MealSlots'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MealSlots_DietPlanId]
        ON [dbo].[MealSlots] ([DietPlanId] ASC, [SortOrder] ASC);
    PRINT 'Index [IX_MealSlots_DietPlanId] created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FoodOptions_MealSlotId' AND object_id = OBJECT_ID('dbo.FoodOptions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FoodOptions_MealSlotId]
        ON [dbo].[FoodOptions] ([MealSlotId] ASC, [SortOrder] ASC);
    PRINT 'Index [IX_FoodOptions_MealSlotId] created.';
END
GO
