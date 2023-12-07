using System.Net;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace System
{
    public static class HttpRequestDataExtensions
    {
        public async static Task<HttpResponseData> CreateResponse(this HttpRequestData req, HttpStatusCode statusCode, object? body)
        {
            var response = req.CreateResponse(statusCode);

            if (body != null)
            {
                await response.WriteAsJsonAsync(body);
            }
            return response;
        }

        public async static Task<T?> Body<T>(this HttpRequestData request)
        {
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
                return default;

            return JsonConvert.DeserializeObject<T>(requestBody);
        }
    }
}
