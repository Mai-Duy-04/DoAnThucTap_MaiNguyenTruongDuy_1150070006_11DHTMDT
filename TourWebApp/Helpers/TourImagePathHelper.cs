namespace TourWebApp.Helpers;

public static class TourImagePathHelper
{
    private const string TourImageWebFolder = "/img/tours/";

    public static string NormalizeTourImagePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return string.Empty;

        var path = imagePath.Trim().Replace('\\', '/');

        var webRootMarker = "/wwwroot/img/tours/";
        var webRootIndex = path.IndexOf(webRootMarker, StringComparison.OrdinalIgnoreCase);
        if (webRootIndex >= 0)
            path = path[(webRootIndex + webRootMarker.Length)..];

        if (path.StartsWith("~", StringComparison.Ordinal))
            path = path[1..];

        path = path.TrimStart('/');

        if (path.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
            path = path["wwwroot/".Length..];

        if (path.StartsWith("img/tours/", StringComparison.OrdinalIgnoreCase))
            path = path["img/tours/".Length..];

        return path.TrimStart('/');
    }

    public static string ToTourImageSrc(string? imagePath, string fallback = "vungtau.jpg")
    {
        if (!string.IsNullOrWhiteSpace(imagePath)
            && (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return imagePath;
        }

        var normalized = NormalizeTourImagePath(imagePath);
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = fallback;

        return TourImageWebFolder + normalized;
    }
}
