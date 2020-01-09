Create solution
Run it and try to log in or register so that the EF error page appears - click migrate and refresh. This should create the user database.
Add classes as required in Model folder
Add controllers as required using EF API CRUD. Adjust as required.
Add migrations, update database
Add Angular components as required. Service for HTTP requests 


Adding fields to User (UserIdentity. ApplicationUser extends UserIdentity)
It is automatic in that you just add properties to the blank ApplicationUser class. It is already linked to use this class everywhere.

Add a migrations as required. E.G. "Add-migration Initial" then "update-database"
To revert - update-database CreateIdentitySchema (1st migration when Authorisation project created. Not worth going back further, i.e. update-database 0)
Then "Remove-Migration" to go back to previous migration. If you've done several just use "Remove-Migration" again.

All the views and code is hidden initially. To unhide it Add a scaffolding item and choose Identity then tick whatever you need to unhide.
You can then edit views and c# files as required.


Adding ngx-bootstrap datepicker:
  ng add ngx-bootstrap  --component datepicker

Needed to delete the line in angular.json where the styles are: node_modules/bootstrap/dist/css/bootstrap.min.css". ngx-bootstrap adds it in but it was already
put there by the ASP.NET Core Angular template.

--------------------------------------------------------------------------------------------------------
MySql version

To recreate from blank database:
Run app and register a user. Then add records so that this user is an administrator (and manager). You can then do whatever you want.
Tables that you need to populate would be:

			 Id	  Name	       Normalized Name
AspNetRoles. 1    employee     EMPLOYEE				
			 2    admin		   ADMIN
			 3    manager      MANAGER

				  Id			UserId							ClaimType										   ClaimValue
AspNetUserClaims. 1   {userId of user just created}, http://schemas.microsoft.com/ws/2008/06/identity/claims/role, employee
			      2   {userId of user just created}, http://schemas.microsoft.com/ws/2008/06/identity/claims/role, admin
				  3   {userId of user just created}, http://schemas.microsoft.com/ws/2008/06/identity/claims/role, manager

AspNetUserRoles.  UserId = {userId of user just created}, RoleId = 1
				  UserId = {userId of user just created}, RoleId = 2
				  UserId = {userId of user just created}, RoleId = 3


Deploying to Azure
------------------

Used "MySQL in App". Not suitable for production.
Export the MySQL database from phpMyAdmin.
Publish solution in Visual Studio (no database or migrations). Web page will show error (because of db).
Go into "MySQL in App" in the app in Azure and click on "Manage" to go into Azure phpMyAdmin.
Since MySql is only started with the main site, do make sure that the main site is running (simplest 
way is to turn on AlwaysOn) before using phpMyAdmin. This not possible on the Free option. 
Import the database from the file just exported.
Find the connection string from:
	Go to Advanced Tools click Go (goes to new tab)
	In ‘Kudu’, click ‘Debug console’ (you can use console to change username and password as well),
	browse to ‘D:\home\data\mysql\’ and locate file ‘MYSQLCONNSTR_localdb.ini’. In this file, you 
	have connection string for MySQL db containing credentials.
It isn't quite right but it is always:
	server=localhost;port=49480;database=localdb;uid=azure;password=6#vWHD_$;
	The port is different for each application. Find the port in Azure phpMyAdmin
	by looking at the current server in the top left corner dropdown.
	Docs say "the port number may vary for each application life cycle depending on its availability
	at startup time". Think this is ok for demos but will change if I change the app plan or if Azure 
	upgrade things (not really sure). 
	The port info is also available as an env variable WEBSITE_MYSQL_PORT to the site. 
	I think you should really create connect string in the C# rather than use the Azure connection
	string. I have added code to startup.cs. This is better because port number can change. 
	(As long as format of "MYSQLCONNSTR_localdb" doesn't change.....).
	As a fall back if the above causes a problem you can put the above connection string in the 
	Azure Configuration page (with correct password) as "DefaultConnection". (Change type to Custom
	so when you connect to PHPMyAdmin it will force it to use MYSQLCONNSTR_localdb and connect to
	the MySQL in App server).
Refresh page. It should now work.

See https://github.com/projectkudu/kudu/wiki/MySQL-in-app for more details