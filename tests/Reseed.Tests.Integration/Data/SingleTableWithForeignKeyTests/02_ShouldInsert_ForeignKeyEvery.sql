CREATE TABLE [User] (
	Id int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
	FirstName nvarchar(100) NOT NULL,
	LastName nvarchar(100) NOT NULL,
	ManagerId int NULL,
	Age int NULL,

	CONSTRAINT [FK_User_ManagerId] FOREIGN KEY (ManagerId) REFERENCES [dbo].[User](Id)
)