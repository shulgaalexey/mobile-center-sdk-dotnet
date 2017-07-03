﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Channel;
using Microsoft.Azure.Mobile.Storage;
using Microsoft.Azure.Mobile.Ingestion.Models;
using Microsoft.Azure.Mobile.Ingestion.Models.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Azure.Mobile.Ingestion;

namespace Microsoft.Azure.Mobile.Test.Channel
{
    [TestClass]
    public class ChannelTest
    {
        private Mobile.Channel.Channel _channel;
        private readonly MockIngestion _mockIngestion = new MockIngestion();
        private IStorage _storage = new Mobile.Storage.Storage();

        private const string ChannelName = "channelName";
        private const int MaxLogsPerBatch = 3;
        private readonly TimeSpan _batchTimeSpan = TimeSpan.FromSeconds(1);
        private const int MaxParallelBatches = 3;
        private readonly string _appSecret = Guid.NewGuid().ToString();

        // We wait tasks now and don't need wait more
        private const int DefaultWaitTime = 500;

        // Event semaphores for invokation verification
        private const int SendingLogSemaphoreIdx = 0;
        private const int SentLogSemaphoreIdx = 1;
        private const int FailedToSendLogSemaphoreIdx = 2;
        private const int EnqueuingLogSemaphoreIdx = 3;
        private readonly List<SemaphoreSlim> _eventSemaphores = new List<SemaphoreSlim> { new SemaphoreSlim(0), new SemaphoreSlim(0), new SemaphoreSlim(0), new SemaphoreSlim(0) };

        public ChannelTest()
        {
            LogSerializer.AddLogType(TestLog.JsonIdentifier, typeof(TestLog));
        }

        [TestInitialize]
        public void InitializeChannelTest()
        {
            _mockIngestion.CallShouldSucceed = true;
            _mockIngestion.Open();
            SetChannelWithTimeSpan(_batchTimeSpan);
        }

        /// <summary>
        /// Verify that channel is enabled by default
        /// </summary>
        [TestMethod]
        public void ChannelEnabledByDefault()
        {
            Assert.IsTrue(_channel.IsEnabled);
        }

        /// <summary>
        /// Verify that disabling channel has the expected effect
        /// </summary>
        [TestMethod]
        public void DisableChannel()
        {
            _channel.SetEnabledAsync(false).RunNotAsync();

            Assert.IsFalse(_channel.IsEnabled);
        }

        /// <summary>
        /// Verify that enabling the channel has the expected effect
        /// </summary>
        [TestMethod]
        public void EnableChannel()
        {
            _channel.SetEnabledAsync(false).RunNotAsync();
            _channel.SetEnabledAsync(true).RunNotAsync();

            Assert.IsTrue(_channel.IsEnabled);
        }

        /// <summary>
        /// Verify that enqueuing a log passes the same log reference to enqueue event argument
        /// </summary>
        [TestMethod]
        public void EnqueuedLogsAreSame()
        {
            var log = new TestLog();
            var sem = new SemaphoreSlim(0);
            _channel.EnqueuingLog += (sender, args) =>
            {
                Assert.AreSame(log, args.Log);
                sem.Release();
            };

            _channel.EnqueueAsync(log).RunNotAsync();
            Assert.IsTrue(sem.Wait(DefaultWaitTime));
        }

        /// <summary>
        /// Verify that when a batch reaches its capacity, it is sent immediately
        /// </summary>
        [TestMethod]
        public void EnqueueMaxLogs()
        {
            SetChannelWithTimeSpan(TimeSpan.FromHours(1));
            for (var i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }
            Assert.IsTrue(SendingLogOccurred(1));
        }

        /// <summary>
        /// Verify that when channel is disabled, sent event is not triggered
        /// </summary>
        [TestMethod]
        public void EnqueueWhileDisabled()
        {
            _channel.SetEnabledAsync(false).RunNotAsync();
            var log = new TestLog();
            _channel.EnqueueAsync(log).RunNotAsync();
            Assert.IsFalse(SentLogOccurred(1));
        }

