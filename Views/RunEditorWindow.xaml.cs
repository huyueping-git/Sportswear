using Sportswear.Models;

namespace Sportswear.Views;

public partial class RunEditorWindow : SportWindow
{
    private readonly long _id;

    public RunEditorWindow(IReadOnlyList<Shoe> shoes, long? preferredShoeId, RunEntry? existing = null)
    {
        InitializeComponent();
        _id = existing?.Id ?? 0;
        TitleText.Text = existing == null ? "记录里程" : "修改记录";
        Title = TitleText.Text;

        ShoeBox.ItemsSource = shoes;
        var shoeId = existing?.ShoeId ?? preferredShoeId;
        ShoeBox.SelectedItem = shoes.FirstOrDefault(shoe => shoe.Id == shoeId) ?? shoes.FirstOrDefault();

        DateBox.Text = (existing?.RunDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (existing != null)
        {
            DistanceBox.Text = Km.Precise(existing.DistanceKm);
            TimeBox.Text = existing.StartTime;
            WeatherBox.Text = existing.Weather;
            TemperatureBox.Text = existing.Temperature;
            NotesBox.Text = existing.Notes;
        }
    }

    public RunEntry? Result { get; private set; }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        DistanceBox.Focus();
        DistanceBox.SelectAll();
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string distance })
            return;

        DistanceBox.Text = distance;
        DistanceBox.Focus();
        DistanceBox.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ShoeBox.SelectedItem is not Shoe shoe)
        {
            ShowError("请选择一双跑鞋。");
            return;
        }

        if (!Dates.TryParse(DateBox.Text, out var date))
        {
            ShowError("日期请用 2026-11-04 这种格式。");
            DateBox.Focus();
            return;
        }

        if (date.Date > DateTime.Today)
        {
            ShowError("日期不能晚于今天。");
            DateBox.Focus();
            return;
        }

        var startTime = "";
        if (!string.IsNullOrWhiteSpace(TimeBox.Text))
        {
            if (!Clock.TryParse(TimeBox.Text, out startTime))
            {
                ShowError("开始时间请用 06:30 这种格式，或留空。");
                TimeBox.Focus();
                return;
            }
        }

        if (!Km.TryParse(DistanceBox.Text, out var distance) || distance <= 0)
        {
            ShowError("里程需要大于 0。");
            DistanceBox.Focus();
            return;
        }

        if (distance > 500)
        {
            ShowError("单次最多记录 500 公里。更早的累计请填在跑鞋资料的「记录前已跑」。");
            DistanceBox.Focus();
            return;
        }

        Result = new RunEntry
        {
            Id = _id,
            ShoeId = shoe.Id,
            RunDate = date.Date,
            DistanceKm = distance,
            StartTime = startTime,
            Weather = WeatherBox.Text.Trim(),
            Temperature = TemperatureBox.Text.Trim(),
            Notes = NotesBox.Text.Trim()
        };
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
