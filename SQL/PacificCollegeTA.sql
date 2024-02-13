use pstar;


IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PacificCollegeRegData')
BEGIN
	CREATE TABLE PacificCollegeRegData (
		randtoken varchar(256) NOT NULL,
		created datetime NOT NULL,
		firstname varchar(256) NOT NULL,
		lastname varchar(256) NOT NULL,
		email varchar(256) NOT NULL,
		phone varchar(256) NOT NULL,
		campus varchar(256) NOT NULL,
    studentid varchar(256) NOT NULL,
		program varchar(256),
		username varchar(256),
		regcompleted datetime,
		PRIMARY KEY (randtoken)
	)
END
ELSE
BEGIN
	-- If the table already exists, check if 'program' column exists, and add if it doesn't exist
	IF NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = N'PacificCollegeRegData' AND COLUMN_NAME = N'program')
		ALTER TABLE PacificCollegeRegData ADD program VARCHAR(256);
END;
GO


IF EXISTS (SELECT name FROM sysobjects WHERE name = 'createPacificCollegeRegistration' AND type = 'P')
    DROP PROCEDURE createPacificCollegeRegistration
go
CREATE PROCEDURE createPacificCollegeRegistration
	@randtoken varchar(256),
	@firstname varchar(256), 
	@lastname varchar(256),
	@email varchar(256),
	@phone varchar(256),
	@campus varchar(256),
  @studentid varchar(256),
  -- @program varchar(256), -- uncomment if you want to add this from the form
	@errnum int output,
	@errstr varchar(256) output
AS
BEGIN TRY
	insert into PacificCollegeRegData(randtoken, created, firstname, lastname, email, phone, campus, studentid, program) 
		select @randtoken, GETUTCDATE(), @firstname, @lastname, @email, @phone, @campus, '', ''; -- if passing in, use @program instead of ''
		
	set @errnum = 0;
	set @errstr = 'Successfully inserted registration data for token: ' + @randtoken;
END TRY
BEGIN CATCH
 	set @errnum = ERROR_NUMBER()
 	set @errstr = ERROR_MESSAGE()
END CATCH
go


IF EXISTS (SELECT name FROM sysobjects WHERE name = 'getPacificCollegeRegistration' AND type = 'P')
    DROP PROCEDURE getPacificCollegeRegistration
go
CREATE PROCEDURE getPacificCollegeRegistration
	@randtoken varchar(256),
	@errnum int output,
	@errstr varchar(256) output
AS
BEGIN TRY
	-- Using "regcompleted IS NULL" to ensure registration tokens CANNOT be reused!
	select * from PacificCollegeRegData where randtoken = @randtoken and regcompleted IS NULL;
		
	set @errnum = 0;
	set @errstr = 'Successfully queried registration data for token: ' + @randtoken;
END TRY
BEGIN CATCH
 	set @errnum = ERROR_NUMBER()
 	set @errstr = ERROR_MESSAGE()
END CATCH
go


IF EXISTS (SELECT name FROM sysobjects WHERE name = 'updatePacificCollegeRegistration' AND type = 'P')
    DROP PROCEDURE updatePacificCollegeRegistration
go
CREATE PROCEDURE updatePacificCollegeRegistration
	@randtoken varchar(256),
	@username varchar(256), 
	@errnum int output,
	@errstr varchar(256) output
AS
BEGIN TRY
	update PacificCollegeRegData set username = @username, regcompleted = GETUTCDATE()
		where randtoken = @randtoken and regcompleted IS NULL;
		
	set @errnum = 0;
	set @errstr = 'Successfully updated registration data for token: ' + @randtoken;
END TRY
BEGIN CATCH
 	set @errnum = ERROR_NUMBER()
 	set @errstr = ERROR_MESSAGE()
END CATCH
go

-- declare @localnum int, @localstr varchar(256);
-- exec createPacificCollegeRegistration 'randomtokenhere', 'First', 'Last', 'first.last@email.com', '512-555-1212', 'chicago', @localnum OUTPUT, @localstr OUTPUT
-- exec getPacificCollegeRegistration 'randomtokenhere', @localnum OUTPUT, @localstr OUTPUT
