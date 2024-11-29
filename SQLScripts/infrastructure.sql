USE gerund_or_infinitive
GO

CREATE TABLE [Examples] (

	[Id] INT IDENTITY(0, 1) NOT NULL,
	[SourceSentence] NVARCHAR(100) NOT NULL,
	[UsedWord] NVARCHAR(30) NOT NULL,
	[CorrectAnswer] NVARCHAR(30) NOT NULL,
	[AlternativeCorrectAnswer] NVARCHAR(30) NULL,

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

CREATE TABLE [Answers](
	
	[Id] UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL,
	[UserId] BIGINT NOT NULL,
	[ExampleId] INT NOT NULL,
	[ReceivingTime] DATETIME NOT NULL,
	[Result] BIT NOT NULL,

	CONSTRAINT [PK_Answers] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Answers_UserData_UserId] FOREIGN KEY ([UserId]) REFERENCES [UserData] ([UserId])
	ON DELETE CASCADE
	ON UPDATE CASCADE,
	CONSTRAINT [FK_Answers_Examples_ExampleId] FOREIGN KEY ([ExampleId]) REFERENCES [Examples] ([Id])
	ON DELETE NO ACTION
	ON UPDATE NO ACTION
)
GO


ALTER TABLE [UserData]
ADD [ExampleImpressionsJson] VARCHAR(MAX) DEFAULT('') NOT NULL
GO

CREATE VIEW [ShowUsagesOfExample] AS
SELECT [Examples].Id AS [Example id], COUNT([UserData].CurrentExampleId) AS [Usages count]
FROM [Examples]
LEFT JOIN [UserData] ON [UserData].CurrentExampleId = [Examples].Id
GROUP BY [Examples].Id
GO

SELECT * FROM [Examples]
GO

SELECT COUNT(*)
FROM [Examples]
GO


SELECT * FROM [UserData]
GO

SELECT * FROM [ShowUsagesOfExample]
WHERE [Usages count] > 0
GO

INSERT INTO [Answers] ([UserId], [ExampleId], [ReceivingTime], [Result])
VALUES (835300262, 23, GETDATE(), 1)
GO

INSERT INTO [Answers] ([UserId], [ExampleId], [ReceivingTime], [Result])
VALUES (835300262, 30, '2023-09-25 14:30:00', 1)
GO

INSERT INTO [Answers] ([UserId], [ExampleId], [ReceivingTime], [Result])
VALUES (923067232, 30, '2023-09-25 12:50:00', 1)
GO

SELECT * FROM [Answers]
GO

SELECT [Answers].ExampleId AS [Example Id], COUNT(*) AS [Impressions count] 
FROM [Answers]
GROUP BY [Answers].ExampleId
GO

INSERT INTO [UserData] ([UserId], [CurrentExampleId])
VALUES (111369552, 30);
GO


SELECT * FROM sys.tables
GO

EXEC sp_helpdb 'gerund_or_infinitive'
GO

DELETE FROM [Examples]
GO

DELETE FROM [Answers]
GO

DELETE FROM [UserData]
GO

DROP VIEW [ShowUsagesOfExample]
GO

DROP TABLE [Answers]
GO

DROP TABLE [Examples]
GO

DROP TABLE [UserData]
GO

