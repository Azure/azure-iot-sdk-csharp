// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Azure.IoT.DigitalTwin.E2ETests.meta
{
    /// <summary>
    /// Spawn a thread to run this listener which will read all EventHub messages and store them in a gettable list
    /// for verifications from any test suite
    /// </summary>
    public class EventHubListener : IDisposable
    {
        private EventHubClient eventHubClient;
        private string EventHubConnectionString = Configuration.EventHubConnectionString;

        private bool startedListening = false;

        private BlockingCollection<string> receivedMessages;

        private IList<Thread> eventhubReceiverThreads;
        private IList<PartitionReceiver> partitionReceivers;

        private static EventHubListener instance;

        private static int receivedMessagesCheckPeriodMilliseconds = 100;

        private static readonly object instanceLock = new object();

        public static EventHubListener Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new EventHubListener();
                    }
                    return instance;
                }
            }
        }

        private EventHubListener()
        {
            //singleton pattern, not publicly accessible only purpose
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString);

            receivedMessages = new BlockingCollection<string>();
            partitionReceivers = new List<PartitionReceiver>();
            eventhubReceiverThreads = new List<Thread>();
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
        }

        public void startListening()
        {
            if (startedListening)
            {
                return;
            }

            startedListening = true;

            EventHubRuntimeInformation runtimeInformation = eventHubClient.GetRuntimeInformationAsync().Result;

            //spawn a thread per partition to receive over that partition
            int partitionIndex = 0;
            foreach (string partitionId in runtimeInformation.PartitionIds)
            {
                partitionReceivers.Add(eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, EventPosition.FromEnqueuedTime(DateTime.Now)));

                Thread partitionReceiverThread = new Thread(() => receive(partitionIndex));
                eventhubReceiverThreads.Add(partitionReceiverThread);
                partitionReceiverThread.Start();
                partitionIndex++;
            }
        }

        private void receive(int partitionIndex)
        {
            try
            {
                while (true)
                {
                    PartitionReceiver partitionReceiver = partitionReceivers.ElementAt(partitionIndex - 1);
                    var receiveTask = partitionReceiver.ReceiveAsync(1, TimeSpan.FromSeconds(10));
                    receiveTask.Wait();
                    IEnumerable<EventData> receivedEvents = receiveTask.Result;

                    if (receivedEvents != null)
                    {
                        foreach (EventData eventData in receivedEvents)
                        {
                            receivedMessages.Add(Encoding.UTF8.GetString(eventData.Body.ToArray()));
                        }
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                //Thread was aborted, so allow it to end
            }
        }

        public bool messageWasReceived(string expectedPayload, int timeoutOutSeconds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                foreach (string payload in receivedMessages)
                {
                    if (expectedPayload.Equals(payload, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (stopwatch.Elapsed.TotalSeconds > timeoutOutSeconds)
                {
                    throw new TimeoutException("Timed out waiting for message to be received by EventHub receiver");
                }

                //If EventHub listener has stopped listening, then don't wait for timeout, the message will never be received
                if (!startedListening)
                {
                    return false;
                }

                Thread.Sleep(receivedMessagesCheckPeriodMilliseconds);
            }
        }

        public bool messageContainingSubstringsWasReceived(int timeoutOutSeconds, string[] expectedSubstrings)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                if (messageReceivedThatContainsAllSubstrings(expectedSubstrings))
                {
                    return true;
                }

                if (stopwatch.Elapsed.TotalSeconds > timeoutOutSeconds)
                {
                    throw new TimeoutException("Timed out waiting for message to be received by EventHub receiver");
                }

                //If EventHub listener has stopped listening, then don't wait for timeout, the message will never be received
                if (!startedListening)
                {
                    return false;
                }

                Thread.Sleep(receivedMessagesCheckPeriodMilliseconds);
            }
        }

        private bool messageReceivedThatContainsAllSubstrings(string[] substrings)
        {
            foreach (string payload in receivedMessages)
            {
                bool payloadContainsAllSubstrings = true;
                foreach (string substring in substrings)
                {
                    payloadContainsAllSubstrings &= payload.Replace(" ", "").Contains(substring.Replace(" ", ""));
                }

                if (payloadContainsAllSubstrings)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            foreach (Thread receiverThread in eventhubReceiverThreads)
            {
                receiverThread.Interrupt();
            }

            foreach (PartitionReceiver receiver in partitionReceivers)
            {
                receiver.Close();
            }

            startedListening = false;
        }
    }
}
