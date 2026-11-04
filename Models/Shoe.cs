namespace Sportswear.Models;

public sealed class Shoe
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string Color { get; set; } = "";
    public DateTime? PurchaseDate { get; set; }
    public double InitialKm { get; set; }
    public double RetireKm { get; set; } = 800;
    public string Notes { get; set; } = "";
    public bool IsRetired { get; set; }
    public double LoggedKm { get; set; }
    public int RunCount { get; set; }

    public double TotalKm => InitialKm + LoggedKm;

    public double ProgressRatio => RetireKm <= 0 ? 0 : TotalKm / RetireKm;

    public double BarValue => Math.Min(ProgressRatio, 1);

    public string Status
    {
        get
        {
            if (IsRetired)
                return "retired";
            if (ProgressRatio >= 1)
                return "danger";
            if (ProgressRatio >= 0.8)
                return "warn";
            return "good";
        }
    }

    public string StatusText => Status switch
    {
        "retired" => "已退役",
        "danger" => "建议更换",
        "warn" => "接近寿命",
        _ => "状态良好"
    };

    public string BrandText => string.IsNullOrWhiteSpace(Brand) ? "RUNNING" : Brand.ToUpper(Invariant);

    public string MetaText
    {
        get
        {
            var parts = new[] { Model, Color }.Where(part => !string.IsNullOrWhiteSpace(part));
            var meta = string.Join("  ·  ", parts);
            return string.IsNullOrWhiteSpace(meta) ? "未填写型号" : meta;
        }
    }

    public string MileageText => Km.Text(TotalKm);

    public string PercentText => (ProgressRatio * 100).ToString("0", Invariant) + "%";

    public string RemainingText
    {
        get
        {
            if (IsRetired)
                return "已退役";

            var diff = RetireKm - TotalKm;
            return diff >= 0
                ? $"还可跑 {Km.Text(diff)} km"
                : $"已超出 {Km.Text(-diff)} km";
        }
    }

    public string BreakdownText =>
        InitialKm > 0
            ? $"记录前 {Km.Text(InitialKm)} km  ·  {RunCount} 次记录 {Km.Text(LoggedKm)} km"
            : $"{RunCount} 次记录";

    public string PurchaseText =>
        PurchaseDate is null
            ? "购买日期未填"
            : "购于 " + PurchaseDate.Value.ToString("yyyy-MM-dd", Invariant);

    public string RetireText => $"建议在 {Km.Text(RetireKm)} km 时更换";

    public string PickerLabel
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(Brand) ? Name : $"{Name} · {Brand}";
            return IsRetired ? label + "（已退役）" : label;
        }
    }

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public string RetireButtonText => IsRetired ? "恢复使用" : "标记退役";
}
