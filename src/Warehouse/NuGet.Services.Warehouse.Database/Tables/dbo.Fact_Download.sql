CREATE TABLE [dbo].[Fact_Download] (
    [Dimension_UserAgent_Id] INT NOT NULL,
    [Dimension_Package_Id]   INT NOT NULL,
    [Dimension_Date_Id]      INT NOT NULL,
    [Dimension_Time_Id]      INT NOT NULL,
    [Dimension_Operation_Id] INT NOT NULL,
    [Dimension_Project_Id]   INT NOT NULL,
    [DownloadCount]          INT NOT NULL,
    CONSTRAINT [PK_Fact_Download_Dim] PRIMARY KEY CLUSTERED ([Dimension_UserAgent_Id] ASC, [Dimension_Package_Id] ASC, [Dimension_Date_Id] ASC, [Dimension_Time_Id] ASC, [Dimension_Operation_Id] ASC, [Dimension_Project_Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [Fact_Download_NCI_Date_Id]
    ON [dbo].[Fact_Download]([Dimension_Date_Id] ASC)
    INCLUDE([Dimension_Package_Id], [DownloadCount]);


GO
CREATE NONCLUSTERED INDEX [Fact_Download_NCI_DownloadCount]
    ON [dbo].[Fact_Download]([DownloadCount] ASC);


GO
CREATE NONCLUSTERED INDEX [Fact_Download_NCI_Package_Id]
    ON [dbo].[Fact_Download]([Dimension_Package_Id] ASC)
    INCLUDE([Dimension_UserAgent_Id], [Dimension_Date_Id], [Dimension_Operation_Id], [DownloadCount]);

