USE gerund_or_infinitive
GO

CREATE TABLE [Examples] (
	[Id] INT IDENTITY(0, 1) NOT NULL PRIMARY KEY,
	[SourceSentence] NVARCHAR(100) NOT NULL,
	[UsedWord] NVARCHAR(30) NOT NULL,
	[CorrectAnswer] NVARCHAR(30) NOT NULL,
)
GO

CREATE TABLE [UserData] (
	[UserId] BIGINT NOT NULL PRIMARY KEY,
	[CurrentExampleId] INT NULL,

	CONSTRAINT [FK_UserData_Examples_CurrentExampleId] FOREIGN KEY ([CurrentExampleId]) REFERENCES [Examples] ([Id])
)
GO


SELECT * FROM [Examples]
GO

SELECT * FROM [UserData]
GO

INSERT INTO [UserData] ([UserId], [CurrentExampleId])
VALUES (923067232, 0);
GO


INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('I can''t ... go on holiday this summer.', 'afford', 'afford to');

INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('They have ... call off the wedding.', 'decide', 'decided to');

 GO

DROP TABLE [Examples]
GO

DROP TABLE [UserData]
GO
