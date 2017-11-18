// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal enum ErrorCode
    {
        InvalidErrorCode = 0,        

        //BadRequest - 400
        InvalidProtocolVersion = 400001,
        DeviceInvalidResultCount = 400002,
        InvalidOperation = 400003,
        ArgumentInvalid = 400004,
        ArgumentNull = 400005,
        IotHubFormatError = 400006,
        DeviceStorageEntitySerializationError = 400007,
        BlobContainerValidationError = 400008,
        ImportWarningExistsError = 400009,
        InvalidSchemaVersion = 400010,
        DeviceDefinedMultipleTimes = 400011,
        DeserializationError = 400012,
        BulkRegistryOperationFailure = 400013,

        //Unauthorized - 401
        IotHubNotFound = 401001,
        IotHubUnauthorizedAccess = 401002,
        IotHubUnauthorized = 401003,

        // NotFound - 404
        DeviceNotFound = 404001,
        JobNotFound = 404002,
        PartitionNotFound = 404003,

        //PreconditionFailed - 412
        PreconditionFailed = 412001,
        DeviceMessageLockLost = 412002,

        //Throttling Exception - 429
        ThrottlingException = 429001,
        ThrottleBacklogLimitExceeded = 429002,
        InvalidThrottleParameter = 429003,

        //InternalServerError - 500
        ServerError = 500001,
        JobCancelled = 500002,
    }
}
