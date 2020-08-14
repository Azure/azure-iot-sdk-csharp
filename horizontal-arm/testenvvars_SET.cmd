
echo on
//set environment variables. Fill in the fields below, then run each line in the command window one at a time
//then run the .NET Core app from the command window. You should be in the same folder as the arm-read-write.csproj file.
CMD> dotnet run

//Doing this instead of hardcoding these variables ensures that you don't accidentally save them to github.
//
SET IOT_DEVICE_ID=Contoso-Test-Device
SET IOT_HUB_URI="IOT-HUB-NAME-GOES-HERE.azure-devices-net";
SET IOT_DEVICE_KEY="IoT-device-key-goes-here"
//
//to see local environment variables, type set and hit return, and it will show them all