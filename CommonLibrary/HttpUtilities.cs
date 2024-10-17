using System.Net.NetworkInformation;

namespace CommonLibrary
{
    /// <summary>
    /// Provides utility methods for performing HTTP calls.
    /// </summary>
    /// <remarks>
    /// To inject this library in the Program.cs of an ASP.NET minimal API, add the following code:
    /// 
    /// using Microsoft.Extensions.DependencyInjection;
    /// 
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Add services to the container.
    /// builder.Services.AddHttpClient();
    /// builder.Services.AddSingleton<HttpUtilities>();
    /// 
    /// var app = builder.Build();
    /// 
    /// // Configure the HTTP request pipeline.
    /// app.MapGet("/api/call", async (HttpUtilities httpUtilities) =>
    /// {
    ///     var (success, response, errors) = await httpUtilities.PerformHttpCallAsync(
    ///         "https://example.com/api",
    ///         HttpMethod.Get
    ///     );
    /// 
    ///     if (success)
    ///     {
    ///         return Results.Ok(await response.Content.ReadAsStringAsync());
    ///     }
    ///     else
    ///     {
    ///         return Results.BadRequest(string.Join("\n", errors));
    ///     }
    /// });
    /// 
    /// app.Run();
    /// </remarks>  
    public class HttpUtilities
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpUtilities"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public HttpUtilities(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Performs an HTTP call with the specified parameters.
        /// </summary>
        /// <param name="url">The URL to which the request is sent.</param>
        /// <param name="method">The HTTP method used for the request.</param>
        /// <param name="content">The HTTP content sent with the request.</param>
        /// <param name="headers">The headers to include in the request.</param>
        /// <param name="retries">The number of retry attempts in case of failure.</param>
        /// <returns>A tuple containing a success flag, the HTTP response message, and a list of error messages.</returns>
        public async Task<(bool Success, HttpResponseMessage? HttpResponseMessageObject, List<string> ErrorMessagesList)> PerformHttpCallAsync(string url,
                                                                                                                                               HttpMethod method,
                                                                                                                                               HttpContent? content = null,
                                                                                                                                               Dictionary<string, string>? headers = null,
                                                                                                                                               int retries = 3)
        {
            var client = _httpClientFactory.CreateClient();
            var errorMessages = new List<string>();

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    HttpRequestMessage request = new(method, url) { Content = content };
                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        return (true, response, errorMessages);
                    }
                    else
                    {
                        // Log the reason for failure
                        string errorMessage = $"HTTP call failed with status code: {response.StatusCode}";
                        errorMessages.Add(errorMessage);
                        errorMessages.Add($"Response: {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Log the exception
                    string errorMessage = $"HTTP request exception: {ex.Message}";
                    errorMessages.Add(errorMessage);

                    // Perform network checks
                    if (IsNetworkRelatedException(ex))
                    {
                        errorMessages.Add("Performing network diagnostics...");
                        errorMessages.AddRange(PerformNetworkDiagnostics(url));
                    }
                }
            }

            return (false, null, errorMessages);
        }

        /// <summary>
        /// Determines whether the specified exception is network-related.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns><c>true</c> if the exception is network-related; otherwise, <c>false</c>.</returns>
        private static bool IsNetworkRelatedException(HttpRequestException ex)
        {
            // Check if the exception is network related
            return ex.InnerException is System.Net.Sockets.SocketException;
        }

        /// <summary>
        /// Performs network diagnostics for the specified URL.
        /// </summary>
        /// <param name="url">The URL to diagnose.</param>
        /// <returns>A list of diagnostic messages.</returns>
        private static List<string> PerformNetworkDiagnostics(string url)
        {
            var diagnosticsMessages = new List<string>();

            try
            {
                var uri = new Uri(url);
                var host = uri.Host;

                // Check DNS resolution
                var addresses = System.Net.Dns.GetHostAddresses(host);
                diagnosticsMessages.Add($"DNS resolved {host} to: {string.Join(", ", addresses.Select(a => a.ToString()))}");

                // Check if the server is reachable
                foreach (var address in addresses)
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(address);
                        if (reply.Status == IPStatus.Success)
                        {
                            diagnosticsMessages.Add($"Ping to {address} successful.");
                        }
                        else
                        {
                            diagnosticsMessages.Add($"Ping to {address} failed with status: {reply.Status}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                diagnosticsMessages.Add($"Network diagnostics failed: {ex.Message}");
            }

            return diagnosticsMessages;
        }
    }
}
