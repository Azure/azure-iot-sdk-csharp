// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Microsoft.Azure.Devices.E2ETests.Config
{
    /// <summary>
    /// Creates a storage container for your use for the lifetime of this class
    /// </summary>
    public class StorageContainer : IDisposable
    {
        private bool _disposed = false;

        public string ContainerName { get; }
        public Uri SasUri { get; private set; }
        public BlobContainerClient BlobContainerClient { get; private set; }

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
            var constraints = new AccountSasBuilder
            {
                Services = AccountSasServices.Blobs,
                ResourceTypes = AccountSasResourceTypes.Service | AccountSasResourceTypes.Object,
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = expiresOn ?? DateTimeOffset.UtcNow.AddHours(1),
                Protocol = SasProtocol.Https,
            };
            constraints.SetPermissions(AccountSasPermissions.All);

            var credential = new StorageSharedKeyCredential(Configuration.Storage.Name, Configuration.Storage.Key);

            var sasUri = new UriBuilder(BlobContainerClient.Uri)
            {
                Query = constraints.ToSasQueryParameters(credential).ToString(),
            };
            SasUri = sasUri.Uri;
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

            if (disposing && BlobContainerClient != null)
            {
                BlobContainerClient.Delete();
                BlobContainerClient = null;
            }

            _disposed = true;
        }

        private async Task InitializeAsync()
        {
            BlobContainerClient = new BlobContainerClient(Configuration.Storage.ConnectionString, ContainerName);
            await BlobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            UpdateSasUri();
        }
    }
}
