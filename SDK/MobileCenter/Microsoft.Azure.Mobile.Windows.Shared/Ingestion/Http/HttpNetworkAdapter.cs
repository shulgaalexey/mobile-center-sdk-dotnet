﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Mobile.Ingestion.Http
{
    public sealed class HttpNetworkAdapter : IHttpNetworkAdapter
    {
        private HttpClient _httpClient;
        private TimeSpan? _timeout;
        private readonly object _lockObject = new object();

        // Exception codes (HResults) involving poor network connectivity:
        //      0x80072EE7: WININET_E_NAME_NOT_RESOLVED
        //      0x80072EFD: WININET_E_CANNOT_CONNECT
        private static readonly uint[] NetworkUnavailableCodes = {0x80072EE7, 0x80072EFD};

        public HttpNetworkAdapter(TimeSpan? timeout = null)
        {
            _timeout = timeout;
        }

        private HttpClient HttpClient
        {
            get
            {
                lock (_lockObject)
                {
                    if (_httpClient != null)
                    {
                        return _httpClient;
                    }

                    _httpClient = new HttpClient();

                    if (_timeout.HasValue)
                    {
                        _httpClient.Timeout = _timeout.Value;
                    }
                    return _httpClient;
                }
            }
        }

        /// <summary>
        /// Asynchronously makes an HTTP request
        /// </summary>
        /// <param name="request">The request message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the HTTP response</returns>
        /// <exception cref="IngestionException"/>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException e)
            {
                throw new IngestionException(e);
            }
            catch (Exception e)
            {
                // If the HResult indicates a network outage, throw a NetworkIngestionException so
                // it can be dealt with properly
                if (Array.Exists(NetworkUnavailableCodes, code => code == (uint) e.HResult))
                {
                    throw new NetworkIngestionException();
                }
                throw;
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _httpClient?.Dispose();
                _httpClient = null;
            }
        }
    }
}
