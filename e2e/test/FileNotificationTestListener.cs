// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class FileNotificationTestListener
    {
        private static readonly TimeSpan s_duration = TimeSpan.FromHours(2);
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_checkInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_checkDuration = TimeSpan.FromMinutes(5);
        private static readonly TestLogging s_log = TestLogging.GetInstance();

        private static readonly SemaphoreSlim s_lock = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentDictionary<string, FileNotification> s_fileNotifications = new ConcurrentDictionary<string, FileNotification>();

        private static FileNotificationReceiver<FileNotification> s_fileNotificationReceiver;
        private static bool s_receiving = false;

        public static async Task InitAsync()
        {
            bool gained = await s_lock.WaitAsync(s_interval).ConfigureAwait(false);
            if (gained)
            {
                try
                {
                    if (!s_receiving)
                    {
                        s_log.WriteLine("Initializing FileNotificationReceiver...");
                        ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
                        s_fileNotificationReceiver = serviceClient.GetFileNotificationReceiver();
                        s_log.WriteLine("Receiving once to connect FileNotificationReceiver...");
                        await s_fileNotificationReceiver.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        s_log.WriteLine("FileNotificationReceiver connected.");
                        _ = StartReceivingLoopAsync().ConfigureAwait(false);
                        s_receiving = true;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
        }

        public static async Task VerifyFileNotification(string fileName, string deviceId)
        {
            string key = RetrieveKey(fileName);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                while (stopwatch.Elapsed < s_checkDuration)
                {
                    bool received = s_fileNotifications.TryRemove(key, out var fileNotification);
                    if (received)
                    {
                        s_log.WriteLine($"Completing FileNotification: deviceId={fileNotification.DeviceId}, blobName={fileNotification.BlobName}.");

                        Assert.AreEqual(deviceId, fileNotification.DeviceId);
                        Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty.");
                        try
                        {
                            await s_fileNotificationReceiver.CompleteAsync(fileNotification).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            s_log.WriteLine("Ignore any exception while completing file upload notification.");
                        }
                        return;
                    }
                    await Task.Delay(s_checkInterval).ConfigureAwait(false);
                }
            }
            finally
            {
                stopwatch.Stop();
            }

            Assert.Fail($"FileNotification is not received in {s_checkDuration}: deviceId={deviceId}, fileName={fileName}.");
        }

        private static async Task StartReceivingLoopAsync()
        {
            s_log.WriteLine("Starting receiving file notification loop...");

            using var cts = new CancellationTokenSource(s_duration);
            var cancellationToken = cts.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    FileNotification fileNotification = await s_fileNotificationReceiver.ReceiveAsync(s_interval).ConfigureAwait(false);
                    if (fileNotification != null)
                    {
                        string key = RetrieveKey(fileNotification.BlobName);
                        s_fileNotifications.TryAdd(key, fileNotification);
                        s_log.WriteLine($"File notification received deviceId={fileNotification.DeviceId}, blobName={fileNotification.BlobName}.");
                        await s_fileNotificationReceiver.AbandonAsync(fileNotification).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    s_log.WriteLine("Ignore any exception while receiving/abandon file upload notification.");
                }
            }

            s_log.WriteLine("End receiving file notification loop.");
        }

        private static string RetrieveKey(string fileName)
        {
            return RetrieveValueAfterChar(RetrieveValueAfterChar(fileName, '/'), '\\');
        }

        private static string RetrieveValueAfterChar(string value, char ch)
        {
            return value.Substring(value.LastIndexOf(ch) + 1);
        }
    }
}
