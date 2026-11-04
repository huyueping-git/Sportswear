namespace Sportswear.Views;

public partial class AlertWindow : SportWindow
{
    public AlertWindow(string title, string message, string okText, bool showCancel, bool danger)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        OkButton.Content = okText;
        CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
        if (danger)
            OkButton.Style = (Style)FindResource("SportDangerFill");
    }

    public static bool Confirm(Window owner, string title, string message, string okText = "确定")
    {
        var dialog = new AlertWindow(title, message, okText, showCancel: true, danger: true) { Owner = owner };
        return dialog.ShowDialog() == true;
    }

    public static void Info(Window owner, string title, string message)
    {
        var dialog = new AlertWindow(title, message, "知道了", showCancel: false, danger: false) { Owner = owner };
        dialog.ShowDialog();
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
