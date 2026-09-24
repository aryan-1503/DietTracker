-- ============================================================
-- Migration: 001_CreateWeeklyWeightsTable.sql
-- Date:      2026-09-24
-- Purpose:   Add WeeklyWeights table for weekly weight tracking
-- Run in:    SSMS against the DietTracker database
-- ============================================================

USE DietTracker;
GO

-- Create the WeeklyWeights table
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'WeeklyWeights'
)
BEGIN
    CREATE TABLE WeeklyWeights (
        Id          INT IDENTITY(1,1) NOT NULL,
        UserId      INT NOT NULL,
        Year        INT NOT NULL,
        WeekNumber  INT NOT NULL,
        WeightKg    DECIMAL(5,1) NOT NULL,
        RecordedAt  DATETIME2 NOT NULL CONSTRAINT DF_WeeklyWeights_RecordedAt DEFAULT GETUTCDATE(),

        CONSTRAINT PK_WeeklyWeights PRIMARY KEY CLUSTERED (Id),

        CONSTRAINT FK_WeeklyWeights_Users
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,

        CONSTRAINT UQ_WeeklyWeights_UserWeek
            UNIQUE (UserId, Year, WeekNumber)
    );

    -- Index for lookups by user
    CREATE NONCLUSTERED INDEX IX_WeeklyWeights_UserId
        ON WeeklyWeights (UserId);

    PRINT 'WeeklyWeights table created successfully.';
END
ELSE
BEGIN
    PRINT 'WeeklyWeights table already exists. Skipping.';
END
GO
