CREATE TABLE [User] (
	Id int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
	FirstName nvarchar(100) NOT NULL,
	LastName nvarchar(100) NOT NULL,
	Age int NULL
);

INSERT INTO [User] (FirstName, LastName, Age)
VALUES (N'John', N'Doe', 30);

CREATE TABLE [dbo].[Child] (
	Id int NOT NULL PRIMARY KEY,
	UserId int NOT NULL,
	CONSTRAINT [FK_Child_User]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id])
);

INSERT INTO [dbo].[Child] ([Id], [UserId]) VALUES (1, 1);

GO

CREATE VIEW [dbo].[viUser]
WITH SCHEMABINDING
AS
	SELECT [Id], [FirstName], [LastName]
	FROM [dbo].[User];

GO

CREATE UNIQUE CLUSTERED INDEX [IX_viUser_Id]
	ON [dbo].[viUser] ([Id]);
