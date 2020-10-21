using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Atomex.Guard.Common
{
    public class HttpRequestHeaders : List<KeyValuePair<string, IEnumerable<string>>> { };

    public static class HttpHelper
    {
        public const int SslHandshakeFailed = 525;
        public static HttpClient HttpClient { get; } = new HttpClient();

        public static async Task<HttpResponseMessage> SendRequestAsync(
            string baseUri,
            string requestUri,
            HttpMethod method,
            HttpContent content,
            HttpRequestHeaders headers,
            bool useCache = false,
            string pathToCache = null,
            RequestLimitControl requestLimitControl = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(method, new Uri($"{baseUri}{requestUri}"));

            if (headers != null)
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

            if (method == HttpMethod.Post)
                request.Content = content;

            if (useCache && method == HttpMethod.Get)
            {
                var cacheContent = await new HttpCache(pathToCache).GetAsync(
                    url: baseUri + requestUri,
                    cancellationToken: cancellationToken);

                if (cacheContent != null)
                    return new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(cacheContent)
                    };
            }

            if (requestLimitControl != null)
                await requestLimitControl.WaitAsync(cancellationToken);

            var response = await HttpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (useCache && response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                await new HttpCache(pathToCache).AddAsync(
                    url: baseUri + requestUri,
                    content: responseContent,
                    cancellationToken: cancellationToken);
            }

            return response;
        }

        private static async Task<T> SendRequestAsync<T>(
            string baseUri,
            string requestUri,
            HttpMethod method,
            HttpContent content,
            HttpRequestHeaders headers,
            Func<HttpResponseMessage, Task<T>> responseHandler,
            bool useCache = false,
            string pathToCache = null,
            RequestLimitControl requestLimitControl = null,
            CancellationToken cancellationToken = default)
        {
            using var response = await SendRequestAsync(
                baseUri: baseUri,
                requestUri: requestUri,
                method: method,
                content: content,
                headers: headers,
                useCache: useCache,
                pathToCache: pathToCache,
                requestLimitControl: requestLimitControl,
                cancellationToken: cancellationToken);

            return await responseHandler(response);
        }

        public static Task<T> GetAsync<T>(
            string baseUri,
            string requestUri,
            HttpRequestHeaders headers,
            Func<HttpResponseMessage, Task<T>> responseHandler,
            bool useCache = false,
            string pathToCache = null,
            RequestLimitControl requestLimitControl = null,
            CancellationToken cancellationToken = default)
        {
            return SendRequestAsync(
                baseUri: baseUri,
                requestUri: requestUri,
                method: HttpMethod.Get,
                content: null,
                headers: headers,
                responseHandler: responseHandler,
                useCache: useCache,
                pathToCache: pathToCache,
                requestLimitControl: requestLimitControl,
                cancellationToken: cancellationToken);
        }

        public static Task<T> GetAsync<T>(
            string baseUri,
            string requestUri,
            Func<HttpResponseMessage, Task<T>> responseHandler,
            bool useCache = false,
            string pathToCache = null,
            RequestLimitControl requestLimitControl = null,
            CancellationToken cancellationToken = default)
        {
            return GetAsync(
                baseUri: baseUri,
                requestUri: requestUri,
                headers: null,
                responseHandler: responseHandler,
                useCache: useCache,
                pathToCache: pathToCache,
                requestLimitControl: requestLimitControl,
                cancellationToken: cancellationToken);
        }
    }
}