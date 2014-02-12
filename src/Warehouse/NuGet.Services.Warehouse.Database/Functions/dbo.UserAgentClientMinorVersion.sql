
CREATE FUNCTION [dbo].[UserAgentClientMinorVersion] (@value NVARCHAR(900))
RETURNS INT
AS
BEGIN
	IF (CHARINDEX('NuGet Add Package Dialog/', @value) > 0
		OR CHARINDEX('NuGet Command Line/', @value) > 0
		OR CHARINDEX('NuGet Package Explorer/', @value) > 0
		OR CHARINDEX('NuGet Package Manager Console/', @value) > 0
		OR CHARINDEX('NuGet Visual Studio Extension/', @value) > 0
		OR CHARINDEX('WebMatrix', @value) > 0
		OR CHARINDEX('Package-Installer/', @value) > 0)

		RETURN CAST(SUBSTRING(
				@value, 
				CHARINDEX('.', @value, CHARINDEX('/', @value) + 1) + 1, 
				(CHARINDEX('.', CONCAT(@value, '.'), CHARINDEX('.', @value, CHARINDEX('/', @value) + 1) + 1)) - ((CHARINDEX('.', @value, CHARINDEX('/', @value) + 1)) + 1)
			) AS INT)

	RETURN 0
END