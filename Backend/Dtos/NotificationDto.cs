namespace TaskInventoryApi.Dtos;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "task", "inventory", etc.
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
