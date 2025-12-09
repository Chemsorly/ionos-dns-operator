using IonosDns;
using IonosDnsOperator.Configuration;
using IonosDnsOperator.Mappers;
using IonosDnsOperator.Services;
using KubeOps.Abstractions.Builder;
using KubeOps.Operator;
using KubeOps.Operator.Web.Builder;
using KubeOps.Operator.Web.Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var opBuilder = builder.Services
	.AddKubernetesOperator(settings =>
	{
		settings.LeaderElectionType = LeaderElectionType.Single;
		settings.LeaderElectionLeaseDuration = TimeSpan.FromSeconds(15);
		settings.LeaderElectionRenewDeadline = TimeSpan.FromSeconds(10);
		settings.LeaderElectionRetryPeriod = TimeSpan.FromSeconds(2);
	})
	.RegisterComponents();

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Standard Services
builder.Services
    .AddLogging()
    .Configure<IonosConfiguration>(builder.Configuration.GetSection("Ionos"))
    .AddScoped<DnsRecordMapper>()
    .AddScoped<IIonosDnsClient>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<IonosConfiguration>>();
        return new IonosDnsClient(new IonosDnsClientOptions { ApiKey = config.Value.ApiKey });
    })
    .AddScoped<IDnsSyncService, DnsSyncService>();

// Operator
#if DEBUG
opBuilder.AddCrdInstaller(c =>
    {
        // Careful, this can be very destructive.
        c.OverwriteExisting = true;
        //c.DeleteOnShutdown = true;
    });

const string ip = "192.168.1.191"; // local ip
const ushort port = 8443;
using var generator = new CertificateGenerator(ip);
var cert = generator.Server.CopyServerCertWithPrivateKey();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.Listen(IPAddress.Any, port, listenOptions =>
	{
		listenOptions.UseHttps(cert);
	});
});

opBuilder.UseCertificateProvider(port, ip, generator);
#endif
// Webhook
builder.Services
	.AddControllers()
	// fix: json to enum serialization with kubeops
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
		options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
	});

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

var ionosConfiguration = host.Services.GetRequiredService<IOptions<IonosConfiguration>>().Value;
if (string.IsNullOrWhiteSpace(ionosConfiguration.ApiKey))
	throw new ArgumentException("Ionos.ApiKey is not set in configuration");

// Webhook
host.UseRouting();
host.UseDeveloperExceptionPage();
host.MapControllers();

logger.LogInformation("Starting operator");
await host.RunAsync();