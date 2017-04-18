﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Ingestion.Models;
using Microsoft.Azure.Mobile.Ingestion;
using Microsoft.Azure.Mobile.Storage;
using Microsoft.Azure.Mobile.Utils;
using Microsoft.Azure.Mobile.Utils.Synchronization;

namespace Microsoft.Azure.Mobile.Channel
{
    public sealed class Channel : IChannelUnit
    {
        private const int ClearBatchSize = 100;
        private Ingestion.Models.Device _device;
        private readonly string _appSecret;
        private readonly IStorage _storage;
        private readonly IIngestion _ingestion;
        private readonly IDeviceInformationHelper _deviceInfoHelper = new DeviceInformationHelper();
        private readonly Dictionary<string, List<Log>> _sendingBatches = new Dictionary<string, List<Log>>();
        private readonly int _maxParallelBatches;
        private readonly int _maxLogsPerBatch;
        private long _pendingLogCount;
        private bool _enabled;
        private bool _discardLogs;
        private bool _batchScheduled;
        private TimeSpan _batchTimeInterval;
        private readonly StatefulMutex _mutex;
        private readonly StateKeeper _stateKeeper = new StateKeeper();

        internal Channel(string name, int maxLogsPerBatch, TimeSpan batchTimeInterval, int maxParallelBatches,
            string appSecret, IIngestion ingestion, IStorage storage)
        {
            _mutex = new StatefulMutex(_stateKeeper);
            Name = name;
            _maxParallelBatches = maxParallelBatches;
            _maxLogsPerBatch = maxLogsPerBatch;
            _appSecret = appSecret;
            _ingestion = ingestion;
            _storage = storage;
            _batchTimeInterval = batchTimeInterval;
            _batchScheduled = false;
            _enabled = true;
            DeviceInformationHelper.InformationInvalidated += (sender, e) => InvalidateDeviceCache();
            Task.Factory.StartNew(CountFromDiskAsync);
        }

        public void SetEnabled(bool enabled)
        {
            _mutex.Lock();
            try
            {
                if (_enabled == enabled)
                {
                    return;
                }
                if (enabled)
                {
                    _enabled = true;
                    _discardLogs = false;
                    _stateKeeper.InvalidateState();
                    CheckPendingLogs();
                }
                else
                {
                    Suspend(true, new CancellationException());
                }
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        /// <summary>
        /// Gets value indicating whether the Channel is enabled
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                _mutex.Lock();
                try
                {
                    return _enabled;
                }
                finally
                {
                    _mutex.Unlock();
                }
            }
        }

        /// <summary>
        /// The channel's name
        /// </summary>
        public string Name { get; }

        #region Events
        public event EventHandler<EnqueuingLogEventArgs> EnqueuingLog;
        public event EventHandler<SendingLogEventArgs> SendingLog;
        public event EventHandler<SentLogEventArgs> SentLog;
        public event EventHandler<FailedToSendLogEventArgs> FailedToSendLog;
        #endregion

