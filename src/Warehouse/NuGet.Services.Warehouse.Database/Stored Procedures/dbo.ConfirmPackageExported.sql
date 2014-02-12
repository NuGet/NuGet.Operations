
CREATE PROCEDURE [dbo].[ConfirmPackageExported]
@PackageId NVARCHAR(128),
@DirtyCount INT
AS
BEGIN
	DELETE PackageReportDirty
	WHERE PackageId = @PackageId
	  AND DirtyCount = @DirtyCount
END