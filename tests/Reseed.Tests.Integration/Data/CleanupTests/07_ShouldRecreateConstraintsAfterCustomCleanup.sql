CREATE TABLE [dbo].[Parent] (
	Id int NOT NULL PRIMARY KEY
);

CREATE TABLE [dbo].[Child] (
	Id int NOT NULL PRIMARY KEY,
	ParentId int NOT NULL,
	CONSTRAINT [FK_Child_Parent]
		FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Parent] ([Id])
);

INSERT INTO [dbo].[Parent] ([Id]) VALUES (1);
INSERT INTO [dbo].[Child] ([Id], [ParentId]) VALUES (1, 1);
