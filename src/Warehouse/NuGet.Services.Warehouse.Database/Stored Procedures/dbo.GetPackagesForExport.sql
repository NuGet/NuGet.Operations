
CREATE PROCEDURE [dbo].[GetPackagesForExport]
AS
BEGIN
	SELECT PackageId, DirtyCount
	FROM PackageReportDirty
END