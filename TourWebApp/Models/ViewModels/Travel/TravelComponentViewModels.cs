namespace TourWebApp.Models.ViewModels.Travel;

public class SectionHeadingModel
{
    public string Kicker { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AlignClass { get; set; } = "text-center";
}

public class HeroModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string PrimaryButtonText { get; set; } = string.Empty;
    public string PrimaryButtonUrl { get; set; } = string.Empty;
    public string SecondaryButtonText { get; set; } = string.Empty;
    public string SecondaryButtonUrl { get; set; } = string.Empty;
    public string BackgroundImageUrl { get; set; } = string.Empty;
}

public class CtaSectionModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string PrimaryButtonText { get; set; } = string.Empty;
    public string PrimaryButtonUrl { get; set; } = string.Empty;
    public string SecondaryButtonText { get; set; } = string.Empty;
    public string SecondaryButtonUrl { get; set; } = string.Empty;
    public string BackgroundImageUrl { get; set; } = string.Empty;
}
