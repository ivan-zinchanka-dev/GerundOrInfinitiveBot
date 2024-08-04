USE gerund_or_infinitive
GO

CREATE TABLE [Examples] (
	[Id] INT IDENTITY(0, 1) NOT NULL,
	[SourceSentence] NVARCHAR(100) NOT NULL,
	[UsedWord] NVARCHAR(30) NOT NULL,
	[CorrectAnswer] NVARCHAR(30) NOT NULL,

	CONSTRAINT [PK_Examples] PRIMARY KEY ([Id])
)
GO

CREATE TABLE [UserData] (
	[UserId] BIGINT NOT NULL,
	[CurrentExampleId] INT NULL,

	CONSTRAINT [PK_UserData] PRIMARY KEY ([UserId]),
	CONSTRAINT [FK_UserData_Examples_CurrentExampleId] FOREIGN KEY ([CurrentExampleId]) REFERENCES [Examples] ([Id])
	ON DELETE SET NULL
	ON UPDATE CASCADE
)
GO

CREATE VIEW [ShowUsagesOfExample] AS
SELECT [Examples].Id AS [Example id], COUNT([UserData].CurrentExampleId) AS [Usages count]
FROM [Examples]
         LEFT JOIN [UserData] ON [UserData].CurrentExampleId = [Examples].Id
GROUP BY [Examples].Id
GO

SELECT * FROM [Examples]
GO

SELECT * FROM [UserData]
GO

SELECT * FROM [ShowUsagesOfExample]
WHERE [Usages count] > 0
GO


INSERT INTO [UserData] ([UserId], [CurrentExampleId])
VALUES (111369552, 3);
GO

DELETE FROM [UserData]
GO

/*DROP VIEW [ShowUsagesOfExample]
GO

DROP TABLE [Examples]
GO

DROP TABLE [UserData]
GO

SELECT * FROM sys.tables
GO*/
