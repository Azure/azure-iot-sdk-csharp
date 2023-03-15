// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Copy of a subset of the helper functions defined in the Azure.Core class by the same name:
    /// https://github.com/Azure/autorest.csharp/blob/main/src/assets/Generator.Shared/PageableHelpers.cs
    /// </summary>
    internal static class PageableHelpers
    {
        public static AsyncPageable<T> CreateAsyncEnumerable<T>(Func<int?, Task<Page<T>>> firstPageFunc, Func<string, int?, Task<Page<T>>> nextPageFunc, int? pageSize = default) where T : notnull
        {
            AsyncPageFunc<T> first = (continuationToken, pageSizeHint) => firstPageFunc(pageSizeHint);
            AsyncPageFunc<T> next = nextPageFunc != null ? new AsyncPageFunc<T>(nextPageFunc) : null;
            return new FuncAsyncPageable<T>(first, next, pageSize);
        }

        internal delegate Task<Page<T>> AsyncPageFunc<T>(string continuationToken = default, int? pageSizeHint = default);
        internal delegate Page<T> PageFunc<T>(string continuationToken = default, int? pageSizeHint = default);

        internal class FuncAsyncPageable<T> : AsyncPageable<T> where T : notnull
        {
            private readonly AsyncPageFunc<T> _firstPageFunc;
            private readonly AsyncPageFunc<T> _nextPageFunc;
            private readonly int? _defaultPageSize;

            public FuncAsyncPageable(AsyncPageFunc<T> firstPageFunc, AsyncPageFunc<T> nextPageFunc, int? defaultPageSize = default)
            {
                _firstPageFunc = firstPageFunc;
                _nextPageFunc = nextPageFunc;
                _defaultPageSize = defaultPageSize;
            }

            public override async IAsyncEnumerable<Page<T>> AsPages(string continuationToken = default, int? pageSizeHint = default)
            {
                AsyncPageFunc<T> pageFunc = string.IsNullOrEmpty(continuationToken) ? _firstPageFunc : _nextPageFunc;

                if (pageFunc == null)
                {
                    yield break;
                }

                int? pageSize = pageSizeHint ?? _defaultPageSize;
                do
                {
                    Page<T> pageResponse = await pageFunc(continuationToken, pageSize).ConfigureAwait(false);
                    yield return pageResponse;
                    continuationToken = pageResponse.ContinuationToken;
                    pageFunc = _nextPageFunc;
                } while (!string.IsNullOrEmpty(continuationToken) && pageFunc != null);
            }
        }
    }
}
