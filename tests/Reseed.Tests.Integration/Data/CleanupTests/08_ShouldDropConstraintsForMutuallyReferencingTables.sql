CREATE TABLE [dbo].[Left] (
	Id int NOT NULL PRIMARY KEY,
	RightId int NULL
);

CREATE TABLE [dbo].[Right] (
	Id int NOT NULL PRIMARY KEY,
	LeftId int NULL
);

ALTER TABLE [dbo].[Left]
	ADD CONSTRAINT [FK_Left_Right]
	FOREIGN KEY ([RightId]) REFERENCES [dbo].[Right] ([Id]);

ALTER TABLE [dbo].[Right]
	ADD CONSTRAINT [FK_Right_Left]
	FOREIGN KEY ([LeftId]) REFERENCES [dbo].[Left] ([Id]);

INSERT INTO [dbo].[Left] ([Id]) VALUES (1);
INSERT INTO [dbo].[Right] ([Id], [LeftId]) VALUES (1, 1);
UPDATE [dbo].[Left] SET [RightId] = 1 WHERE [Id] = 1;
