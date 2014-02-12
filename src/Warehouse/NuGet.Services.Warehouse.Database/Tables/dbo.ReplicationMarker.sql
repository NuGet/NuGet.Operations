CREATE TABLE [dbo].[ReplicationMarker] (
    [LastOriginalKey] INT NOT NULL,
    CONSTRAINT [PK_ReplicationMarker] PRIMARY KEY CLUSTERED ([LastOriginalKey] ASC)
);

