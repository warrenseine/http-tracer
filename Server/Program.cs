using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HttpTracer.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HttpTracer.Server
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await Task.WhenAll(Task.Run(RunHttpRequests), CreateHostBuilder(args).Build().RunAsync());
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<HttpEventListenerService>();
                });

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

            using var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

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
