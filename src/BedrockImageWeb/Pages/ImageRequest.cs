namespace BedrockImageWeb.Pages;

public class ImageRequest(string prompt, int seed, string stylePreset)
{
    public string Prompt { get; set; } = prompt;
    public int Seed { get; set; } = seed;
    public string StylePreset { get; set; } = stylePreset;
}