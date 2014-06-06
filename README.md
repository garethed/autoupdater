autoupdater
===========

Runs a .net exe, and monitors a file location for updates, seamlessly relaunching whenever an update is available.

Usage
=====
* Update the various constants at the top of Program.cs
** PERSISTENCE_KEY is a label used to transfer data between old and new versions when upgrading - you can leave this unchanged.
** EXE_NAME is the name given to the downloaded file. It must have a different name from the updater exe, and you can achieve this by giving it a .dll extension (but it should be an exe with a main function)
** SERVER_FILE_PATH is the path to the file location where you'll deposit updated versions. This should point at the specific file which has the Main function. Any other dlls in the same directory will also be downloaded
** CHECK_INTERVAL_MINS is the update check frequency
* Update the icon associated with the AutoUpdate project to be your app's icon
* Update the assemblyinfo metadata also
* Build the autoUpdate project
* Rename AutoUpdate.exe to the name you want your app exe to have
* Distribute this renamed file only. When users run it for the first time it will fetch the actual app from the location you configured and run this.

Passing data between app versions
=================================
* Your app can pass data from the old version to the new one, in order to e.g. preserve the runtime state of the app when it's upgraded. 
** In an onclose event (in your actual app, not the autoupdater code discussed above) if you have data to send to the new instance, call AppDomain.CurrentDomain.SetData("AUTOUPDATE_PERSISTENCE_DATA", "data I want to pass, as a string")
** In your initialization code, check for this data by testing AppDomain.CurrentDomain.GetData("AUTOUPDATE_PERSISTENCE_DATA")  
* The testApp project has an example of this 

Known issues
============

* As it stands, makes no effort to verify the security of the file being downloaded - should use code-signing or similar in future
* There is no automated mechanism for updating this application. quis autoupdatiet ipsos autoupdateres?
* The downloaded application is stored in the users temp folder - if this download is corrupted (or interrupted part way) then the auto-updater currently won't be able to run the app. It should be a small fix to catch this case and delete the cached data before retrying
* It can be slow to download the first time - should really have some UI to tell the user it's doing something, and also to show any errors 
