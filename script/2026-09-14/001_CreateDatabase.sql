-- ============================================================
-- Script   : 001_CreateDatabase.sql
-- Purpose  : Create the DietTracker database
-- Run as   : SA or a login with CREATE DATABASE permission
-- Date     : 2026-09-14
-- ============================================================

USE master;
GO

IF NOT EXISTS (
    SELECT name FROM sys.databases WHERE name = N'DietTracker'
)
BEGIN
    CREATE DATABASE [DietTracker]
        COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database [DietTracker] created.';
END
ELSE
BEGIN
    PRINT 'Database [DietTracker] already exists — skipped.';
END
GO
