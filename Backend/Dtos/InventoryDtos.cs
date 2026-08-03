namespace TaskInventoryApi.Dtos;

public class InventoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int CriticalThreshold { get; set; }
}

public class InventoryUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int CriticalThreshold { get; set; }
}

public class InventoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int CriticalThreshold { get; set; }
}