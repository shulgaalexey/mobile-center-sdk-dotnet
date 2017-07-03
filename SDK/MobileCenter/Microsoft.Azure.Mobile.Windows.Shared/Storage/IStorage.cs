﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Ingestion.Models;

namespace Microsoft.Azure.Mobile.Storage
{
    public interface IStorage : IDisposable
    {
        Task PutLogAsync(string channelName, Log log);
        Task DeleteLogsAsync(string channelName, string batchId);
        Task DeleteLogsAsync(string channelName);
        Task<int> CountLogsAsync(string channelName);
        Task ClearPendingLogStateAsync(string channelName);
        Task<string> GetLogsAsync(string channelName, int limit, List<Log> logs);
        Task<bool> ShutdownAsync(TimeSpan timeout);
    }
}
