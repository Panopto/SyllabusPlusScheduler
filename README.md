# Panopto-ScheduleRecordingService
The Panopto Syllabus Plus Scheduler service queries a table of scheduled events (meetings, classes, etc.) in a Syllabus Plus database and then schedules recordings in Panopto using the Panopto SOAP API.

## How does it work?
* The service periodically checks the specified database's "Schedules" table to determine if any new or updated entries have been added. The sync interval is a configurable setting.
* When the service finds a new scheduled event row that needs to be created in Panopto, it calls the Panopto SOAP API to create the scheduled recording with the specified session name, folder, remote recorder, start time, duration and whether the session is a webcast or just a regular recording.
  * Optionally, you can also specify a secondary remote recorder (for rooms equipped with two remote recorders) and the presenter's username. Note that if you omit the presenter's username, the scheduled recording's owner in Panopto will be the remote recorder service.
* When the service finds an existing scheduled event row that needs to be updated, it calls the API to update the start time, duration, or session name. Note that currently any changes to change the presenter's username, change the folder, or any other information will not be sync'ed after the recording has been scheduled. For now, please make these changes manually through the Panopto web application.
* When the service finds an existing scheduled event row that has an updated remote recorder ID, the original session will be deleted and a new session will be scheduled for the updated remote recorder.
* When the service finds an existing scheduled event row that needs to be cancelled (based on if the cancel schedule flag is set), the service will cancel the scheduled recording in Panopto.
* The service uses the lastUpdate column (oldest date first) to determine in which order items are synced. This can be overridden with optional config change OrderSyncById. Setting to true will use the ID (PK auto increment) to determine order of sync, starting with the lowest value first.  

## How do I set this up?
1. Run DB Script\SyllabusPlusDBScript.sql to create the Schedules table in your database.
2. Populate the Schedules table with data from your Syllabus Plus database.
    * **Important:** Right now this service does not extract data from the Syllabus Plus tables and insert it into the Schedules table. Currently you will need to have separate code (either in SQL or some other format) to pull data from Syllabus Plus and update the Schedules table on a recurring basis.
3. Install the service using the installation instructions below. Then the service will start sync'ing data from the Schedules table into Panopto.

## Installation instructions
1. Download ScheduleRecordingServiceInstaller.msi
2. Open a command prompt with administrator privileges.
3. Run the installer with the following command:
```
ScheduleRecordingServiceInstaller.msi PANOPTOSERVERNAME=<Panopto Servername> USERNAME=<Panopto Username> PASSWORD=<Panopto Password> SYNCINTERVAL=<Sync Interval in minutes> DEFAULTFOLDER=<Folder ID to use if the folder specified in the DB row is invalid> DBSERVER=<Location of database server> DBNAME=<Name of database>
```
Note that the account provided for the username and password must have access to the remote recorders. We recommend that you create a separate Panopto administrator account specifically for this service.

To uninstall the service, go to Programs and Features and uninstall Panopto Syllabus Plus Scheduler Service.

## Optional additional configuration
You can change configuration after you have installed the service by directly editing the SyllabusPlusService.exe.config file located in \Programs Files (x86)\Panopto\Syllabus Plus Scheduler Service. After making changes to SyllabusPlusService.exe.config, please restart the service.
```
...
<appSettings>
    <add key="PanoptoSite" value="<Panopto Servername>"/>
    <add key="PanoptoUserName" value="<Panopto Username>"/>
    <add key="PanoptoPassword" value="<Panopto Password>"/>
    <add key="SyncInterval" value="<Sync Interval in minutes>"/><!--In minutes-->
    <add key="PanoptoDefaultFolder" value="<Panopto Folder ID>"/>
    <add key="OrderSyncById" value="false" />
</appSettings>
<connectionStrings>
    <add name="SyllabusPlusDBContext" connectionString="Data Source=<Location of database server>;Initial Catalog=<Name of database>;Integrated Security=True" providerName="System.Data.SqlClient"/>
</connectionStrings>
...
```

