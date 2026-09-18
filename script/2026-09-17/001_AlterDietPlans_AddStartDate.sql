USE [DietTracker];
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'DietPlans' AND COLUMN_NAME = 'StartDate'
)
BEGIN
    ALTER TABLE [dbo].[DietPlans]
        ADD [StartDate] DATE NULL;
    PRINT 'Column [StartDate] added to [DietPlans].';
END
ELSE
BEGIN
    PRINT 'Column [StartDate] already exists -- skipped.';
END
GO
