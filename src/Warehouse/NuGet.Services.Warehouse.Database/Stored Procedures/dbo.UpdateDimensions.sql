CREATE PROCEDURE [dbo].[UpdateDimensions]
    @StartDate DATETIME = '2010-01-01',
    @EndDate DATETIME = '2020-12-31'
AS
   
SET XACT_ABORT ON
SET NOCOUNT ON

-- Populate Null Date Dimension if not already present
IF NOT EXISTS(SELECT * FROM [dbo].[Dimension_Date] WHERE Id = -1)
BEGIN

    SET IDENTITY_INSERT [dbo].[Dimension_Date] ON

    INSERT INTO [dbo].[Dimension_Date] (
/*1*/   [Id]
        ,[Date]
        ,[DateName]
        ,[DayOfWeek]
        ,[DayOfWeekName]
        ,[MonthName]
        ,[WeekdayIndicator]
        ,[DayOfYear]
        ,[WeekOfYear]
/*10*/	,[WeekOfYearName]
        ,[WeekOfYearNameInYear]
        ,[MonthOfYear]
        ,[MonthOfYearName]
        ,[MonthOfYearNameInYear]
        ,[Quarter]
        ,[QuarterName]
        ,[QuarterNameInYear]
        ,[HalfYear]
        ,[HalfYearName]
/*20*/	,[HalfYearNameInYear]
        ,[Year]
        ,[YearName]
        ,[FiscalDayOfYear]
        ,[FiscalWeekOfYear]
        ,[FiscalWeekOfYearName]
        ,[FiscalWeekOfYearNameInYear]
        ,[FiscalMonthOfYear]
        ,[FiscalMonthOfYearName]
        ,[FiscalMonthOfYearNameInYear]
/*30*/  ,[FiscalQuarter]
        ,[FiscalQuarterName]
        ,[FiscalQuarterNameInYear]
        ,[FiscalHalfYear]
        ,[FiscalHalfYearName]
        ,[FiscalHalfYearNameInYear]
        ,[FiscalYear]
/*37*/	,[FiscalYearName])
    OUTPUT 'INSERT' AS [Action], 'Dimension_Date' AS [Table], INSERTED.*
    VALUES(
/*1*/	-1
        ,null
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,null
/*10*/	,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
/*20*/	,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,null
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
/*30*/	,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
        ,'(Unknown)'
        ,'(Unknown)'
        ,null
/*37*/	,'(Unknown)')

    SET IDENTITY_INSERT [dbo].[Dimension_Date] OFF
    
END

DECLARE @FYDays INT, @FYWeek INT, @FYMonth INT, @FYQuarter INT, @FYYear INT, @FYStartDate DATETIME

DECLARE @Dates TABLE (
    [Date] DATETIME,
    [FYDays] INT,
    [FYWeek] INT,
    [FYMonth] INT,
    [FYQuarter] INT,
    [FYYear] INT
)

-- Iterate over days between start and end
WHILE @StartDate <= @EndDate
BEGIN
    -- Calculate Fiscal Year information
    SET @FYStartDate = '7/1/' + 
        CASE WHEN DATEPART(MONTH, @StartDate) < 7 THEN CAST(DATEPART(YEAR, @StartDate) - 1 AS NVARCHAR(4))
        ELSE CAST(DATEPART(YEAR, @StartDate) AS NVARCHAR(4))
    END
    SET @FYDays = DATEDIFF(DAY, @FYStartDate, @StartDate) + 1
    SET @FYWeek = DATEDIFF(WEEK, @FYStartDate, @StartDate) + 1
    SET @FYMonth = DATEDIFF(MONTH, @FYStartDate, @StartDate) + 1
    SET @FYQuarter = CASE
        WHEN @FYMonth BETWEEN 1 AND 3 THEN 1
        WHEN @FYMonth BETWEEN 4 AND 6 THEN 2
        WHEN @FYMonth BETWEEN 7 AND 9 THEN 3
        WHEN @FYMonth BETWEEN 10 AND 12 THEN 4
    END
    SET @FYYear = DATEPART(YEAR, @FYStartDate) + 1

    -- Insert values into the a temp table
    INSERT INTO @Dates(
        [Date],
        [FYDays],
        [FYWeek],
        [FYMonth],
        [FYQuarter],
        [FYYear]
    ) 
    VALUES(
        @StartDate,
        @FYDays,
        @FYWeek,
        @FYMonth,
        @FYQuarter,
        @FYYear
    )

    SET @StartDate = DATEADD(d, 1, @StartDate)
