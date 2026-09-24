using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Enables ASP.NET Core integration so HTTP triggers can use HttpRequest / IActionResult.
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// One shared Table Storage client, pointed at the same storage account the Functions host uses.
// Locally this is Azurite ("UseDevelopmentStorage=true"); in Azure it is the Function App's storage account.
builder.Services.AddSingleton(_ =>
{
    var connectionString = builder.Configuration["AzureWebJobsStorage"]
        ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured.");
    return new TableServiceClient(connectionString);
});

builder.Build().Run();
