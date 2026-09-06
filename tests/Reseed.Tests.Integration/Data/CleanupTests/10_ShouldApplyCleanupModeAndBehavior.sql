CREATE TABLE [dbo].[Parent] (
	Id int NOT NULL PRIMARY KEY
);

CREATE TABLE [dbo].[Child] (
	Id int NOT NULL PRIMARY KEY,
	ParentId int NOT NULL,
	CONSTRAINT [FK_Child_Parent]
		FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Parent] ([Id])
);

CREATE TABLE [dbo].[UnrelatedParent] (
	Id int NOT NULL PRIMARY KEY
);

CREATE TABLE [dbo].[UnrelatedChild] (
	Id int NOT NULL PRIMARY KEY,
	ParentId int NOT NULL,
	CONSTRAINT [FK_UnrelatedChild_UnrelatedParent]
		FOREIGN KEY ([ParentId]) REFERENCES [dbo].[UnrelatedParent] ([Id])
);

INSERT INTO [dbo].[Parent] ([Id]) VALUES (1);
INSERT INTO [dbo].[Child] ([Id], [ParentId]) VALUES (1, 1);
INSERT INTO [dbo].[UnrelatedParent] ([Id]) VALUES (1);
INSERT INTO [dbo].[UnrelatedChild] ([Id], [ParentId]) VALUES (1, 1);
