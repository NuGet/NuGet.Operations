CREATE TABLE [dbo].[PackageReportDirty] (
    [PackageId]  NVARCHAR (128) NOT NULL,
    [DirtyCount] INT            NULL,
    CONSTRAINT [PK_PackageReportDirty] PRIMARY KEY CLUSTERED ([PackageId] ASC)
);

