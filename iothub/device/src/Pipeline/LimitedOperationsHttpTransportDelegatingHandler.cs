// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class LimitedOperationsHttpTransportDelegatingHandler : DefaultDelegatingHandler
    {
        // File upload operation
        private readonly HttpTransportHandler _fileUploadHttpTransportHandler;

        public LimitedOperationsHttpTransportDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            _fileUploadHttpTransportHandler = new HttpTransportHandler(context);
        }

        public override Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken)
        {
            return _fileUploadHttpTransportHandler.GetFileUploadSasUriAsync(request, cancellationToken);
        }

        public override Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken)
        {
            return _fileUploadHttpTransportHandler.CompleteFileUploadAsync(notification, cancellationToken);
        }
    }
}
