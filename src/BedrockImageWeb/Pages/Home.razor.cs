using System.Net.Http.Json;

namespace BedrockImageWeb.Pages;

public partial class Home
{
    private string? Description { get; set; }

    private string? Image { get; set; } = "./icon-192.png";

    private async Task GenerateImage()
    {
        // Example API gateway call https://{api-id}.execute-api.region.amazonaws.com/DEV/image
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync("api/image", new { Description });
        Image = await response.Content.ReadAsStringAsync();
    }
}
