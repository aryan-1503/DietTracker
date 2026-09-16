-- ============================================================
-- Script   : 002_CreateUsersTable.sql
-- Purpose  : Create the Users table
-- Run as   : DietTracker DB owner
-- Date     : 2026-09-14
-- ============================================================

USE [DietTracker];
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users'
)
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                              INT             NOT NULL IDENTITY(1,1),
        [Email]                           NVARCHAR(255)   NOT NULL,
        [PasswordHash]                    NVARCHAR(255)   NOT NULL,
        [IsActive]                        BIT             NOT NULL DEFAULT 1,
        [IsEmailVerified]                 BIT             NOT NULL DEFAULT 0,
        [EmailVerificationToken]          NVARCHAR(64)    NULL,
        [EmailVerificationTokenExpiresAt] DATETIME2(7)    NULL,

        -- Onboarding
        [OnboardingCompleted]             BIT             NOT NULL DEFAULT 0,
        [HeightCm]                        DECIMAL(5,2)    NULL,
        [WeightKg]                        DECIMAL(5,2)    NULL,
        [GoalWeightKg]                    DECIMAL(5,2)    NULL,
        [TargetDurationWeeks]             INT             NULL,

        [CreatedAt]                       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]                       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'Table [dbo].[Users] created.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[Users] already exists — skipped.';
END
GO
