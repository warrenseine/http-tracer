using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HttpTracer.Server;

namespace HttpTracer.Example
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            await Task.WhenAll(HttpTracerServer.Run(), RunHttpRequests());
        }

        private static readonly IReadOnlyList<string> Urls = new[]
        {
            "http://localhost:5000/css/app.css", // 200 OK
            "https://localhost:5001/css/app.css", // 200 OK (https)
            "http://localhost:5000/favicon.ico", // 200 OK (long)
            "http://localhost:5000/not-found.html", // 404 Not Found
            "http://localhost:5001/foo.jpg", // Refused
        };

        private static readonly Random Random = new Random();

        private static async Task RunHttpRequests()
        {
            await Task.Delay(5000);

            while (true)
            {
                var url = Urls[Random.Next(Urls.Count)];
                var delay = Random.Next(400) + 1100;
                await Task.Delay(delay);

                await RunHttpRequest(url);
            }
        }

        private static async Task RunHttpRequest(string url)
        {
            var uri = new Uri(url);
            var message = new HttpRequestMessage(HttpMethod.Get, uri.PathAndQuery);

            using var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority))
            };

            try
            {
                await client.SendAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
