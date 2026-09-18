USE [DietTracker];
GO

-- ── Fix FK_DailyEntries_DietPlans: NO ACTION -> CASCADE ───────────────────────
-- When a DietPlan is deleted, its DailyEntries should also be deleted.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DailyEntries_DietPlans')
BEGIN
    ALTER TABLE [dbo].[DailyEntries] DROP CONSTRAINT [FK_DailyEntries_DietPlans];
    PRINT 'Dropped FK_DailyEntries_DietPlans';
END
GO
ALTER TABLE [dbo].[DailyEntries]
    ADD CONSTRAINT [FK_DailyEntries_DietPlans]
    FOREIGN KEY ([DietPlanId]) REFERENCES [dbo].[DietPlans]([Id])
    ON DELETE CASCADE;
PRINT 'Recreated FK_DailyEntries_DietPlans as CASCADE';
GO

-- ── Fix FK_DailyEntries_MealSlots: NO ACTION -> CASCADE ──────────────────────
-- When a MealSlot is deleted, its DailyEntries should also be deleted.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DailyEntries_MealSlots')
BEGIN
    ALTER TABLE [dbo].[DailyEntries] DROP CONSTRAINT [FK_DailyEntries_MealSlots];
    PRINT 'Dropped FK_DailyEntries_MealSlots';
END
GO
ALTER TABLE [dbo].[DailyEntries]
    ADD CONSTRAINT [FK_DailyEntries_MealSlots]
    FOREIGN KEY ([MealSlotId]) REFERENCES [dbo].[MealSlots]([Id])
    ON DELETE CASCADE;
PRINT 'Recreated FK_DailyEntries_MealSlots as CASCADE';
GO

-- ── Fix FK_DailyEntries_FoodOptions: NO ACTION -> SET NULL ───────────────────
-- When a FoodOption is deleted, null out FoodOptionId (preserves the entry).
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DailyEntries_FoodOptions')
BEGIN
    ALTER TABLE [dbo].[DailyEntries] DROP CONSTRAINT [FK_DailyEntries_FoodOptions];
    PRINT 'Dropped FK_DailyEntries_FoodOptions';
END
GO
ALTER TABLE [dbo].[DailyEntries]
    ADD CONSTRAINT [FK_DailyEntries_FoodOptions]
    FOREIGN KEY ([FoodOptionId]) REFERENCES [dbo].[FoodOptions]([Id])
    ON DELETE SET NULL;
PRINT 'Recreated FK_DailyEntries_FoodOptions as SET NULL';
GO

PRINT 'All DailyEntries FK constraints updated successfully.';
