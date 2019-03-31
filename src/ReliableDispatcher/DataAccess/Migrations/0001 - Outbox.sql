CREATE TABLE [dbo].[Outbox] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [Body]             XML              NOT NULL,
    [DispatchedDate]   DATETIME2 (7)    NULL,
    [DispatchAttempts] INT              DEFAULT ((0)) NOT NULL,
	[CreatedDate]       DATETIME2 (7)    DEFAULT (GETDATE()) NOT NULL,
    CONSTRAINT [PK_Outbox] PRIMARY KEY CLUSTERED ([Id] ASC)
);

GO
CREATE NONCLUSTERED INDEX [IX_Outbox_DispatchedDate]
    ON [dbo].[Outbox]([DispatchedDate] ASC) WHERE ([DispatchedDate] IS NULL);