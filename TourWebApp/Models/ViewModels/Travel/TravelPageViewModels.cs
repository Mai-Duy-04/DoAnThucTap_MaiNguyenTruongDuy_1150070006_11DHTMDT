using System.ComponentModel.DataAnnotations;
using TourWebApp.Models.Travel;

namespace TourWebApp.Models.ViewModels.Travel;

public class ContactFormInput
{
    [Required(ErrorMessage = "Please enter your name.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your message.")]
    public string Message { get; set; } = string.Empty;
}

public class TravelHomeViewModel
{
    public required IReadOnlyList<FeatureItem> WhyTravelWithUs { get; set; }
    public required IReadOnlyList<DestinationItem> FeaturedDestinations { get; set; }
    public required IReadOnlyList<TourPackageItem> PopularTours { get; set; }
    public required IReadOnlyList<GuidePostItem> GuidePosts { get; set; }
    public required IReadOnlyList<TestimonialItem> Testimonials { get; set; }
}

public class TravelDestinationsViewModel
{
    public required IReadOnlyList<string> Filters { get; set; }
    public required IReadOnlyList<DestinationItem> Items { get; set; }
    public required string SelectedFilter { get; set; }
}

public class TravelToursViewModel
{
    public required IReadOnlyList<string> Filters { get; set; }
    public required IReadOnlyList<TourPackageItem> Items { get; set; }
    public required string SelectedFilter { get; set; }
}

public class TravelDestinationDetailViewModel
{
    public required DestinationItem Item { get; set; }
    public required IReadOnlyList<DestinationItem> RelatedDestinations { get; set; }
}

public class TravelTourDetailViewModel
{
    public required TourPackageItem Item { get; set; }
    public required IReadOnlyList<TourPackageItem> RelatedTours { get; set; }
}

public class TravelAboutViewModel
{
    public required IReadOnlyList<TeamMemberItem> TeamMembers { get; set; }
    public required IReadOnlyList<StatItem> Stats { get; set; }
}

public class TravelContactViewModel
{
    public ContactFormInput Form { get; set; } = new();
    public bool IsSubmitted { get; set; }
    public string SuccessMessage { get; set; } = string.Empty;
}
