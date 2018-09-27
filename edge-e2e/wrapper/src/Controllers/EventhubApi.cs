/*
 * IoT SDK Device & Client REST API
 *
 * REST API definition for End-to-end testing of the Azure IoT SDKs.  All SDK APIs that are tested by our E2E tests need to be defined in this file.  This file takes some liberties with the API definitions.  In particular, response schemas are undefined, and error responses are also undefined.
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Attributes;
using IO.Swagger.Models;

namespace IO.Swagger.Controllers
{ 
    /// <summary>
    /// 
    /// </summary>
    public class EventhubApiController : Controller
    { 
        /// <summary>
        /// Connect to eventhub
        /// </summary>
        /// <remarks>Connect to the Azure eventhub service.</remarks>
        /// <param name="connectionString">Service connection string</param>
        /// <response code="200">OK</response>
        [HttpPut]
        [Route("/eventhub/connect")]
        [ValidateModelState]
        [SwaggerOperation("EventhubConnectPut")]
        [SwaggerResponse(statusCode: 200, type: typeof(ConnectResponse), description: "OK")]
        public virtual IActionResult EventhubConnectPut([FromQuery][Required()]string connectionString)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(ConnectResponse));

            string exampleJson = null;
            exampleJson = "{\n  \"connectionId\" : \"connectionId\"\n}";
            
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<ConnectResponse>(exampleJson)
            : default(ConnectResponse);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// wait for telemetry sent from a specific device
        /// </summary>
        
        /// <param name="connectionId">Id for the connection</param>
        /// <param name="deviceId"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/eventhub/{connectionId}/deviceTelemetry/{deviceId}")]
        [ValidateModelState]
        [SwaggerOperation("EventhubConnectionIdDeviceTelemetryDeviceIdGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "OK")]
        public virtual IActionResult EventhubConnectionIdDeviceTelemetryDeviceIdGet([FromRoute][Required]string connectionId, [FromRoute][Required]string deviceId)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(string));

            string exampleJson = null;
            exampleJson = "";
            
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<string>(exampleJson)
            : default(string);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Disconnect from the eventhub
        /// </summary>
        /// <remarks>Disconnects from the Azure eventhub service</remarks>
        /// <param name="connectionId">Id for the connection</param>
        /// <response code="200">OK</response>
        [HttpPut]
        [Route("/eventhub/{connectionId}/disconnect/")]
        [ValidateModelState]
        [SwaggerOperation("EventhubConnectionIdDisconnectPut")]
        public virtual IActionResult EventhubConnectionIdDisconnectPut([FromRoute][Required]string connectionId)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);


            throw new NotImplementedException();
        }

        /// <summary>
        /// Enable telemetry
        /// </summary>
        
        /// <param name="connectionId">Id for the connection</param>
        /// <response code="200">OK</response>
        [HttpPut]
        [Route("/eventhub/{connectionId}/enableTelemetry")]
        [ValidateModelState]
        [SwaggerOperation("EventhubConnectionIdEnableTelemetryPut")]
        public virtual IActionResult EventhubConnectionIdEnableTelemetryPut([FromRoute][Required]string connectionId)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);


            throw new NotImplementedException();
        }
    }
}
