namespace GiftOfTheGivers.Functions.Models;

// Project update posted by an employee on the web app dashboard.
public class ProjectUpdateLogRequest
{
    public int ProjectUpdateId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? PostedByUserId { get; set; }

    public string? PostedByName { get; set; }

    public DateTime? PostedOnUtc { get; set; }
}
