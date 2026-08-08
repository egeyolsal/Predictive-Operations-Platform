using TaskInventoryApi.Models;

namespace TaskInventoryApi.Services;

public interface ITaskAnomalyService
{
    Task<bool> EvaluateTaskAnomalyAsync(TaskItem currentTask);
}
