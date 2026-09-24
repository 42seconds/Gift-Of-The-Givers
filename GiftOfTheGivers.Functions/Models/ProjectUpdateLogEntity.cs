using Azure;
using Azure.Data.Tables;

namespace GiftOfTheGivers.Functions.Models;

// One row in the Azure Table Storage "ProjectUpdateLog" table.
public class ProjectUpdateLogEntity : ITableEntity
{
    public const string DefaultPartitionKey = "ProjectUpdates";

    public string PartitionKey { get; set; } = DefaultPartitionKey;

    // Reverse-tick row keys make Table Storage return the newest log entries first.
    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public int ProjectUpdateId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PostedByUserId { get; set; } = string.Empty;

    public string PostedByName { get; set; } = string.Empty;

    public DateTime PostedOnUtc { get; set; }

    public DateTime LoggedOnUtc { get; set; }
}
