using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using GiftOfTheGivers.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGivers.Functions.Functions;

// HTTP-triggered functions that keep an audit log of employee project updates in Azure Table Storage.
//   POST /api/project-updates  -> called by the web app when an employee posts an update
//   GET  /api/project-updates  -> lists the newest log entries (easy to check in a browser)
public class ProjectUpdateLog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly TableServiceClient _tableServiceClient;
    private readonly string _tableName;
    private readonly ILogger<ProjectUpdateLog> _logger;

    public ProjectUpdateLog(TableServiceClient tableServiceClient, IConfiguration configuration, ILogger<ProjectUpdateLog> logger)
    {
        _tableServiceClient = tableServiceClient;
        _tableName = configuration["ProjectUpdateLogTable"] ?? "ProjectUpdateLog";
        _logger = logger;
    }

    [Function("LogProjectUpdate")]
    public async Task<IActionResult> LogProjectUpdate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "project-updates")] HttpRequest req)
    {
        ProjectUpdateLogRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<ProjectUpdateLogRequest>(req.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "The request body is not valid JSON." });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return new BadRequestObjectResult(new { error = "Title and description are required." });
        }

        var now = DateTime.UtcNow;
        var entity = new ProjectUpdateLogEntity
        {
            RowKey = $"{DateTime.MaxValue.Ticks - now.Ticks:D19}-{Guid.NewGuid():N}",
            ProjectUpdateId = request.ProjectUpdateId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            PostedByUserId = request.PostedByUserId ?? string.Empty,
            PostedByName = string.IsNullOrWhiteSpace(request.PostedByName) ? "Employee" : request.PostedByName.Trim(),
            PostedOnUtc = DateTime.SpecifyKind(request.PostedOnUtc ?? now, DateTimeKind.Utc),
            LoggedOnUtc = now
        };

        try
        {
            var table = await GetTableAsync();
            await table.AddEntityAsync(entity);
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException)
        {
            _logger.LogError(ex, "Could not write project update {ProjectUpdateId} to table {Table}.", request.ProjectUpdateId, _tableName);
            return new ObjectResult(new { error = "Azure Storage is unavailable. Is Azurite / the storage account running?" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        _logger.LogInformation("Logged project update {ProjectUpdateId} '{Title}' to table {Table}.", entity.ProjectUpdateId, entity.Title, _tableName);

        return new ObjectResult(ToDto(entity)) { StatusCode = StatusCodes.Status201Created };
    }

    [Function("GetProjectUpdateLog")]
    public async Task<IActionResult> GetProjectUpdateLog(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "project-updates")] HttpRequest req)
    {
        var take = int.TryParse(req.Query["take"], out var requested) ? Math.Clamp(requested, 1, 100) : 20;

        try
        {
            var table = await GetTableAsync();
            var entries = new List<object>();

            await foreach (var entity in table.QueryAsync<ProjectUpdateLogEntity>(
                               e => e.PartitionKey == ProjectUpdateLogEntity.DefaultPartitionKey, maxPerPage: take))
            {
                entries.Add(ToDto(entity));
                if (entries.Count >= take)
                {
                    break;
                }
            }

            return new OkObjectResult(new { table = _tableName, count = entries.Count, entries });
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException)
        {
            _logger.LogError(ex, "Could not read table {Table}.", _tableName);
            return new ObjectResult(new { error = "Azure Storage is unavailable. Is Azurite / the storage account running?" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }

    private async Task<TableClient> GetTableAsync()
    {
        var table = _tableServiceClient.GetTableClient(_tableName);
        await table.CreateIfNotExistsAsync();
        return table;
    }

    private static object ToDto(ProjectUpdateLogEntity e) => new
    {
        partitionKey = e.PartitionKey,
        rowKey = e.RowKey,
        e.ProjectUpdateId,
        e.Title,
        e.Description,
        e.PostedByUserId,
        e.PostedByName,
        e.PostedOnUtc,
        e.LoggedOnUtc
    };
}
