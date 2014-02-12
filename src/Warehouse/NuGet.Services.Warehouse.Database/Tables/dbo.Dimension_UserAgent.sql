CREATE TABLE [dbo].[Dimension_UserAgent] (
    [Id]                 INT           IDENTITY (1, 1) NOT NULL,
    [Value]              VARCHAR (900) NULL,
    [Client]             VARCHAR (128) NULL,
    [ClientMajorVersion] INT           NULL,
    [ClientMinorVersion] INT           NULL,
    [ClientCategory]     VARCHAR (64)  NULL,
    CONSTRAINT [PK_Dimension_UserAgent] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [Dimension_UserAgent_2]
    ON [dbo].[Dimension_UserAgent]([Value] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_3]
    ON [dbo].[Dimension_UserAgent]([Client] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_4]
    ON [dbo].[Dimension_UserAgent]([ClientMajorVersion] ASC, [ClientMinorVersion] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_5]
    ON [dbo].[Dimension_UserAgent]([ClientCategory] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_NCI_Client]
    ON [dbo].[Dimension_UserAgent]([Client] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_NCI_ClientCategory]
    ON [dbo].[Dimension_UserAgent]([ClientCategory] ASC);


GO
CREATE NONCLUSTERED INDEX [Dimension_UserAgent_NCI_ClientMajorVersion_ClientMinorVersion]
    ON [dbo].[Dimension_UserAgent]([ClientMajorVersion] ASC, [ClientMinorVersion] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [Dimension_UserAgent_NCI_Value]
    ON [dbo].[Dimension_UserAgent]([Value] ASC);

