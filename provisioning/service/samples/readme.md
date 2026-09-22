## Provisioning Device Sample - Microsoft Azure IoT SDK for .NET

### Service provisioning samples

- [Group certificate verification][group-cert-sample] sample
- [Bulk operation][bulk-op-sample] sample
- [Individual enrollment][enrollment-sample] sample
- [Enrollment group][enrollment-group-sample] sample
- [Clean up enrollments][clean-up-enrollments-sample] sample

### Selecting a service API version

By default, `ProvisioningServiceClient` uses service API version `2019-03-31`, which preserves the
existing wire behavior. To opt in to a newer API version, pass a `ProvisioningServiceClientOptions`
with an explicit `Version`:

```csharp
var options = new ProvisioningServiceClientOptions
{
    // 2026-11-02-preview is an explicit opt-in version. It enables preview-only fields such as
    // IndividualEnrollment/EnrollmentGroup.DeviceTypeRefs and the read-only
    // DeviceRegistrationState.ConnectionProfile response field.
    Version = ServiceVersion.V2026_11_02_Preview,
};

using var client = ProvisioningServiceClient.CreateFromConnectionString(connectionString, options);
```

`ServiceVersion.V2026_11_01` is also an explicit opt-in version that supports the 2026-11-01 model
fields. The Individual enrollment and Enrollment group samples accept a `--ApiVersion` switch
(`2019-03-31`, `2026-11-01`, or `2026-11-02-preview`) to demonstrate version selection and the
associated model fields.

[group-cert-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/how%20to%20guides/GroupCertificateVerificationSample
[bulk-op-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/how%20to%20guides/BulkOperationSample
[enrollment-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/EnrollmentSample
[enrollment-group-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/EnrollmentGroupSample
[clean-up-enrollments-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/CleanupEnrollmentsSample