CREATE TABLE [dbo].[Fact_Download_Old] (
    [Dimension_UserAgent_Id] INT NOT NULL,
    [Dimension_Package_Id]   INT NOT NULL,
    [Dimension_Date_Id]      INT NOT NULL,
    [Dimension_Time_Id]      INT NOT NULL,
    [Dimension_Operation_Id] INT NOT NULL,
    [DownloadCount]          INT NULL,
    [Dimension_Project_Id]   INT DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Fact_Download] PRIMARY KEY CLUSTERED ([Dimension_UserAgent_Id] ASC, [Dimension_Package_Id] ASC, [Dimension_Date_Id] ASC, [Dimension_Time_Id] ASC, [Dimension_Operation_Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [Fact_Download_2]
    ON [dbo].[Fact_Download_Old]([DownloadCount] ASC);

