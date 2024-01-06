# PortalGuard


CHANGES:

1. updated file PacificCollegeTA.sql: (database schema changes)
   - add new colunn 'program' to table PacificCollegeRegData
   - NOTE: the existing stored procedure getPacificCollegeRegistration performs 'SELECT * FROM PacificCollegeRegData', 
     so the newly added field 'program' is already included in the result set

2. updated file web.config:
   - name new email subject and body values for 'moodle', 'blackboard'
   - please customize the subjects provided here!
   
3. added new email templates: email-template-moodle.html, email-template-blackboard.html
   - please customize these templates!

4. updated file Pacific-SelfReg-TA.ash.cs:
   - add new definition FRMFLD_PROGRAM
   
5. updated file register.ashx.cs:
   - NOTE: the function ProcessRequest() describes the overall flow for the latter half of the registration process
   - NOTE: the function PortalGuard.Utilities.getRegistration() reads fields from SQL
   - add new function SendSecondaryEmail(), utilizing new web.config values and new email templates
   - call SendSecondaryEmail() from PostCreateUserAccount()