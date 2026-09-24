-- Migration: 001_CreateDailyNotes
-- Creates the DailyNotes table to store one optional note per user per date.
-- Run in SSMS against the DietTracker database.

CREATE TABLE [dbo].[DailyNotes] (
    [Id]        INT            IDENTITY(1,1) NOT NULL,
    [UserId]    INT            NOT NULL,
    [EntryDate] DATE           NOT NULL,
    [NoteText]  NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2      NOT NULL CONSTRAINT [DF_DailyNotes_CreatedAt] DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2      NOT NULL CONSTRAINT [DF_DailyNotes_UpdatedAt] DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_DailyNotes]       PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DailyNotes_Users] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_DailyNotes_UserDate] UNIQUE ([UserId], [EntryDate])
);
GO

CREATE INDEX [IX_DailyNotes_UserId] ON [dbo].[DailyNotes] ([UserId]);
GO
