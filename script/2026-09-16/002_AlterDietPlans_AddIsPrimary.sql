-- ============================================================
-- Script   : 002_AlterDietPlans_AddIsPrimary.sql
-- Purpose  : Add IsPrimary flag to DietPlans table.
--            Only one plan per user can be primary at a time
--            (enforced at the application layer, not a DB constraint,
--            because swapping atomically would require a deferred constraint).
-- Run after: 001_CreateDietPlanTables.sql
-- Date     : 2026-09-16
-- ============================================================

USE [DietTracker];
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'DietPlans' AND COLUMN_NAME = 'IsPrimary'
)
BEGIN
    ALTER TABLE [dbo].[DietPlans]
        ADD [IsPrimary] BIT NOT NULL DEFAULT 0;
    PRINT 'Column [IsPrimary] added to [DietPlans].';
END
ELSE
BEGIN
    PRINT 'Column [IsPrimary] already exists -- skipped.';
END
GO
