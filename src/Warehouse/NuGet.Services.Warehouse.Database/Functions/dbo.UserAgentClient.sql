
--  'Nexus' refer to www.sonatype.com for details on NuGet integration
--  'JetBrains TeamCity' refer to www.jetbrains.com for details on NuGet integration
--  'Artifactory' refer to www.jfrog.com for details on NuGet integration

CREATE FUNCTION [dbo].[UserAgentClient] (@value nvarchar(900))
RETURNS NVARCHAR(128)
AS
BEGIN
	IF CHARINDEX('NuGet Add Package Dialog', @value) > 0 
		RETURN 'NuGet Add Package Dialog'
	IF CHARINDEX('NuGet Command Line', @value) > 0 
		RETURN 'NuGet Command Line'
	IF CHARINDEX('NuGet Package Explorer Metro', @value) > 0 
		RETURN 'NuGet Package Explorer Metro'
	IF CHARINDEX('NuGet Package Explorer', @value) > 0 
		RETURN 'NuGet Package Explorer'
	IF CHARINDEX('NuGet Package Manager Console', @value) > 0 
		RETURN 'NuGet Package Manager Console'
	IF CHARINDEX('NuGet Visual Studio Extension', @value) > 0 
		RETURN 'NuGet Visual Studio Extension'
	IF CHARINDEX('WebMatrix', @value) > 0 
		RETURN 'WebMatrix'
	IF CHARINDEX('Package-Installer', @value) > 0 
		RETURN 'Package-Installer'
	IF CHARINDEX('JetBrains TeamCity', @value) > 0 
		RETURN 'JetBrains TeamCity'
	IF CHARINDEX('Nexus', @value) > 0 
		RETURN 'Sonatype Nexus'
	IF CHARINDEX('Artifactory', @value) > 0 
		RETURN 'JFrog Artifactory'
	RETURN 'Other'
END