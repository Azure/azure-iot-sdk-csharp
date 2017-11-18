// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /**
     * The query iterator.
     *
     * <p> The {@code Query} iterator is the result of the query factory for
     * <table summary="Query factories">
     *     <tr>
     *         <td><b>IndividualEnrollment:</b></td>
     *         <td>{@link ProvisioningServiceClient#createIndividualEnrollmentQuery(QuerySpecification, int)}</td>
     *     </tr>
     *     <tr>
     *         <td><b>EnrollmentGroup:</b></td>
     *         <td>{@link ProvisioningServiceClient#createEnrollmentGroupQuery(QuerySpecification, int)}</td>
     *     </tr>
     *     <tr>
     *         <td><b>RegistrationStatus:</b></td>
     *         <td>{@link ProvisioningServiceClient#createEnrollmentGroupRegistrationStatusQuery(QuerySpecification, String, int)}</td>
     *     </tr>
     * </table>
     * <p> On all cases, the <b>QuerySpecification</b> contains a SQL query that must follow the
     *     <a href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language">Query Language</a>
     *     for the Device Provisioning Service.
     *
     * <p> Optionally, an {@code Integer} with the <b>pageSize</b>, can determine the maximum number of the items in the
     *     {@link QueryResult} returned by the {@link #next()}. It must be any positive integer, and if it contains 0,
     *     the Device Provisioning Service will ignore it and use a standard page size.
     *
     * <p> You can use this Object as a standard Iterator, just using the {@link #hasNext()} and {@link #next()} in a
     *     {@code while} loop, up to the point where the {@link #hasNext()} return {@code false}. But, keep in mind
     *     that the {@link QueryResult} can contain a empty list, even if the {@link #hasNext()} returned {@code true}.
     *     For example, image that you have 10 IndividualEnrollment in the Device Provisioning Service and you created
     *     new query with the {@code pageSize} equals 5. The first {@code hasNext()} will return {@code true}, and the
     *     first {@code next()} will return a {@code QueryResult} with 5 items. After that you call the {@code hasNext},
     *     which will returns {@code true}. Now, before you get the next page, somebody delete all the IndividualEnrollment,
     *     What happened, when you call the {@code next()}, it will return a valid {@code QueryResult}, but the
     *     {@link QueryResult#getItems()} will return a empty list.
     *
     * <p> You can also store a query context (QuerySpecification + ContinuationToken) and restart it in the future, from
     *     the point where you stopped.
     *
     * <p> Besides the Items, the queryResult contains the continuationToken, the {@link QueryResult#getContinuationToken()}
     *     shall return it. In any point in the future, you may recreate the query using the same query factories that you
     *     used for the first time, and call {@link #next(String)} providing the stored continuationToken to get the next page.
     *
     * @see <a href="https://docs.microsoft.com/en-us/azure/iot-dps/">Azure IoT Hub Device Provisioning Service</a>
     * @see <a href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language">Query Language</a>
     */
    public class Query
    {
    }
}
