-- ============================================================
-- Script   : 004_AlterUsers_AddNameFields.sql
-- Purpose  : Add FirstName, MiddleName, LastName columns and
--            a persisted computed column FullName to Users.
-- Run after: 003_CreateIndexes.sql
-- Date     : 2026-09-14
-- ============================================================

USE [DietTracker];
GO

-- ── FirstName ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'FirstName'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [FirstName] NVARCHAR(100) NULL;
    PRINT 'Column [FirstName] added.';
END
GO

-- ── MiddleName ────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'MiddleName'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [MiddleName] NVARCHAR(100) NULL;
    PRINT 'Column [MiddleName] added.';
END
GO

-- ── LastName ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastName'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [LastName] NVARCHAR(100) NULL;
    PRINT 'Column [LastName] added.';
END
GO

-- ── FullName (persisted computed column) ─────────────────────────────────────
-- Builds: "First [Middle ]Last", trimming extra spaces when MiddleName is NULL.
-- Falls back to Email when all name parts are NULL (covers pre-onboarding rows).
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'FullName'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [FullName] AS (
            CASE
                WHEN [FirstName] IS NULL AND [LastName] IS NULL
                    THEN [Email]
                WHEN [MiddleName] IS NULL
                    THEN LTRIM(RTRIM(
                            ISNULL([FirstName], '') + ' ' + ISNULL([LastName], '')
                         ))
                ELSE
                    LTRIM(RTRIM(
                        ISNULL([FirstName], '') + ' ' +
                        ISNULL([MiddleName], '') + ' ' +
                        ISNULL([LastName], '')
                    ))
            END
        ) PERSISTED;
    PRINT 'Computed column [FullName] added.';
END
GO
