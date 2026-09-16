-- ============================================================
-- Script   : 003_CreateIndexes.sql
-- Purpose  : Add indexes and check constraints on Users
-- Run after: 002_CreateUsersTable.sql
-- Date     : 2026-09-14
-- ============================================================

USE [DietTracker];
GO

-- Unique index on Email
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Users_Email' AND object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Users_Email]
        ON [dbo].[Users] ([Email] ASC);
    PRINT 'Index [UQ_Users_Email] created.';
END
GO

-- Check: weights and height must be positive
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Users_HeightCm' AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [CK_Users_HeightCm]
        CHECK ([HeightCm] IS NULL OR [HeightCm] > 0);
    PRINT 'Constraint [CK_Users_HeightCm] added.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Users_WeightKg' AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [CK_Users_WeightKg]
        CHECK ([WeightKg] IS NULL OR [WeightKg] > 0);
    PRINT 'Constraint [CK_Users_WeightKg] added.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Users_GoalWeightKg' AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [CK_Users_GoalWeightKg]
        CHECK ([GoalWeightKg] IS NULL OR [GoalWeightKg] > 0);
    PRINT 'Constraint [CK_Users_GoalWeightKg] added.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Users_TargetDurationWeeks' AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [CK_Users_TargetDurationWeeks]
        CHECK ([TargetDurationWeeks] IS NULL OR [TargetDurationWeeks] BETWEEN 1 AND 104);
    PRINT 'Constraint [CK_Users_TargetDurationWeeks] added.';
END
GO
