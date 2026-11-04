namespace Sportswear;

internal static class Km
{
    public static string Text(double km) =>
        km.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Precise(double km) =>
        km.ToString("0.####", CultureInfo.InvariantCulture);

    public static string Board(double km) =>
        km.ToString("#,##0.##", CultureInfo.InvariantCulture);

    public static bool TryParse(string? text, out double km)
    {
        km = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim().Replace('，', '.').Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out km)
               && double.IsFinite(km);
    }
}

internal static class Dates
{
    private static readonly string[] Formats = ["yyyy-MM-dd", "yyyy/M/d", "yyyy.M.d", "yyyy-M-d"];

    public static bool TryParse(string? text, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return DateTime.TryParseExact(
            text.Trim(),
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    public static string ToStorage(DateTime date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

internal static class Clock
{
    private static readonly string[] Formats = ["h\\:mm", "hh\\:mm"];

    public static bool TryParse(string? text, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim().Replace('：', ':');
        if (!TimeSpan.TryParseExact(value, Formats, CultureInfo.InvariantCulture, out var time))
            return false;

        if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            return false;

        normalized = $"{(int)time.TotalHours:00}:{time.Minutes:00}";
        return true;
    }
}
