namespace Backend.Services
{
    public interface IAiAssistantService
    {
        // Artık dışarıdan kullanıcının yazdığı serbest metni (userMessage) alacak
        Task<string> AskAssistantAsync(string userMessage);
    }
}