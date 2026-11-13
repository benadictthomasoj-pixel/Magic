using Magic.Infrastructure.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Magic.Api.IntegrationTests;

/// <summary>
/// Authoritative isolated integration test harness.
/// Directs all test operations to the dedicated 'magic_test' database, ensuring zero contamination of 'magic_dev'.
/// Explicitly disables background outbox polling workers during tests to prevent worker contention.
/// </summary>
public class MagicTestFixture : WebApplicationFactory<Program>
{
    public const string TestConnectionString = "Host=127.0.0.1;Port=5432;Database=magic_test;Username=magic_dev";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MagicDatabase", TestConnectionString);
        builder.UseSetting("Seeding:AutoSeedDevelopmentData", "false");
        var testKey = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("MagicTest_IsolatedTestKey_OnlyForIntegrationTests_2026")));
        builder.UseSetting("Security:BankEncryptionKey", testKey);

        builder.ConfigureServices(services =>
        {
            // Remove automatic background polling worker to eliminate competition with manual test execution
            var outboxWorkerDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(OutboxProcessorBackgroundService));

            if (outboxWorkerDescriptor is not null)
            {
                services.Remove(outboxWorkerDescriptor);
            }
        });
    }
}
