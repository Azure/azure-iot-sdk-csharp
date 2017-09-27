// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Query on enrollments, enrollmentGroups and registrations 
    /// </summary>
    class DpsQuery : IDpsQuery
    {
        private string continuationToken = string.Empty;
        private bool newQuery = true;
        private readonly Func<string, Task<DpsQueryResult>> queryTaskFunc;

        /// <summary>
        ///     internal ctor
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="queryTaskFunc"></param>
        internal DpsQuery(Func<string, Task<DpsQueryResult>> queryTaskFunc)
        {
            this.queryTaskFunc = queryTaskFunc;
        }

        /// <summary>
        ///     return true before any next calls or when a continuation token is present
        /// </summary>
        public bool HasMoreResults => newQuery || !string.IsNullOrEmpty(this.continuationToken);

        /// <summary>
        ///     fetch the next paged result as Json strings
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetNextAsJsonAsync()
        {
            return await this.GetNextAsJsonAsync(null);
        }

        public async Task<QueryResponse<string>> GetNextAsJsonAsync(QueryOptions options)
        {
            IEnumerable<string> response;
            if (!this.HasMoreResults)
            {
                response = new List<string>();
            }
            else
            {
                DpsQueryResult r = await this.GetNextAsync(options);
                response = r.Items.Select(o => o.ToString());
            }

            return new QueryResponse<string>(response, this.continuationToken);
        }

        public async Task<IEnumerable<Enrollment>> GetNextAsEnrollmentAsync()
        {
            return await this.GetNextAsEnrollmentAsync(null);
        }

        public async Task<QueryResponse<Enrollment>> GetNextAsEnrollmentAsync(QueryOptions options)
        {
            IEnumerable<Enrollment> results = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<Enrollment>(DpsQueryResultType.Enrollment, options)
                : new List<Enrollment>();

            return new QueryResponse<Enrollment>(results, this.continuationToken);
        }

        public async Task<IEnumerable<EnrollmentGroup>> GetNextAsEnrollmentGroupAsync()
        {
            return await this.GetNextAsEnrollmentGroupAsync(null);
        }

        public async Task<QueryResponse<EnrollmentGroup>> GetNextAsEnrollmentGroupAsync(QueryOptions options)
        {
            IEnumerable<EnrollmentGroup> results = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<EnrollmentGroup>(DpsQueryResultType.EnrollmentGroup, options)
                : new List<EnrollmentGroup>();

            return new QueryResponse<EnrollmentGroup>(results, this.continuationToken);
        }

        public async Task<IEnumerable<RegistrationStatus>> GetNextAsRegistrationStatusAsync()
        {
            return await this.GetNextAsRegistrationStatusAsync(null);
        }

        public async Task<QueryResponse<RegistrationStatus>> GetNextAsRegistrationStatusAsync(QueryOptions options)
        {
            IEnumerable<RegistrationStatus> results = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<RegistrationStatus>(DpsQueryResultType.DeviceRegistration, options)
                : new List<RegistrationStatus>();

            return new QueryResponse<RegistrationStatus>(results, this.continuationToken);
        }

        async Task<IEnumerable<T>> GetAndCastNextResultAsync<T>(DpsQueryResultType type, QueryOptions options)
        {
            DpsQueryResult r = await this.GetNextAsync(options);
            return CastResultContent<T>(r, type);
        }

        static IEnumerable<T> CastResultContent<T>(DpsQueryResult result, DpsQueryResultType expected)
        {
            if (result.Type != expected)
            {
                throw new InvalidCastException($"result type is {result.Type}");
            }

            // TODO: optimize this 2nd parse from JObject to target object type T
            return result.Items.Select(o => ((JObject) o).ToObject<T>());
        }

        async Task<DpsQueryResult> GetNextAsync(QueryOptions options)
        {
            this.newQuery = false;
            DpsQueryResult result = await this.queryTaskFunc(!string.IsNullOrWhiteSpace(options?.ContinuationToken) ? options.ContinuationToken : this.continuationToken);
            this.continuationToken = result.ContinuationToken;
            return result;
        }
    }
}
