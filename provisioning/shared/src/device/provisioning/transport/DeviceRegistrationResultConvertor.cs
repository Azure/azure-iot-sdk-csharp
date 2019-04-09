using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class DeviceRegistrationResultConvertor
    {
        internal static DeviceRegistrationResult ConvertToProvisioningRegistrationResult(RegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            var substatus = ProvisioningRegistrationSubstatusType.InitialAssignment;
            Enum.TryParse(result.Substatus, true, out substatus);

            byte[] returnData = null;
            if (result.ReturnData != null && result.ReturnData.HasValues)
            {
                returnData = System.Text.Encoding.UTF8.GetBytes(result.ReturnData.ToString(Formatting.None));
            }
            return new DeviceRegistrationResult(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                substatus,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag,
                returnData);
        }
    }
}