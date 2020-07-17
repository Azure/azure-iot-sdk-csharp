
echo on
//set environment variables. Fill in the fields below, then run this at the command level before running the NET Core app.
//Then run the app from the command level. Like this:
CMD> testenvvars_SET
CMD> dotnet urn

//Doing this instead of hardcoding these variables ensures that you don't accidentally save them to github.
//
SET IOT_DEVICE_ID=Contoso-Test-Device
SET IOT_HUB_URI="IOT-HUB-NAME-GOES-HERE.azure-devices-net";
SET IOT_DEVICE_KEY="IOT-DEVICE-KEY-GOES-HERE"
//
//to see local environment variables, type set and hit return, and it will show them all