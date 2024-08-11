using System.Net.Http.Json;

namespace BedrockImageWeb.Pages;

public partial class Home
{
    private string Description { get; set; } = "A picture with Abe Lincoln riding a turtle.";
    private string Image { get; set; } = "./icon-192.png";
    private string StylePreset { get; set; } = "3d-model";

    private async Task GenerateImage()
    {
        try
        {
            var httpClient = new HttpClient(); 
            var response = await httpClient.PostAsJsonAsync(
                "https://{api-id}.execute-api.region.amazonaws.com/DEV/image",
                new ImageRequest(Description, 0, StylePreset));
            Image = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
