#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

// This function will return the iot hub hostname to provision to based on which of the list of hub names has the longest host name.
public static async Task<IActionResult> Run(HttpRequestMessage req, ILogger log)
{
    // Just some diagnostic logging
    log.LogInformation("Request.Content:...");
    log.LogInformation(await req.Content.ReadAsStringAsync().ConfigureAwait(true));

    try
    {
        // Get request body
        dynamic data = await req.Content.ReadAsAsync<object>().ConfigureAwait(true);

        // Get registration ID of the device
        string regId = data?.deviceRuntimeContext?.registrationId;

        string message = "Uncaught error";
        bool fail = false;
        var obj = new ResponseObj
        {
            initialTwin = new TwinState(new TwinCollection(), new TwinCollection()),
        };

        if (regId == null)
        {
            message = "Registration ID not provided for the device.";
            log.LogInformation("Registration ID : NULL");
            fail = true;
        }
        else
        {
            string[] hubs = data?.linkedHubs.ToObject<string[]>();

            obj.payload = data?.deviceRuntimeContext?.payload;

            // Must have hubs selected on the enrollment
            if (hubs == null)
            {
                message = "No hub group defined for the enrollment.";
                log.LogInformation("linkedHubs : NULL");
                fail = true;
            }
            else
            {
                //find hub name with longest hostname
                obj.iotHubHostName = "";
                foreach (string hubString in hubs)
                {
                    if (hubString.Length > obj.iotHubHostName.Length)
                    {
                        obj.iotHubHostName = hubString;
                    }
                }

                if (String.IsNullOrEmpty(obj.iotHubHostName))
                {
                    message = "No hub names were provided in iothubs field for this enrollment.";
                    log.LogInformation(message);
                    fail = true;
                }
            }
        }

        string responsePayload = JsonConvert.SerializeObject(obj, Formatting.Indented);
        log.LogInformation($"Response payload is: [{responsePayload}].");

        return (fail)
            ? (ActionResult)new BadRequestObjectResult(message)
            : (ActionResult)new OkObjectResult(responsePayload);
    }
    catch (Exception ex)
    {
        log.LogError(ex, $"Failed to process custom alloc request.");
        return (ActionResult)new BadRequestObjectResult(ex.Message);
    }
}

public class ResponseObj
{
    public string iotHubHostName { get; set; }
    public TwinState initialTwin { get; set; }
    public dynamic payload { get; set; }
}