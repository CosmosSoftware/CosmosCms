
IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ArticleLocks')
BEGIN
	ALTER TABLE [dbo].[ArticleLocks] DROP CONSTRAINT IF EXISTS [FK_ArticleLock_Articles_ArticleId];
ALTER TABLE [dbo].[ArticleLocks] DROP CONSTRAINT IF EXISTS [FK_ArticleLock_AspNetUsers_IdentityUserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ArticleLogs')
BEGIN
ALTER TABLE [dbo].[ArticleLogs] DROP CONSTRAINT IF EXISTS [FK_ArticleLogs_Articles_ArticleId];
ALTER TABLE [dbo].[ArticleLogs] DROP CONSTRAINT IF EXISTS [FK_ArticleLogs_AspNetUsers_IdentityUserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Articles')
BEGIN
ALTER TABLE [dbo].[Articles] DROP CONSTRAINT IF EXISTS [FK_Articles_Layouts_LayoutId];
ALTER TABLE [dbo].[Articles] DROP CONSTRAINT IF EXISTS [FK_Articles_Teams_TeamId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ArticleLocks')
BEGIN
ALTER TABLE [dbo].[AspNetRoleClaims] DROP CONSTRAINT IF EXISTS [FK_AspNetRoleClaims_AspNetRoles_RoleId];
ALTER TABLE [dbo].[AspNetUserClaims] DROP CONSTRAINT IF EXISTS [FK_AspNetUserClaims_AspNetUsers_UserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins')
BEGIN
ALTER TABLE [dbo].[AspNetUserLogins] DROP CONSTRAINT IF EXISTS [FK_AspNetUserLogins_AspNetUsers_UserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
BEGIN
ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT IF EXISTS [FK_AspNetUserRoles_AspNetRoles_RoleId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
BEGIN
ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT IF EXISTS [FK_AspNetUserRoles_AspNetUsers_UserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens')
BEGIN
ALTER TABLE [dbo].[AspNetUserTokens] DROP CONSTRAINT IF EXISTS [FK_AspNetUserTokens_AspNetUsers_UserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MenuItems')
BEGIN
ALTER TABLE [dbo].[MenuItems] DROP CONSTRAINT IF EXISTS [FK_MenuItems_Articles_ArticleId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TeamMembers')
BEGIN
ALTER TABLE [dbo].[TeamMembers] DROP CONSTRAINT IF EXISTS [FK_TeamMembers_Teams_TeamId];
ALTER TABLE [dbo].[TeamMembers] DROP CONSTRAINT IF EXISTS [FK_TeamMembers_AspNetUsers_UserId];
END

IF EXISTS (select * from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Templates')
BEGIN
ALTER TABLE [dbo].[Templates] DROP CONSTRAINT IF EXISTS [FK_Templates_Layouts_LayoutId];
END

DROP TABLE IF EXISTS [dbo].[__EFMigrationsHistory];
DROP TABLE IF EXISTS [dbo].[__EFMigrationsHistory_tracking];
DROP TABLE IF EXISTS [dbo].[ArticleLocks];
DROP TABLE IF EXISTS [dbo].[ArticleLocks_tracking];
DROP TABLE IF EXISTS [dbo].[ArticleLogs];
DROP TABLE IF EXISTS [dbo].[ArticleLogs_tracking];
DROP TABLE IF EXISTS [dbo].[Articles];
DROP TABLE IF EXISTS [dbo].[Articles_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetRoleClaims];
DROP TABLE IF EXISTS [dbo].[AspNetRoleClaims_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetRoles];
DROP TABLE IF EXISTS [dbo].[AspNetRoles_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetUserClaims];
DROP TABLE IF EXISTS [dbo].[AspNetUserClaims_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetUserLogins];
DROP TABLE IF EXISTS [dbo].[AspNetUserLogins_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetUserRoles];
DROP TABLE IF EXISTS [dbo].[AspNetUserRoles_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetUsers];
DROP TABLE IF EXISTS [dbo].[AspNetUsers_tracking];
DROP TABLE IF EXISTS [dbo].[AspNetUserTokens];
DROP TABLE IF EXISTS [dbo].[AspNetUserTokens_tracking];
DROP TABLE IF EXISTS [dbo].[Layouts];
DROP TABLE IF EXISTS [dbo].[Layouts_tracking];
DROP TABLE IF EXISTS [dbo].[MenuItems];
DROP TABLE IF EXISTS [dbo].[MenuItems_tracking];
DROP TABLE IF EXISTS [dbo].[scope_info];
DROP TABLE IF EXISTS [dbo].[scope_info_history];
DROP TABLE IF EXISTS [dbo].[scope_info_server];
DROP TABLE IF EXISTS [dbo].[TeamMembers];
DROP TABLE IF EXISTS [dbo].[TeamMembers_tracking];
DROP TABLE IF EXISTS [dbo].[Teams];
DROP TABLE IF EXISTS [dbo].[Teams_tracking];
DROP TABLE IF EXISTS [dbo].[Templates];
DROP TABLE IF EXISTS [dbo].[Templates_tracking];