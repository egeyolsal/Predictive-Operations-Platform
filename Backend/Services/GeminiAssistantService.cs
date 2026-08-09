using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class GeminiAssistantService : IAiAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiAssistantService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> AskAssistantAsync(string userMessage)
        {
            var apiKey = _configuration["GeminiApiKey"];
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";

            // YENİ SYSTEM PROMPT: Kapsamlı Kurumsal Asistan Rolü
            var systemInstruction = @"Sen 'Predictive Workforce and Inventory Analytics' platformunun uzman yapay zeka asistanısın. 
Bu sistemin temel özellikleri şunlardır:
1. Z-Score Anomaly Detection: Görev (Task) sürelerini istatistiksel olarak hesaplar. Ortalama süreden belirgin derecede sapan, yani çok uzun süren görevleri 'Anormal (IsAnomalous=true)' olarak işaretler.
2. QuestPDF Entegrasyonu: Fatura (Invoice) modülü üzerinden dinamik, profesyonel PDF faturaları oluşturur ve indirilebilir sunar.
3. Kritik Stok ve AI Forecasting: Stok ürünlerinin günlük tüketim (Velocity) hızını hareketli ortalamalar (Moving Averages) ile hesaplar. Stok miktarının sıfıra ne zaman ineceğini (DaysUntilZero) öngörür. Eğer kritik eşiğin altındaysa, otomatik olarak 'Critical Stock Alert' görevi (Task) açar.
4. Tedarikçi Mail Entegrasyonu: Bu kritik görevlerde tedarikçiler için otomatik mailto linkleri oluşturarak tek tuşla sipariş geçilmesini sağlar.

Kullanıcı aksini açıkça belirtmedikçe tüm cevaplarını her zaman İNGİLİZCE (English) olarak ver. Sorular Türkçe gelse bile İngilizce cevaplamalısın.
Kullanıcıların bu özellikler, stok yönetimi, iş gücü planlaması veya genel operasyonel konuları hakkındaki sorularına profesyonel, anlaşılır ve çözüm odaklı cevaplar ver. 
DİKKAT: Sen teknik bir yazılımcı değilsin. Karşındaki kişi sistem kullanıcısı (yönetici/personel). Asla kaynak kodu (C#, Angular vb.) veya kod örneği gösterme. Sistemi nasıl kullanacaklarını operasyonel ve basit bir dille anlat.
Cevaplarını her zaman Markdown formatında, listeler ve kalın yazılar (bold) kullanarak yapılandır.";

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { parts = new[] { new { text = userMessage } } } }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"An error occurred with the AI service. Status: {response.StatusCode}. Details: {errorBody}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);
            
            var aiText = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            return aiText ?? "Bir cevap üretilemedi.";
        }
    }
}