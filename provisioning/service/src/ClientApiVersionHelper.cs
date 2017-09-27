namespace Microsoft.Azure.Devices.Provisioning.Service
{
    class ClientApiVersionHelper
    {
        const string ApiVersionDpsPreview = "2017-08-31-preview";

        public const string ApiVersionQueryPrefix = "api-version=";
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionDpsPreview;

    }
}
