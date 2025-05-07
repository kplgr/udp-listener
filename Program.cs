using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace UdpListener
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService<Worker>();

            var app = builder.Build();

            app.Run();
        }
    }
}
