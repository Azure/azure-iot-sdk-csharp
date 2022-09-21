#r "Newtonsoft.Json"
using System.Net;
using System.Text;
using Newtonsoft.Json;

using Microsoft.Azure.Devices.Provisioning.Service;

//This function will return the iot hub hostname to provision to based on which of the list of hub names has the longest host name
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    // Just some diagnostic logging
    log.Info("C# HTTP trigger function processed a request.");
    log.Info("Request.Content:...");    
    log.Info(req.Content.ReadAsStringAsync().Result);

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
        log.Info("Registration ID : NULL");
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
            log.Info("linkedHubs : NULL");
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
                log.Info(message);
                fail = true;
            }
        }
    }

    return (fail)
        ? req.CreateResponse(HttpStatusCode.BadRequest)
        : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(obj, Formatting.Indented),
                    Encoding.UTF8,
                    "application/json")
            };
}    

public class ResponseObj
{
    public string iotHubHostName {get; set;}
    public TwinState initialTwin {get; set;}
    public dynamic payload {get; set;}
}