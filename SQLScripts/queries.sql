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
VALUES ('I can''t afford ... on holiday this summer.', 'go', 'to go');

INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('They have decided ... off the wedding.', 'call', 'to call');

INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('I always try to avoid ... in the rush hour', 'drive', 'driving');

INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('It isn''t worth ... to the exhibition. It''s realy boring.', 'go', 'going');

INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer])
VALUES ('I have agreed ... David back the money he lent me next week.', 'pay', 'to pay');


 GO

DELETE FROM [Examples]
GO

DELETE FROM [UserData]
GO

DROP TABLE [Examples]
GO

DROP TABLE [UserData]
GO
