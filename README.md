Panopto-ScheduleRecordingService
=====================

Panopto Schedule Recording Service

This service uses Panopto SOAP API and C# to schedule recordings to server.

Most options explained below can be found and modified in the ScheduleRecordingService.exe.config file

This service creates remote recorder scheduling using credentials stored in config file for its installer

This service will periodically (based on the interval set in the config file) check the DB's "Schedules" table to determine if any new or updated entries have been added. 
Note: Once a recording is scheduled in Panopto, only changes to the start and duration of the session will take place. 
Any changes to renaming the session, updating the presenter's username, or any other information will need to be manually changed through the Panopto web UI. 

If the lastUpdate timestamp exceeds lastPanoptoSync timestamp or PanoptoSyncSuccess is null, the service will attempt to schedule a remote recording based on the data provided in that row.

If any errors occurs or conflicts in scheduled recordings, the schedule table includes an errorMessage column containing an XML with the appropriate error message.

Using the service: 


	Two Options
	Option 1:
		Open command prompt with admin priviledges
		
		Navigate to ScheduleRecordingServiceInstaller/Bin/Release and run the following command:
		
		ScheduleRecordingServiceInstaller.msi PANOPTOSERVERNAME=<Panopto Servername> USERNAME=<Panopto Username> PASSWORD=<Panopto Password> SYNCINTERVAL=<Sync Interval in minutes> DBSERVER=<Location of DB server> DBNAME=<Name of DB>
		
		The service should run and install automatically to ProgramsFolder\Panopto
		
		To uninstall the service, go to Programs and Features to uninstall the service
	Option 2:
		Before starting the service, be sure to modify the *.config as necessary:
		- DB Connection String
		- Panopto Username
		- Panopto Password
		- Panopto Servername
		- Sync Interval in minutes
		
		Use the Developer Command Prompt in Visual Studio Tools to install the service's .exe file; 
			navigate to the folder containing the .exe file for the service and use the command: installutil.exe ScheduleRecordingService.exe

		Run services.msc to view the full list of service. Find "Panopto Schedule Recording Service" and start the service if not starated automatically

		The service can be uninstalled using the command: installutil.exe /u ScheduleRecordingService.exe
