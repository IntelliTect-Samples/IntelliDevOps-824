using System.Net.Http.Json;

namespace BedrockImageWeb.Pages;

public partial class Home
{
    private string? Text { get; set; }

    private string? Image { get; set; } = "./icon-192.png";

    private async Task GenerateImage()
    {
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync("api/image", new { Text });
        Image = await response.Content.ReadAsStringAsync();
    }
}
