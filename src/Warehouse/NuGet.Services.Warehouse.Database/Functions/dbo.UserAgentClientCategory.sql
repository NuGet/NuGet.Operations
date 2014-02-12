
CREATE FUNCTION [dbo].[UserAgentClientCategory] (@value NVARCHAR(900))
RETURNS VARCHAR(64)
AS
BEGIN
	IF (CHARINDEX('NuGet Add Package Dialog', @value) > 0
		OR CHARINDEX('NuGet Command Line', @value) > 0
		OR CHARINDEX('NuGet Package Explorer Metro', @value) > 0
		OR CHARINDEX('NuGet Package Explorer', @value) > 0
		OR CHARINDEX('NuGet Package Manager Console', @value) > 0
		OR CHARINDEX('NuGet Visual Studio Extension', @value) > 0
		OR CHARINDEX('Package-Installer', @value) > 0)
		RETURN 'NuGet'

	IF CHARINDEX('WebMatrix', @value) > 0
		RETURN 'WebMatrix'

	IF (CHARINDEX('Mozilla', @value) > 0 or CHARINDEX('Opera', @value) > 0)
		RETURN 'Browser'

	RETURN ''
END