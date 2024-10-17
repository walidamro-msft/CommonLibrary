using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CommonLibrary.Tests
{
    /// <summary>
    /// Unit tests for the HttpUtilities class.
    /// </summary>
    [TestClass]
    public class HttpUtilitiesTests
    {
        /// <summary>
        /// Tests that a successful HTTP call returns a success response.
        /// </summary>
        [TestMethod]
        public async Task PerformHttpCallAsync_SuccessfulCall_ReturnsSuccess()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success")
                });

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpMessageHandler.Object));

            var httpUtilities = new HttpUtilities(mockHttpClientFactory.Object);

            // Act
            var (isSuccess, response, errorMessages) = await httpUtilities.PerformHttpCallAsync(
                "https://example.com",
                HttpMethod.Get);

            // Assert
            Assert.IsTrue(isSuccess);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, errorMessages.Count);
        }

        /// <summary>
        /// Tests that a failed HTTP call returns a failure response.
        /// </summary>
        [TestMethod]
        public async Task PerformHttpCallAsync_FailedCall_ReturnsFailure()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Server Error")
                });

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpMessageHandler.Object));

            var httpUtilities = new HttpUtilities(mockHttpClientFactory.Object);

            // Act
            var (isSuccess, response, errorMessages) = await httpUtilities.PerformHttpCallAsync(
                "https://example.com",
                HttpMethod.Get);

            // Assert
            Assert.IsFalse(isSuccess);
            Assert.IsNull(response);
            Assert.IsTrue(errorMessages.Count > 0);
            Assert.IsTrue(errorMessages[0].Contains("HTTP call failed with status code: InternalServerError"));
        }

        /// <summary>
        /// Tests a real HTTP GET call to ensure it returns a success response.
        /// </summary>
        [TestMethod]
        public async Task PerformHttpCallAsync_RealHttpCall_Get_ReturnsSuccess()
        {
            // Arrange
            var httpClientFactory = new HttpClientFactory();
            var httpUtilities = new HttpUtilities(httpClientFactory);
            string expectedUrl = "https://httpbin.org/anything";
            string expectedMethod = "GET";

            // Act
            var (isSuccess, response, errorMessages) = await httpUtilities.PerformHttpCallAsync(
                expectedUrl,
                HttpMethod.Get);

            // Assert
            Assert.IsTrue(isSuccess);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, errorMessages.Count);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.AreEqual(expectedUrl, jsonResponse.GetProperty("url").GetString());
            Assert.AreEqual(expectedMethod, jsonResponse.GetProperty("method").GetString());
        }

        /// <summary>
        /// Tests a real HTTP POST call to ensure it returns a success response.
        /// </summary>
        [TestMethod]
        public async Task PerformHttpCallAsync_RealHttpCall_Post_ReturnsSuccess()
        {
            // Arrange
            var httpClientFactory = new HttpClientFactory();
            var httpUtilities = new HttpUtilities(httpClientFactory);
            string expectedUrl = "https://httpbin.org/anything";
            string expectedMethod = "POST";

            // Act
            var (isSuccess, response, errorMessages) = await httpUtilities.PerformHttpCallAsync(
                expectedUrl,
                HttpMethod.Post);

            // Assert
            Assert.IsTrue(isSuccess);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, errorMessages.Count);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.AreEqual(expectedUrl, jsonResponse.GetProperty("url").GetString());
            Assert.AreEqual(expectedMethod, jsonResponse.GetProperty("method").GetString());
        }

        /// <summary>
        /// Tests a real HTTP PUT call with headers and content to ensure it returns a success response.
        /// </summary>
        [TestMethod]
        public async Task PerformHttpCallAsync_RealHttpCall_Put_WithHeaderAndContent_ReturnsSuccess()
        {
            // Arrange
            var httpClientFactory = new HttpClientFactory();
            var httpUtilities = new HttpUtilities(httpClientFactory);
            string expectedUrl = "https://httpbin.org/anything";
            string expectedMethod = "PUT";
            string customHeaderKey = "X-Custom-Header";
            string customHeaderValue = "CustomValue";
            string jsonContent = "{\"key\":\"value\"}";

            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            Dictionary<string, string> headers = new()
                {
                    { customHeaderKey, customHeaderValue }
                };

            // Act
            var (isSuccess, response, errorMessages) = await httpUtilities.PerformHttpCallAsync(
                expectedUrl,
                HttpMethod.Put,
                content,
                headers);

            // Assert
            Assert.IsTrue(isSuccess);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, errorMessages.Count);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.AreEqual(expectedUrl, jsonResponse.GetProperty("url").GetString());
            Assert.AreEqual(expectedMethod, jsonResponse.GetProperty("method").GetString());
            Assert.AreEqual(customHeaderValue, jsonResponse.GetProperty("headers").GetProperty(customHeaderKey).GetString());
            Assert.AreEqual(jsonContent, jsonResponse.GetProperty("data").GetString());
        }

        /// <summary>
        /// A simple implementation of IHttpClientFactory for testing purposes.
        /// </summary>
        private class HttpClientFactory : IHttpClientFactory
        {
            /// <summary>
            /// Creates a new HttpClient instance.
            /// </summary>
            /// <param name="name">The name of the client.</param>
            /// <returns>A new HttpClient instance.</returns>
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }
    }
}