## Schedules table schema
| Column | Type | Description|
|---|---|---|
|ID|int|Primary key. Generated by the database.|
|sessionName|nvarchar(255)|The name of the scheduled recording. Technically optional since a scheduled recording will generate a default session name if none is provided.|
|FolderID|uniqueidentifier|GUID of the Panopto folder for the scheduled recording|
|primaryRemoteRecorderID|uniqueidentifier|GUID of the remote recorder for the scheduled recording|
|secondaryRemoteRecorderID|uniqueidentifier|Optional. GUID of a second remote recorder. Only applicable to a room that is equipped with two remote recorders|
|startTime|datetime|Start time for the scheduled recording. This must be in UTC.|
|duration|int|Length of the scheduled recording in minutes|
|presenterUsername|nvarchar(255)|Optional. The presenter's Panopto username. The service will make this user the owner of the scheduled recording. If omitted, the remote recorder service will become the owner of the scheduled recording.|
|cancelSchedule|bit|Nullable bit used to cancel a scheduled recording. If null, no action. If 0, cancel is requested. If 1, cancel succeeded.|
|webcast|bit|If 1, webcast. If 0, regular recording.|
|scheduledSessionID|uniqueidentifier|Null until the service populates the column with the GUID for the scheduled recording during a sync.|
|lastUpdate|datetime|The last time the row was updated in the database. This must be in UTC.|
|lastPanoptoSync|datetime|The last time the service sync'ed this row. This must be in UTC.|
|panoptoSyncSuccess|bit|Null until the service attempts a sync. If the sync fails for the row, this bit is set to 0. If the sync succeeds, this bit is set to 1.|
|numberOfAttempts|int|Counter for the number of times the service attempted to sync this row but failed. Once the max number of attempts is reached, the service will no longer attempt to sync this row. Currently the max number of attempts is set to 3.|
|errorResponse|nvarchar(max)|If the service fails to schedule the row (say there is a web request failure or a conflicting recording), the error message is stored in this column.|


## Troubleshooting
Here are a few helpful troubleshooting tips:
* When first installing this service, it is useful to manually insert some sample data into the Schedules table. That way you can verify that data is being picked up by the service from the database and sync'ing to Panopto correctly.
* A few quick things to watch out for with your sample data:
    * Make sure you have a valid remote recorder ID and folder ID.
    * Make sure the start times are specified in UTC.
    * Make sure that the credentials used by the service have Creator access to the appropriate folders and remote recorders. 
    * You may also want to use a shorter sync interval (2 mins or 5 mins) when testing the initial service configuration.
* Here is a SQL snippet that you can use to insert some sample data. Be sure to replace the variables (@folderID, @rrID, @date1/2/3) with actual values.
```
insert into Schedules (sessionName,folderID,primaryRemoteRecorderID,startTime,duration,webcast,lastUpdate,numberOfAttempts) values ('Test session 1',@folderID,@rrID,@date1,60,0,GETUTCDATE(),0);

insert into Schedules (sessionName,folderID,primaryRemoteRecorderID,startTime,duration,webcast,lastUpdate,numberOfAttempts) values ('Test session 2',@folderID,@rrID,@date2,60,0,GETUTCDATE(),0);

insert into Schedules (sessionName,folderID,primaryRemoteRecorderID,startTime,duration,webcast,lastUpdate,numberOfAttempts) values ('Test session 3',@folderID,@rrID,@date3,60,0,GETUTCDATE(),0);
```
* If any errors occur or scheduling conflicts are encountered while scheduling recordings, the Schedules table includes an errorMessage column containing XML with the corresponding error message.
* In addition to the error response being stored in the Schedules table, the service also logs sync data to the event log. This is useful when troubleshooting the service. You can pull this event log up by opening the Windows Event Viewer app and navigating to Windows Logs > Application.

## License
Copyright 2020 Panopto

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
