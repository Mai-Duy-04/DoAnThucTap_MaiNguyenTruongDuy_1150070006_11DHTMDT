namespace TourWebApp.Data.Models;

public static class TourPriceAudience
{
    public const string Adult = "NguoiLon";
    public const string Child = "TreEm";
    public const string Infant = "TreNho";

    private const string LegacyAdult = "Người lớn";
    private const string LegacyChild = "Trẻ em";
    private const string LegacyInfant = "Em bé";

    public static bool IsAdult(string? value) => Matches(value, Adult, LegacyAdult);

    public static bool IsChild(string? value) => Matches(value, Child, LegacyChild);

    public static bool IsInfant(string? value) => Matches(value, Infant, LegacyInfant);

    public static string GetDisplayName(string? value)
    {
        if (IsAdult(value)) return LegacyAdult;
        if (IsChild(value)) return LegacyChild;
        if (IsInfant(value)) return LegacyInfant;
        return value ?? string.Empty;
    }

    private static bool Matches(string? value, string code, string legacyValue) =>
        string.Equals(value, code, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, legacyValue, StringComparison.OrdinalIgnoreCase);
}
