using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CoffeeNChill.Gateway.Services;

public class AzureFunctionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _functionBaseUrl;
    private readonly ILogger<AzureFunctionService> _logger;

    public AzureFunctionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureFunctionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _functionBaseUrl = configuration["AzureFunctions:BaseUrl"] ?? "http://localhost:7071";
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string endpoint, HttpMethod method, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_functionBaseUrl}{endpoint}");

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            _logger.LogInformation("Sending to {Endpoint}: {Json}", endpoint, json);

            var stringContent = new StringContent(json, Encoding.UTF8);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            stringContent.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);

            request.Content = stringContent;
        }

        var response = await _httpClient.SendAsync(request);
        return response;
    }

    private async Task<object?> ParseResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            return JsonSerializer.Deserialize<object>(content);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Non-JSON response from Functions app: {Content}", content);
            return new { raw = content };
        }
    }

    // ===== MENU =====
    public async Task<object?> GetMenuItemsAsync()
    {
        var response = await SendRequestAsync("/api/menu", HttpMethod.Get);
        return await ParseResponseAsync(response);
    }

    public async Task<object?> GetMenuItemsByCategoryAsync(string category)
    {
        var response = await SendRequestAsync($"/api/menu/category/{Uri.EscapeDataString(category)}", HttpMethod.Get);
        return await ParseResponseAsync(response);
    }

    public async Task<object?> CreateMenuItemAsync(object menuItem)
    {
        var response = await SendRequestAsync("/api/menu", HttpMethod.Post, menuItem);
        return await ParseResponseAsync(response);
    }

    public async Task<object?> UpdateMenuItemAsync(string category, string id, object updateData)
    {
        var response = await SendRequestAsync($"/api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", HttpMethod.Put, updateData);
        if (!response.IsSuccessStatusCode)
            return null;
        return await ParseResponseAsync(response);
    }

    public async Task<bool> DeleteMenuItemAsync(string category, string id)
    {
        var response = await SendRequestAsync($"/api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", HttpMethod.Delete);
        return response.IsSuccessStatusCode;
    }

    // ===== ORDERS =====
    public async Task<object?> PlaceOrderAsync(object order)
    {
        var response = await SendRequestAsync("/api/orders/queue", HttpMethod.Post, order);
        return await ParseResponseAsync(response);
    }

    public async Task<object?> GetAllOrdersAsync()
    {
        var response = await SendRequestAsync("/api/orders", HttpMethod.Get);
        return await ParseResponseAsync(response);
    }

    // ===== DOCUMENTS =====
    public async Task<object?> ListDocumentsAsync()
    {
        var response = await SendRequestAsync("/api/documents", HttpMethod.Get);
        return await ParseResponseAsync(response);
    }

    public async Task<object?> UploadDocumentAsync(Stream fileStream, string fileName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_functionBaseUrl}/api/documents/upload");
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        return await ParseResponseAsync(response);
    }

    public async Task<(Stream? stream, string? fileName)> DownloadDocumentAsync(string fileName)
    {
        var response = await _httpClient.GetAsync($"{_functionBaseUrl}/api/documents/download/{Uri.EscapeDataString(fileName)}");
        if (!response.IsSuccessStatusCode)
            return (null, null);

        var stream = await response.Content.ReadAsStreamAsync();
        return (stream, fileName);
    }
}