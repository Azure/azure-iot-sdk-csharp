// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    /// <summary>
    /// Creates a storage container for your use for the lifetime of this class
    /// </summary>
    public class StorageContainer : IDisposable
    {
        private bool _disposed;

        public string ContainerName { get; }
        public Uri Uri { get; private set; }
        public Uri SasUri { get; private set; }
        public CloudBlobContainer CloudBlobContainer { get; private set; }

        private StorageContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                containerName = Guid.NewGuid().ToString();
            }

            ContainerName = containerName;
        }

        public static async Task<StorageContainer> GetInstanceAsync(string containerName = null)
        {
            var sc = new StorageContainer(containerName);
            await sc.InitializeAsync().ConfigureAwait(false);
            return sc;
        }

        public void UpdateSasUri(DateTimeOffset? expiresOn = null)
        {
            var constraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = expiresOn ?? DateTimeOffset.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read
                    | SharedAccessBlobPermissions.Write
                    | SharedAccessBlobPermissions.Create
                    | SharedAccessBlobPermissions.List
                    | SharedAccessBlobPermissions.Add
                    | SharedAccessBlobPermissions.Delete,
                SharedAccessStartTime = DateTimeOffset.UtcNow,
            };

            string sasContainerToken = CloudBlobContainer.GetSharedAccessSignature(constraints);
            SasUri = new Uri($"{Uri}{sasContainerToken}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static string BuildContainerName(string prefix, int suffixDigits = 4)
        {
            const int maxLen = 63;
            if (suffixDigits > maxLen)
            {
                throw new ArgumentOutOfRangeException(nameof(suffixDigits), "Suffix digits cannot exceed max length");
            }

            var sb = new StringBuilder(maxLen);

            // Storage container name rules:
            // 3 to 63 Characters;
            // Starts With Letter or Number;
            // Contains Letters, Numbers, and Dash(-);
            // Every Dash(-) Must Be Immediately Preceded and Followed by a Letter or Number
            bool wasLastCharDash = false;
            int charCount = 0;
            foreach (char character in prefix)
            {
                if (charCount >= maxLen - suffixDigits)
                {
                    // save room for 4 digit random suffix
                    break;
                }

                if (!char.IsLetterOrDigit(character)
                    && character != '-')
                {
                    if (!wasLastCharDash)
                    {
                        sb.Append('-');
                        ++charCount;
                        wasLastCharDash = true;
                    }
                }
                else
                {
                    sb.Append(character);
                    ++charCount;
                    wasLastCharDash = false;
                }
            }

            // Add random suffix
            sb.Append(GetRandomSuffix(suffixDigits));

#pragma warning disable CA1308 // Normalize strings to uppercase
            return sb.ToString().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        public static string GetRandomSuffix(int digits)
        {
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, digits);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing && CloudBlobContainer != null)
            {
                CloudBlobContainer.Delete();
                CloudBlobContainer = null;
            }

            _disposed = true;
        }

        private async Task InitializeAsync()
        {
            var storageAccount = CloudStorageAccount.Parse(TestConfiguration.Storage.ConnectionString);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
            await CloudBlobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);

            Uri = CloudBlobContainer.Uri;
            UpdateSasUri();
        }
    }
}