        public void Enqueue(Log log)
        {
            _mutex.Lock();
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            try
            {
                if (_discardLogs)
                {
                    MobileCenterLog.Warn(MobileCenterLog.LogTag, "Channel is disabled; logs are discarded");
                    _mutex.Unlock();
                    SendingLog?.Invoke(this, new SendingLogEventArgs(log));
                    FailedToSendLog?.Invoke(this, new FailedToSendLogEventArgs(log, new CancellationException()));
                    return;
                }
                _mutex.Unlock();
                EnqueuingLog?.Invoke(this, new EnqueuingLogEventArgs(log));
                _mutex.Lock(stateSnapshot);
                PrepareLog(log);
                Task.Factory.StartNew(() => PersistLogAsync(log, stateSnapshot));
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The Enqueue operation has been cancelled", e);
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        private void PrepareLog(Log log)
        {
            if (log.Device == null && _device == null)
            {
                _device = _deviceInfoHelper.GetDeviceInformation();
            }
            log.Device = log.Device ?? _device;
            if (log.Toffset == 0L)
            {
                log.Toffset = TimeHelper.CurrentTimeInMilliseconds();
            }
        }

        private async Task PersistLogAsync(Log log, State stateSnapshot)
        {
            try
            {
                await _storage.PutLogAsync(Name, log).ConfigureAwait(false);
            }
            catch (StorageException e)
            {
                MobileCenterLog.Error(MobileCenterLog.LogTag, "Error persisting log", e);
                return;
            }
            try
            {
                await _mutex.LockAsync(stateSnapshot).ConfigureAwait(false);
                _pendingLogCount++;
                if (_enabled)
                {
                    CheckPendingLogs();
                    return;
                }
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "Channel is temporarily disabled; log was saved to disk");
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The PersistLog operation has been cancelled", e);
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        public void InvalidateDeviceCache()
        {
            _mutex.Lock();
            _device = null;
            _mutex.Unlock();
        }

        public void Clear()
        {
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            _storage.DeleteLogsAsync(Name).ContinueWith(completedTask =>
            {
                try
                {
                    _mutex.Lock(stateSnapshot);
                    _pendingLogCount = 0;
                    _mutex.Unlock();
                }
                catch (StatefulMutexException)
                {
                    //Nothing to handle
                }
            });
        }

        private async Task CountFromDiskAsync()
        {
            await _mutex.LockAsync().ConfigureAwait(false);
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            _mutex.Unlock();
            var logCount = await _storage.CountLogsAsync(Name).ConfigureAwait(false);
            try
            {
                await _mutex.LockAsync(stateSnapshot).ConfigureAwait(false);
                _pendingLogCount = logCount;
                CheckPendingLogs();
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The CountFromDisk operation has been cancelled", e);
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        private void Suspend(bool deleteLogs, Exception exception)
        {
            _enabled = false;
            _batchScheduled = false;
            _discardLogs = deleteLogs;
            _stateKeeper.InvalidateState();
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            try
            {
                if (deleteLogs && FailedToSendLog != null)
                {
                    foreach (var log in _sendingBatches.Values.SelectMany(batch => batch))
                    {
                        _mutex.Unlock();
                        FailedToSendLog?.Invoke(this, new FailedToSendLogEventArgs(log, exception));
                        _mutex.Lock(stateSnapshot);
                    }
                }
                try
                {
                    _ingestion.Close();
                }
                catch (IngestionException e)
                {
                    MobileCenterLog.Error(MobileCenterLog.LogTag, "Failed to close ingestion", e);
                }
                if (deleteLogs)
                {
                    _pendingLogCount = 0;
                    Task.Run(DeleteLogsOnSuspendedAsync);
                    return;
                }
                Task.Run(() => _storage.ClearPendingLogStateAsync(Name));
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The CountFromDisk operation has been cancelled", e);
            }
        }

        private async Task DeleteLogsOnSuspendedAsync()
        {
            await _mutex.LockAsync().ConfigureAwait(false);
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            try
            {
                if (SendingLog != null || FailedToSendLog != null)
                {
                    await SignalDeletingLogs(stateSnapshot).ConfigureAwait(false);
                }
            }
            catch (StorageException)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "Failed to invoke events for logs being deleted.");
                return;
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The DeleteLogs operation has been cancelled", e);
                return;
            }
            finally
            {
                _mutex.Unlock();
            }
            await _storage.DeleteLogsAsync(Name).ConfigureAwait(false);
        }

        private async Task SignalDeletingLogs(State stateSnapshot)
        {
            var logs = new List<Log>();
            _mutex.Unlock();
            await _storage.GetLogsAsync(Name, ClearBatchSize, logs).ConfigureAwait(false);
            await _mutex.LockAsync(stateSnapshot).ConfigureAwait(false);
            foreach (var log in logs)
            {
                _mutex.Unlock();
                SendingLog?.Invoke(this, new SendingLogEventArgs(log));
                FailedToSendLog?.Invoke(this, new FailedToSendLogEventArgs(log, new CancellationException()));
                await _mutex.LockAsync(stateSnapshot);
            }
            if (logs.Count >= ClearBatchSize)
            {
                await SignalDeletingLogs(stateSnapshot).ConfigureAwait(false);
            }
        }

        private async Task TriggerIngestionAsync()
        {
            await _mutex.LockAsync().ConfigureAwait(false);
            var stateSnapshot = _stateKeeper.GetStateSnapshot();
            try
            {
                if (!_enabled)
                {
                    return;
                }
                MobileCenterLog.Debug(MobileCenterLog.LogTag,
                    $"triggerIngestion({Name}) pendingLogCount={_pendingLogCount}");
                _batchScheduled = false;
                if (_sendingBatches.Count >= _maxParallelBatches)
                {
                    MobileCenterLog.Debug(MobileCenterLog.LogTag,
                        "Already sending " + _maxParallelBatches + " batches of analytics data to the server");
                    return;
                }

                // Get a batch from storage
                var logs = new List<Log>();
                _mutex.Unlock();
                var batchId = await _storage.GetLogsAsync(Name, _maxLogsPerBatch, logs).ConfigureAwait(false);
                await _mutex.LockAsync(stateSnapshot).ConfigureAwait(false);
                if (batchId != null)
                {
                    _sendingBatches.Add(batchId, logs);
                    _pendingLogCount -= logs.Count;
                    TriggerIngestion(logs, stateSnapshot, batchId);
                }
            }
            catch (StatefulMutexException e)
            {
                MobileCenterLog.Warn(MobileCenterLog.LogTag, "The TriggerIngestion operation has been cancelled", e);
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        private void TriggerIngestion(IList<Log> logs, State stateSnapshot, string batchId)
        {
            // Before sending logs, trigger the sending event for this channel
            if (SendingLog != null)
            {
                foreach (var eventArgs in logs.Select(log => new SendingLogEventArgs(log)))
                {
                    _mutex.Unlock();
                    SendingLog?.Invoke(this, eventArgs);
                    _mutex.Lock(stateSnapshot);
                }
            }
            var installId = MobileCenter.InstallId.HasValue ? MobileCenter.InstallId.Value : Guid.Empty;
            var serviceCall = _ingestion.PrepareServiceCall(_appSecret, installId, logs);

            serviceCall.ServiceCallFailedCallback = exception =>
            {
                serviceCall.Dispose();
                HandleSendingFailure(batchId, exception);
            };

            serviceCall.ServiceCallSucceededCallback = async () =>
            {
                serviceCall.Dispose();
                if (!_stateKeeper.IsCurrent(stateSnapshot))
                {
                    return;
                }
                try
                {
                    await _storage.DeleteLogsAsync(Name, batchId).ConfigureAwait(false);
                }
                catch (StorageException e)
                {
                    MobileCenterLog.Warn(MobileCenterLog.LogTag, $"Could not delete logs for batch {batchId}", e);
                }
                try
                {
                    await _mutex.LockAsync(stateSnapshot).ConfigureAwait(false);
                    var removedLogs = _sendingBatches[batchId];
                    _sendingBatches.Remove(batchId);
                    if (SentLog != null)
                    {
                        foreach (var log in removedLogs)
                        {
                            _mutex.Unlock();
                            SentLog?.Invoke(this, new SentLogEventArgs(log));
                            _mutex.Lock(stateSnapshot);
                        }
                    }
                    CheckPendingLogs();
                }
                catch (StatefulMutexException)
                {
                }
                finally
                {
                    _mutex.Unlock();
                }
            };
            serviceCall.Execute();
        }

        private void HandleSendingFailure(string batchId, IngestionException e)
        {
            var isRecoverable = e?.IsRecoverable ?? false;
            MobileCenterLog.Error(MobileCenterLog.LogTag, $"Sending logs for channel '{Name}', batch '{batchId}' failed", e);
            var removedLogs = _sendingBatches[batchId];
            _sendingBatches.Remove(batchId);
            if (!isRecoverable && FailedToSendLog != null)
            {
                foreach (var log in removedLogs)
                {
                    FailedToSendLog?.Invoke(this, new FailedToSendLogEventArgs(log, e));
                }
            }
            _mutex.Lock();
            try
            {
                Suspend(!isRecoverable, e);
                if (isRecoverable)
                {
                    _pendingLogCount += removedLogs.Count;
                }
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        private void CheckPendingLogs()
        {
            if (!_enabled)
            {
                MobileCenterLog.Info(MobileCenterLog.LogTag, "The service has been disabled. Stop processing logs.");
                return;
            }

            MobileCenterLog.Debug(MobileCenterLog.LogTag, $"CheckPendingLogs({Name}) pending log count: {_pendingLogCount}");
            if (_pendingLogCount >= _maxLogsPerBatch)
            {
                Task.Run(TriggerIngestionAsync);
            }
            else if (_pendingLogCount > 0 && !_batchScheduled)
            {
                _batchScheduled = true;
                Task.Delay((int)_batchTimeInterval.TotalMilliseconds).ContinueWith(async completedTask =>
                {
                    if (_batchScheduled)
                    {
                        await TriggerIngestionAsync().ConfigureAwait(false);
                    }
                });
            }
        }

        public void Shutdown()
        {
            _mutex.Lock();
            try
            {
                Suspend(false, new CancellationException());
            }
            finally
            {
                _mutex.Unlock();
            }
        }

        public void Dispose()
        {
            _mutex.Dispose();
        }
    }
}
