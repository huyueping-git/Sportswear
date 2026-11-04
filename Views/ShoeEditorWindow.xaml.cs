using Sportswear.Models;

namespace Sportswear.Views;

public partial class ShoeEditorWindow : SportWindow
{
    private readonly long _id;

    public ShoeEditorWindow(Shoe? existing = null)
    {
        InitializeComponent();
        _id = existing?.Id ?? 0;
        TitleText.Text = existing == null ? "添加跑鞋" : "编辑跑鞋";
        Title = TitleText.Text;

        if (existing == null)
        {
            InitialBox.Text = "0";
            RetireBox.Text = "800";
        }
        else
        {
            NameBox.Text = existing.Name;
            BrandBox.Text = existing.Brand;
            ModelBox.Text = existing.Model;
            ColorBox.Text = existing.Color;
            PurchaseBox.Text = existing.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            InitialBox.Text = Km.Text(existing.InitialKm);
            RetireBox.Text = Km.Text(existing.RetireKm);
            NotesBox.Text = existing.Notes;
            RetiredBox.IsChecked = existing.IsRetired;
        }
    }

    public Shoe? Result { get; private set; }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("请填写跑鞋名称，方便区分不同的鞋。");
            NameBox.Focus();
            return;
        }

        if (!Km.TryParse(InitialBox.Text, out var initial) || initial < 0 || initial > 100000)
        {
            ShowError("记录前已跑需要是 0 到 100000 之间的公里数。");
            InitialBox.Focus();
            return;
        }

        if (!Km.TryParse(RetireBox.Text, out var retire) || retire <= 0 || retire > 100000)
        {
            ShowError("建议更换里程需要大于 0，且不超过 100000 公里。");
            RetireBox.Focus();
            return;
        }

        DateTime? purchase = null;
        if (!string.IsNullOrWhiteSpace(PurchaseBox.Text))
        {
            if (!Dates.TryParse(PurchaseBox.Text, out var parsed))
            {
                ShowError("购买日期请用 2026-11-04 这种格式，或留空。");
                PurchaseBox.Focus();
                return;
            }

            purchase = parsed;
        }

        Result = new Shoe
        {
            Id = _id,
            Name = name,
            Brand = BrandBox.Text.Trim(),
            Model = ModelBox.Text.Trim(),
            Color = ColorBox.Text.Trim(),
            PurchaseDate = purchase,
            InitialKm = initial,
            RetireKm = retire,
            Notes = NotesBox.Text.Trim(),
            IsRetired = RetiredBox.IsChecked == true
        };
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
