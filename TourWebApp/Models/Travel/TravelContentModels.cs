namespace TourWebApp.Models.Travel;

public class DestinationItem
{
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string Country { get; set; }
    public required string Category { get; set; }
    public required string Description { get; set; }
    public decimal PriceFrom { get; set; }
    public required string ImageUrl { get; set; }
}

public class TourPackageItem
{
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Duration { get; set; }
    public double Rating { get; set; }
    public decimal Price { get; set; }
    public required string Description { get; set; }
    public required string ImageUrl { get; set; }
}

public class FeatureItem
{
    public required string IconClass { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
}

public class GuidePostItem
{
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required string DateLabel { get; set; }
    public required string ImageUrl { get; set; }
}

public class TestimonialItem
{
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required string Quote { get; set; }
    public required string AvatarUrl { get; set; }
    public int Rating { get; set; }
}

public class TeamMemberItem
{
    public required string Name { get; set; }
    public required string Role { get; set; }
    public required string Bio { get; set; }
    public required string ImageUrl { get; set; }
}

public class StatItem
{
    public required string Value { get; set; }
    public required string Label { get; set; }
}
