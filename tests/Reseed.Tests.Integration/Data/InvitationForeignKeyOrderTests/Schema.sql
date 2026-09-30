CREATE TABLE [dbo].[Invitation]
(
	[Id] int NOT NULL,
	[CreatorId] int NOT NULL,
	[UserId] int NULL,
	CONSTRAINT [PK_Invitation] PRIMARY KEY CLUSTERED ([Id]) WITH (DATA_COMPRESSION = PAGE)
);

CREATE TABLE [dbo].[User]
(
	[Id] int NOT NULL,
	[InvitationId] int NULL,
	CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id]) WITH (DATA_COMPRESSION = PAGE)
);

ALTER TABLE [dbo].[Invitation] ADD CONSTRAINT [FK_Invitation_User]
	FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id]);

ALTER TABLE [dbo].[User] ADD CONSTRAINT [FK_User_Invitation]
	FOREIGN KEY ([InvitationId]) REFERENCES [dbo].[Invitation] ([Id]);

ALTER TABLE [dbo].[Invitation] ADD CONSTRAINT [FK_Invitation_Inviter]
	FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[User] ([Id]);