END

-- Merge the temp table into the main table
MERGE [dbo].[Dimension_Date] targ
USING @Dates src
ON (src.[Date] = targ.[Date])
WHEN NOT MATCHED THEN
    INSERT (
        [Date]
        ,[DateName]
        ,[DayOfWeek]
        ,[DayOfWeekName]
        ,[MonthName]
        ,[WeekdayIndicator]
-- CY			
        ,[DayOfYear]
        ,[WeekOfYear]
        ,[WeekOfYearName]
        ,[WeekOfYearNameInYear]
        ,[MonthOfYear]
        ,[MonthOfYearName]
        ,[MonthOfYearNameInYear]
        ,[Quarter]
        ,[QuarterName]
        ,[QuarterNameInYear]
        ,[HalfYear]
        ,[HalfYearName]
        ,[HalfYearNameInYear]
        ,[Year]
        ,[YearName]
-- FY			
        ,[FiscalDayOfYear]
        ,[FiscalWeekOfYear]
        ,[FiscalWeekOfYearName]
        ,[FiscalWeekOfYearNameInYear]
        ,[FiscalMonthOfYear]
        ,[FiscalMonthOfYearName]
        ,[FiscalMonthOfYearNameInYear]
        ,[FiscalQuarter]
        ,[FiscalQuarterName]
        ,[FiscalQuarterNameInYear]
        ,[FiscalHalfYear]
        ,[FiscalHalfYearName]
        ,[FiscalHalfYearNameInYear]
        ,[FiscalYear]
        ,[FiscalYearName])
    VALUES (
        src.[Date]
        ,DATENAME(WEEKDAY, src.[Date]) + ', ' + DATENAME(MONTH, src.[Date]) + ' ' + DATENAME(DAY, src.[Date]) + ' ' + DATENAME(YEAR, src.[Date])
        ,DATEPART(WEEKDAY, src.[Date])
        ,DATENAME(WEEKDAY, src.[Date])
        ,DATENAME(month, src.[Date])
        ,CASE WHEN DATEPART(WEEKDAY, src.[Date]) > 1 AND DATEPART(WEEKDAY, src.[Date]) < 7 THEN 'Weekday' ELSE 'Weekend' END
-- CY			
        ,DATEPART(DAYOFYEAR, src.[Date])
        ,DATEPART(WEEK, src.[Date])
        ,'Week ' + CAST(DATEPART(WEEK, src.[Date]) AS NVARCHAR(2))
        ,'CY ' + DATENAME(YEAR, src.[Date]) + '-Week ' + CAST(DATEPART(WEEK, src.[Date]) AS NVARCHAR(2))
        ,DATEPART(MONTH, src.[Date])
        ,'Month ' + CAST(DATEPART(MONTH, src.[Date]) as nvarchar(2))
        ,'CY ' + DATENAME(YEAR, src.[Date]) + '-' + RIGHT(REPLICATE('0',2) + CAST(DATEPART(MONTH, src.[Date]) AS NVARCHAR(2)),2)
        ,DATEPART(QUARTER, src.[Date])
        ,'Q' + CAST(DATEPART(QUARTER, src.[Date]) AS NVARCHAR(2))
        ,'CY ' + DATENAME(YEAR, src.[Date]) + '-' + 'Q' + CAST(DATEPART(QUARTER, src.[Date]) AS NVARCHAR(1))
        ,CASE WHEN DATEPART(MONTH, src.[Date]) < 7 THEN 1 ELSE 2 END
        ,'H' + CAST(CASE WHEN DATEPART(MONTH, src.[Date]) < 7 THEN 1 ELSE 2 END AS NVARCHAR(1))
        ,'CY ' + DATENAME(YEAR, src.[Date]) + '-' + 'H' + CAST(CASE WHEN DATEPART(MONTH, src.[Date]) < 7 THEN 1 ELSE 2 END AS NVARCHAR(1))
        ,DATEPART(YEAR, src.[Date])
        ,'CY ' + DATENAME(YEAR, src.[Date])
-- FY			
        ,src.[FYDays]
        ,src.[FYWeek]
        ,'Week ' + CAST(src.[FYWeek] AS NVARCHAR(2))
        ,'FY ' + CAST(src.[FYYear] AS NVARCHAR(4)) + '-Week ' + CAST(src.[FYWeek] AS NVARCHAR(2))
        ,src.[FYMonth]
        ,'Month ' + CAST(src.[FYMonth] AS NVARCHAR(2))
        ,'FY ' + CAST(src.[FYYear] AS NVARCHAR(4)) + '-' + RIGHT(REPLICATE('0',2) + CAST(src.[FYMonth] AS NVARCHAR(2)),2)
        ,src.[FYQuarter]
        ,'Q' + CAST(src.[FYQuarter] AS NVARCHAR(2))
        ,'FY ' + CAST(src.[FYYear] AS NVARCHAR(4)) + '-' + 'Q' + CAST(src.[FYQuarter] AS NVARCHAR(2))
        ,CASE WHEN src.[FYMonth] < 7 THEN 1 ELSE 2 END
        ,'H' + CAST(CASE WHEN src.[FYMonth] < 7 THEN 1 ELSE 2 END AS NVARCHAR(1))
        ,'FY ' + CAST(src.[FYYear] AS NVARCHAR(4)) + '-' + 'H' + CAST(CASE WHEN src.[FYMonth] < 7 THEN 1 ELSE 2 END AS NVARCHAR(1))
        ,src.[FYYear]
        ,'FY ' + CAST(src.[FYYear] AS NVARCHAR(4))
    )
