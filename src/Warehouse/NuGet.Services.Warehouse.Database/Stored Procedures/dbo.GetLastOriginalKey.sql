
CREATE PROCEDURE [dbo].[GetLastOriginalKey]
@OriginalKey INT OUTPUT
AS
BEGIN
	SELECT @OriginalKey = LastOriginalKey
	FROM ReplicationMarker
END