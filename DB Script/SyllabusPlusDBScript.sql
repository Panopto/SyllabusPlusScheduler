CREATE TABLE [dbo].[Schedules] (
    [ID] [int] NOT NULL IDENTITY,
    [sessionName] [nvarchar](255),
    [folderID] [uniqueidentifier] NOT NULL,
    [primaryRemoteRecorderID] [uniqueidentifier] NOT NULL,
    [secondaryRemoteRecorderID] [uniqueidentifier],
    [startTime] [datetime] NOT NULL,
    [duration] [int] NOT NULL,
    [presenterUsername] [nvarchar](255),
    [cancelSchedule] [bit],
    [webcast] [bit] NOT NULL,
    [scheduledSessionID] [uniqueidentifier],
    [lastUpdate] [datetime] NOT NULL,
    [lastPanoptoSync] [datetime],
    [panoptoSyncSuccess] [bit],
    [numberOfAttempts] [int] NOT NULL,
    [errorResponse] [nvarchar](max),
    CONSTRAINT [PK_dbo.Schedules] PRIMARY KEY ([ID])
)