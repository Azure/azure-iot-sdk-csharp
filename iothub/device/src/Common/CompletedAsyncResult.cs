// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    [Serializable]
    internal class CompletedAsyncResultT<T> : AsyncResult
    {
        private readonly T _data;

        public CompletedAsyncResultT(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _data = data;
            Complete(true);
        }

        [Fx.Tag.GuaranteeNonBlocking]
        public static T End(IAsyncResult result)
        {
            Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult<T> was not completed!");
            CompletedAsyncResultT<T> completedResult = End<CompletedAsyncResultT<T>>(result);
            return completedResult._data;
        }
    }

    [Serializable]
    internal class CompletedAsyncResultT2<TResult, TParameter> : AsyncResult
    {
        private readonly TResult _resultData;
        private readonly TParameter _parameter;

        public CompletedAsyncResultT2(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
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
            CompletedAsyncResultT2<TResult, TParameter> completedResult = End<CompletedAsyncResultT2<TResult, TParameter>>(result);
            parameter = completedResult._parameter;
            return completedResult._resultData;
        }
    }
}