        [TestMethod]
        public void ChannelInvokesSendingLogEvent()
        {
            for (var i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }

            Assert.IsTrue(SendingLogOccurred(MaxLogsPerBatch));
        }

        [TestMethod]
        public void ChannelInvokesSentLogEvent()
        {
            for (var i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }

            Assert.IsTrue(SentLogOccurred(MaxLogsPerBatch));
        }

        [TestMethod]
        public void ChannelInvokesFailedToSendLogEvent()
        {
            MakeIngestionCallsFail();
            for (var i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }

            Assert.IsTrue(FailedToSendLogOccurred(MaxLogsPerBatch));
        }

        /// <summary>
        /// Validate that links are same on an error and a log
        /// </summary>
        [TestMethod]
        public void FailedToSendLogEventArgsAreSame()
        {
            var ex = new Exception();
            var log = new TestLog();
            var failedEventLogArgs = new FailedToSendLogEventArgs(log, ex);
            Assert.AreSame(log, failedEventLogArgs.Log);
            Assert.AreSame(ex, failedEventLogArgs.Exception);
        }

        /// <summary>
        /// Validate that channel will send log after enabling
        /// </summary>
        [TestMethod]
        public void ChannelInvokesSendingLogEventAfterEnabling()
        {
            _channel.ShutdownAsync().RunNotAsync();
            for (int i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }

            _channel.SetEnabledAsync(true).RunNotAsync();

            Assert.IsTrue(SendingLogOccurred(MaxLogsPerBatch));
        }

        /// <summary>
        /// Validate that FailedToSendLog calls when channel is disabled
        /// </summary>
        [TestMethod]
        public void ChannelInvokesFailedToSendLogEventAfterDisabling()
        {
            _channel.SetEnabledAsync(false).RunNotAsync();
            for (int i = 0; i < MaxLogsPerBatch; ++i)
            {
                _channel.EnqueueAsync(new TestLog()).RunNotAsync();
            }

            Assert.IsTrue(SendingLogOccurred(MaxLogsPerBatch));
            Assert.IsTrue(FailedToSendLogOccurred(MaxLogsPerBatch));
        }

        /// <summary>
        /// Validate that all logs removed
        /// </summary>
        [TestMethod]
        public void ClearLogs()
        {
            _channel.ShutdownAsync().RunNotAsync();
            _channel.EnqueueAsync(new TestLog()).RunNotAsync();

            _channel.ClearAsync().RunNotAsync();
            _channel.SetEnabledAsync(true).RunNotAsync();

            Assert.IsFalse(SendingLogOccurred(1));
        }

        /// <summary>
        /// Validate that channel's mutex is disposed
        /// </summary>
        [TestMethod]
        public void DisposeChannelTest()
        {
            _channel.Dispose();
            Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => _channel.SetEnabledAsync(true)).RunNotAsync();
        }

        /// <summary>
        /// Validate that StorageException is processing without exception
        /// </summary>
        [TestMethod]
        public void ThrowStorageExceptionInDeleteLogsTime()
        {
            var storage = new Mock<IStorage>();
            storage.Setup(s => s.DeleteLogsAsync(It.IsAny<string>(), It.IsAny<string>())).Throws<StorageException>();
            storage.Setup(s => s.GetLogsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Log>>())).Returns(Task.FromResult(""));

            Mobile.Channel.Channel channel = new Mobile.Channel.Channel("name", 1, _batchTimeSpan, 1, _appSecret, _mockIngestion, storage.Object);

            // Shutdown channel and store some log
            channel.ShutdownAsync().RunNotAsync();
            channel.EnqueueAsync(new TestLog()).RunNotAsync();

            channel.SetEnabledAsync(true).RunNotAsync();

