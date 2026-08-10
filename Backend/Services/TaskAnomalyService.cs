using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Services;

public class TaskAnomalyService : ITaskAnomalyService
{
    private readonly IUnitOfWork _unitOfWork;
    private const double ZScoreThreshold = 2.0;

    public TaskAnomalyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> EvaluateTaskAnomalyAsync(TaskItem currentTask)
    {
        // 1. Validations
        if (currentTask.Status != TaskItemStatus.Done || currentTask.CompletedAt == null)
        {
            return false;
        }

        double currentDuration = (currentTask.CompletedAt.Value - currentTask.CreatedAt).TotalHours;
        if (currentDuration < 0) return false;

        // 2. Fetch past completed tasks in the same category
        var allPastTasks = await _unitOfWork.TaskItems.FindAsync(t => 
            t.CategoryId == currentTask.CategoryId &&
            t.Status == TaskItemStatus.Done && 
            t.CompletedAt != null &&
            !t.IsSystemGenerated);

        // 3. Leave-one-out masking (exclude the current task)
        var pastTasks = allPastTasks.Where(t => t.Id != currentTask.Id).ToList();

        if (pastTasks.Count < 2)
        {
            // Not enough data to calculate standard deviation safely
            return false;
        }

        // 4. Calculate Mean
        double sumDurations = pastTasks.Sum(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours);
        double mean = sumDurations / pastTasks.Count;

        // 5. Calculate Standard Deviation (Sample variance)
        double sumOfSquares = pastTasks.Sum(t => Math.Pow(((t.CompletedAt!.Value - t.CreatedAt).TotalHours) - mean, 2));
        double variance = sumOfSquares / (pastTasks.Count - 1);
        double stdDev = Math.Sqrt(variance);

        // Prevent division by zero or near-zero standard deviation
        if (stdDev < 0.001)
        {
            return false;
        }

        // 6. Calculate Z-Score
        double zScore = (currentDuration - mean) / stdDev;

        // 7. Check against threshold
        return zScore > ZScoreThreshold;
    }
}
