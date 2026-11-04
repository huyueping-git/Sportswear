namespace Sportswear.Models;

public sealed class RunEntry
{
    private static readonly CultureInfo Zh = new("zh-CN");

    public long Id { get; set; }
    public long ShoeId { get; set; }
    public string ShoeName { get; set; } = "";
    public DateTime RunDate { get; set; }
    public double DistanceKm { get; set; }
    public string Notes { get; set; } = "";
    public string Weather { get; set; } = "";
    public string Temperature { get; set; } = "";
    public string StartTime { get; set; } = "";

    public string DateText =>
        RunDate.Year == DateTime.Today.Year
            ? RunDate.ToString("MM-dd", CultureInfo.InvariantCulture)
            : RunDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string WeekdayText => RunDate.ToString("ddd", Zh);

    public string DistanceText => Km.Precise(DistanceKm);

    public string NotesText => string.IsNullOrWhiteSpace(Notes) ? "跑步" : Notes;

    public string TemperatureText
    {
        get
        {
            var value = Temperature.Trim();
            if (value.Length == 0)
                return "";

            return value.Contains('℃') || value.Contains('°') ? value : value + "℃";
        }
    }

    public string LogTitle => string.IsNullOrWhiteSpace(ShoeName) ? "跑步" : ShoeName;

    public string LogSub
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(StartTime))
                parts.Add(StartTime);
            if (!string.IsNullOrWhiteSpace(Weather))
                parts.Add(Weather.Trim());
            if (!string.IsNullOrWhiteSpace(Temperature))
                parts.Add(TemperatureText);
            if (!string.IsNullOrWhiteSpace(Notes))
                parts.Add(Notes.Trim());
            return string.Join("  ·  ", parts);
        }
    }

    public bool HasLogSub => LogSub.Length > 0;

    public string ShoeRunMeta
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(StartTime))
                parts.Add(StartTime);
            if (!string.IsNullOrWhiteSpace(Weather))
                parts.Add(Weather.Trim());
            if (!string.IsNullOrWhiteSpace(Temperature))
                parts.Add(TemperatureText);
            if (!string.IsNullOrWhiteSpace(Notes))
                parts.Add(Notes.Trim());
            return parts.Count == 0 ? "跑步" : string.Join("  ·  ", parts);
        }
    }
}
