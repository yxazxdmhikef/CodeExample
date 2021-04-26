
CREATE PROCEDURE [CorpProp.Law].[AutoAddOnOSToRight]	
	@rightID INT
AS
-- =============================================
-- Author: OvsyannikovaME	
-- Create date: 23.02.2021
-- Description:	Поиск и добавление связи ОС/НМА и объекта права.
-- =============================================
BEGIN
	DECLARE @errorText NVARCHAR(MAX) = N'';
	
	DECLARE @beCode NVARCHAR(1000),
	@cadNumb NVARCHAR(1000),
	@regNumb NVARCHAR(1000);

	SELECT TOP 1
	@beCode = be.Code,
	@cadNumb = r.CadastralNumberPU,
	@regNumb = r.RegNumberPU
	FROM [CorpProp.Law].[Right] r WITH (NOLOCK)
	INNER JOIN [CorpProp.Subject].[Society] og WITH (NOLOCK) ON r.SocietyID = og.ID
	INNER JOIN [CorpProp.Base].[DictObject] be WITH (NOLOCK) ON be.ID = og.ConsolidationUnitID
	WHERE r.ID = @rightID

	IF (@beCode IS NOT NULL AND (@cadNumb IS NOT NULL OR @regNumb IS NOT NULL))
	BEGIN

		IF OBJECT_ID(N'tempdb..#addOS') IS NOT NULL DROP TABLE #addOS;
		CREATE TABLE #addOS (ID INT);
		
		IF (@cadNumb IS NOT NULL)
		BEGIN
			INSERT INTO #addOS
			SELECT s.ID
			FROM [CorpProp.Accounting].[AccountingObjectTbl] s WITH (NOLOCK)
			INNER JOIN  [CorpProp.Base].[DictObject] be WITH (NOLOCK) ON s.[ConsolidationID] = be.[ID]
			WHERE s.[Hidden] = 0 AND s.[IsHistory] = 0 AND s.[IsArchived] = 0 
			AND be.[Code] = @beCode
			AND s.[CadastralNumber] IS NOT NULL
			AND s.[CadastralNumber] = @cadNumb 
		END

		IF (@regNumb IS NOT NULL)
		BEGIN
			INSERT INTO #addOS
			SELECT s.ID
			FROM [CorpProp.Accounting].[AccountingObjectTbl] s WITH (NOLOCK)
			INNER JOIN  [CorpProp.Base].[DictObject] be WITH (NOLOCK) ON s.[ConsolidationID] = be.[ID]
			WHERE s.[Hidden] = 0 AND s.[IsHistory] = 0 AND s.[IsArchived] = 0 
			AND be.[Code] = @beCode
			AND s.[RegNumber] IS NOT NULL
			AND s.[RegNumber] = @regNumb 
		END
		
		-- вставка связей
		MERGE INTO [CorpProp.ManyToMany].[RightAndOS] AS target
		USING
		(
			SELECT s.ID
			FROM #addOS s
			GROUP BY s.ID			 
		) AS source ON (target.[ObjRigthId] = source.[ID] AND target.[ObjLeftId] = @rightID AND target.[Hidden] = 0 )
		WHEN NOT MATCHED BY target THEN
		INSERT ([ObjLeftId], [ObjRigthId], [Hidden], [SortOrder])
		VALUES (@rightID, source.[ID], 0, -1);	

	END
	ELSE
	BEGIN
		SET @errorText = 'Невозможно произвести поиск и привязку из-за отсутствия значений ключевых параметров.';
	END

	IF OBJECT_ID(N'tempdb..#addOS') IS NOT NULL DROP TABLE #addOS;
	SELECT(@errorText);	
END