            // Not throw any exception
        }

        /// <summary>
        /// Verify that when a recoverable http error occurs, ingestion
        /// stays open
        /// </summary>
        [TestMethod]
        public void IngestionNotClosedOnRecoverableHttpError()
        {
            _mockIngestion.CallShouldSucceed = false;
            _mockIngestion.TaskError = new RecoverableIngestionException();
            _channel.EnqueueAsync(new TestLog()).RunNotAsync();

            // wait for SendingLog event
            _eventSemaphores[SendingLogSemaphoreIdx].Wait();
            // wait up to 20 seconds for suspend to finish
            bool disabled = WaitForChannelDisable(TimeSpan.FromSeconds(20));
            Assert.IsTrue(disabled);
            Assert.IsFalse(_mockIngestion.IsClosed);
        }

        /// <summary>
        /// Verify that if a non-recoverable http error occurs, ingestion
        /// is closed
        /// </summary>
        [TestMethod]
        public void IngestionClosedOnNonRecoverableHttpError()
        {
            _mockIngestion.CallShouldSucceed = false;
            _mockIngestion.TaskError = new NonRecoverableIngestionException();
            _channel.EnqueueAsync(new TestLog()).RunNotAsync();

            // wait up to 20 seconds for suspend to finish
            bool disabled = WaitForChannelDisable(TimeSpan.FromSeconds(20));
            Assert.IsTrue(disabled);

            Assert.IsTrue(_mockIngestion.IsClosed);
            Assert.IsFalse(_channel.IsEnabled);
        }

        private void SetChannelWithTimeSpan(TimeSpan timeSpan)
        {
            _storage = new Mobile.Storage.Storage();
            _storage.DeleteLogsAsync(ChannelName).RunNotAsync();
            _channel = new Mobile.Channel.Channel(ChannelName, MaxLogsPerBatch, timeSpan, MaxParallelBatches,
                _appSecret, _mockIngestion, _storage);
            MakeIngestionCallsSucceed();
            SetupEventCallbacks();
        }

        private void MakeIngestionCallsFail()
        {
            _mockIngestion.CallShouldSucceed = false;
        }

        private void MakeIngestionCallsSucceed()
        {
            _mockIngestion.CallShouldSucceed = true;
        }

        private void SetupEventCallbacks()
        {
            foreach (var sem in _eventSemaphores)
            {
                if (sem.CurrentCount != 0)
                {
                    sem.Release(sem.CurrentCount);
                }
            }
            _channel.SendingLog += (sender, args) => { _eventSemaphores[SendingLogSemaphoreIdx].Release(); };
            _channel.SentLog += (sender, args) => { _eventSemaphores[SentLogSemaphoreIdx].Release(); };
            _channel.FailedToSendLog += (sender, args) => { _eventSemaphores[FailedToSendLogSemaphoreIdx].Release(); };
            _channel.EnqueuingLog += (sender, args) => { _eventSemaphores[EnqueuingLogSemaphoreIdx].Release(); };
        }

        private bool FailedToSendLogOccurred(int numTimes, int waitTime = DefaultWaitTime)
        {
            return EventWithSemaphoreOccurred(_eventSemaphores[FailedToSendLogSemaphoreIdx], numTimes, waitTime);
        }

        private bool EnqueuingLogOccurred(int numTimes, int waitTime = DefaultWaitTime)
        {
            return EventWithSemaphoreOccurred(_eventSemaphores[EnqueuingLogSemaphoreIdx], numTimes, waitTime);
        }

        private bool SentLogOccurred(int numTimes, int waitTime = DefaultWaitTime)
        {
            return EventWithSemaphoreOccurred(_eventSemaphores[SentLogSemaphoreIdx], numTimes, waitTime);
        }

        private bool SendingLogOccurred(int numTimes, int waitTime = DefaultWaitTime)
        {
            return EventWithSemaphoreOccurred(_eventSemaphores[SendingLogSemaphoreIdx], numTimes, waitTime);
        }

        private bool WaitForChannelDisable(TimeSpan timeout)
        {
            var task = Task.Run(async () =>
            {
                while (_channel.IsEnabled)
                {
                    await Task.Delay(100);
                }
            });

            return task.Wait(timeout);
        }

        private static bool EventWithSemaphoreOccurred(SemaphoreSlim semaphore, int numTimes, int waitTime)
        {
            for (var i = 0; i < numTimes; ++i)
            {
                if (!semaphore.Wait(waitTime))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
