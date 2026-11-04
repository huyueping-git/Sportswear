namespace Sportswear;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "STRIDE");
            args.Handled = true;
        };

        try
        {
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show("启动失败。\n" + ex.Message, "STRIDE");
            Shutdown(1);
        }
    }
}
