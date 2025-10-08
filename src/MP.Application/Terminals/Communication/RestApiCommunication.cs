using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals.Communication;

namespace MP.Application.Terminals.Communication
{
    /// <summary>
    /// REST API communication for cloud-based terminals
    /// Used by: Stripe Terminal, SumUp, Square Terminal, Adyen
    /// </summary>
    public class RestApiCommunication : ITerminalCommunication, ITransientDependency
    {
        private readonly ILogger<RestApiCommunication> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient? _httpClient;
        private TerminalConnectionSettings? _settings;

        public string ConnectionType => "rest_api";
        public bool IsConnected { get; private set; }

        public RestApiCommunication(
            ILogger<RestApiCommunication> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public Task ConnectAsync(TerminalConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            {
                throw new TerminalCommunicationException("API base URL is required for REST API connection", "MISSING_URL");
            }

            _settings = settings;

            try
            {
                _logger.LogInformation("Connecting to REST API at {BaseUrl}...", settings.ApiBaseUrl);

                _httpClient = _httpClientFactory.CreateClient("TerminalApi");
                _httpClient.BaseAddress = new Uri(settings.ApiBaseUrl);
                _httpClient.Timeout = TimeSpan.FromMilliseconds(settings.Timeout);

                // Set up authentication headers
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    // Bearer token auth (Stripe, SumUp)
                    if (!string.IsNullOrWhiteSpace(settings.AccessToken))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", settings.AccessToken);
                    }
                    // Basic auth or API key header
                    else
                    {
                        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
                    }
                }

                // Set common headers
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "MP-POS/1.0");

                IsConnected = true;

                _logger.LogInformation("Successfully connected to REST API");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to REST API");
                throw new TerminalCommunicationException(
                    $"Failed to connect to API: {ex.Message}",
                    "CONNECTION_FAILED",
                    ex);
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                IsConnected = false;
                _logger.LogInformation("Disconnected from REST API");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during REST API disconnect");
            }

            return Task.CompletedTask;
        }

        public async Task<byte[]> SendAndReceiveAsync(byte[] data, int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _httpClient == null)
            {
                throw new TerminalCommunicationException("Not connected to REST API", "NOT_CONNECTED");
            }

            try
            {
                // Parse the data as JSON to extract HTTP method and endpoint
                var requestJson = Encoding.UTF8.GetString(data);
                var request = JsonSerializer.Deserialize<RestApiRequest>(requestJson);

                if (request == null)
                {
                    throw new TerminalCommunicationException("Invalid request format", "INVALID_REQUEST");
                }

                _logger.LogDebug(
                    "Sending {Method} request to {Endpoint}",
                    request.Method, request.Endpoint);

                HttpResponseMessage response;

                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                switch (request.Method.ToUpper())
                {
                    case "GET":
                        response = await _httpClient.GetAsync(request.Endpoint, linkedCts.Token);
                        break;

                    case "POST":
                        var postContent = new StringContent(
                            request.Body ?? "{}",
                            Encoding.UTF8,
                            "application/json");
                        response = await _httpClient.PostAsync(request.Endpoint, postContent, linkedCts.Token);
                        break;

                    case "PUT":
                        var putContent = new StringContent(
                            request.Body ?? "{}",
                            Encoding.UTF8,
                            "application/json");
                        response = await _httpClient.PutAsync(request.Endpoint, putContent, linkedCts.Token);
                        break;

                    case "DELETE":
                        response = await _httpClient.DeleteAsync(request.Endpoint, linkedCts.Token);
                        break;

                    default:
                        throw new TerminalCommunicationException($"Unsupported HTTP method: {request.Method}", "INVALID_METHOD");
                }

                var responseContent = await response.Content.ReadAsStringAsync(linkedCts.Token);

                _logger.LogDebug(
                    "Received response with status {StatusCode}",
                    (int)response.StatusCode);

                // Build response with status code and body
                var apiResponse = new RestApiResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = responseContent,
                    IsSuccess = response.IsSuccessStatusCode
                };

                var responseJson = JsonSerializer.Serialize(apiResponse);
                return Encoding.UTF8.GetBytes(responseJson);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("REST API communication timeout after {Timeout}ms", timeoutMs);
                throw new TerminalCommunicationException($"API timeout after {timeoutMs}ms", "TIMEOUT");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error");
                throw new TerminalCommunicationException($"HTTP error: {ex.Message}", "HTTP_ERROR", ex);
            }
        }

        public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            // For REST API, send without waiting for response (fire and forget)
            // This is typically not used with REST APIs
            return SendAndReceiveAsync(data, _settings?.Timeout ?? 30000, cancellationToken);
        }

        public async Task<byte[]> ReceiveAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            // REST APIs are request-response, so this is not applicable
            // Return empty response
            await Task.Delay(1, cancellationToken);
            return Array.Empty<byte>();
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _httpClient == null)
            {
                return false;
            }

            try
            {
                // Try to access a health/ping endpoint
                // Most APIs have /health, /ping, or similar
                var response = await _httpClient.GetAsync("/health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                try
                {
                    // Fallback: try root endpoint
                    var response = await _httpClient.GetAsync("/", cancellationToken);
                    return response.StatusCode != System.Net.HttpStatusCode.NotFound;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _httpClient?.Dispose();
        }

        #region Helper Classes

        private class RestApiRequest
        {
            public string Method { get; set; } = "GET";
            public string Endpoint { get; set; } = "/";
            public string? Body { get; set; }
            public System.Collections.Generic.Dictionary<string, string>? Headers { get; set; }
        }

        private class RestApiResponse
        {
            public int StatusCode { get; set; }
            public string Body { get; set; } = "";
            public bool IsSuccess { get; set; }
        }

        #endregion
    }
}
