// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common
{
    // An AsyncResult that completes as soon as it is instantiated.
    [Serializable]
    internal class CompletedAsyncResult : AsyncResult
    {
        public CompletedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true);
        }

        public CompletedAsyncResult(Exception exception, AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true, exception);
        }

        [Fx.Tag.GuaranteeNonBlocking]
        public static void End(IAsyncResult result)
        {
            Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult was not completed!");
            AsyncResult.End<CompletedAsyncResult>(result);
        }
    }

    [Serializable]
    internal class CompletedAsyncResult<T> : AsyncResult
    {
        private readonly T _data;

        public CompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _data = data;
            Complete(true);
        }

        [Fx.Tag.GuaranteeNonBlocking]
        public static T End(IAsyncResult result)
        {
            Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult<T> was not completed!");
            CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
            return completedResult._data;
        }
    }

    [Serializable]
    internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
    {
        private readonly TResult _resultData;
        private readonly TParameter _parameter;

        public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _resultData = resultData;
            _parameter = parameter;
            Complete(true);
        }

        [Fx.Tag.GuaranteeNonBlocking]
        public static TResult End(IAsyncResult result, out TParameter parameter)
        {
            Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult<T> was not completed!");
            CompletedAsyncResult<TResult, TParameter> completedResult = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
            parameter = completedResult._parameter;
            return completedResult._resultData;
        }
    }
}
