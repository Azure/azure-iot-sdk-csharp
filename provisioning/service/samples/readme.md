## Provisioning Device Sample - Microsoft Azure IoT SDK for .NET

### Service provisioning samples

- [Group certificate verification][group-cert-sample] sample
- [Bulk operation][bulk-op-sample] sample
- [Individual enrollment][enrollment-sample] sample
- [Enrollment group][enrollment-group-sample] sample
- [Clean up enrollments][clean-up-enrollments-sample] sample

### Service API version

This preview package targets the `2026-11-02-preview` service API version and issues it on every
request; there is no API-version selector. The samples therefore exercise the preview model fields
directly:

- `IndividualEnrollment` and `EnrollmentGroup` expose `NamespaceName`, `CertificateAuthorityName`,
  `CertificatePolicyName`, and `DeviceTypeRefs` (at most one item). Populate the sample's optional
  fields to send them.
- `DeviceRegistrationState.ConnectionProfile` is a read-only, response-only field. It is extensible
  (known values include `classic` and `mqttV5`, unknown future values are tolerated), and a
  missing/null value semantically resolves to `classic`.

The new-field samples require the `2026-11-02-preview` preview package; build each sample project
against the published preview package version before running.

```csharp
using var client = ProvisioningServiceClient.CreateFromConnectionString(connectionString);
```

[group-cert-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/how%20to%20guides/GroupCertificateVerificationSample
[bulk-op-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/how%20to%20guides/BulkOperationSample
[enrollment-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/EnrollmentSample
[enrollment-group-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/EnrollmentGroupSample
[clean-up-enrollments-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples/getting%20started/CleanupEnrollmentsSample