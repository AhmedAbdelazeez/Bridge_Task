using BridgeTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Tests.Infrastructure;

// Runs the real API against a real SQL Server database, so unique indexes, the restrict FK
// and SQL error translation are exercised exactly as in production.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("BRIDGETASK_TEST_SERVER") ?? DefaultServer;

        // The database name is always generated, so the teardown can never drop a database it did not create.
        ConnectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"BridgeTask_Tests_{Guid.NewGuid():N}"
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }
}