OUTPUT $action AS [Action], 'Dimension_Date' AS [Table], INSERTED.*;

IF ((SELECT COUNT(*) FROM [dbo].[Dimension_Time]) <> 24)
BEGIN
    DECLARE @Times TABLE(
        [HourOfDay] INT
    )

    -- Insert a row in to the temp table for each hour
    DECLARE @current INT = 0;
    WHILE (@current < 24)
    BEGIN
        INSERT @Times ( HourOfDay ) VALUES ( @current );
        SET @current = @current + 1;
    END

    -- Merge in to the main table
    MERGE [dbo].[Dimension_Time] targ
    USING @Times src
    ON (targ.HourOfDay = src.HourOfDay)
    WHEN NOT MATCHED THEN
        INSERT (HourOfDay) VALUES(src.HourOfDay)
    OUTPUT $action AS [Action], 'Dimension_Time' AS [Table], INSERTED.*;
END

-- Merge new values in to the other dimensions
MERGE [dbo].[Dimension_Operation] targ
USING(
    VALUES  ( 'Install' ), 
            ( 'Update' ), 
            ( '(unknown)' ), 
            ( 'Install-Dependency' ), 
            ( 'Update-Dependency' ), 
            ( 'Restore-Dependency' )
    ) AS src (Operation)
ON (src.Operation = targ.Operation)
WHEN NOT MATCHED THEN
    INSERT(Operation) VALUES(src.Operation)
OUTPUT $action AS [Action], 'Dimension_Operation' AS [Table], INSERTED.*;

MERGE [dbo].[Dimension_Project] targ
USING(
    VALUES ( '(unknown)' )
    ) AS src (ProjectTypes)
ON (targ.ProjectTypes = src.ProjectTypes)
WHEN NOT MATCHED THEN
    INSERT(ProjectTypes) VALUES(src.ProjectTypes)
OUTPUT $action AS [Action], 'Dimension_' AS [Table], INSERTED.*;
