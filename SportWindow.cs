using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Sportswear;

public class SportWindow : Window
{
    protected SportWindow()
    {
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/logo.png", UriKind.Absolute));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyDarkTitle(this);
    }

    internal static void ApplyDarkTitle(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int dark = 1;
        _ = DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));

        int caption = 0x00120F0C;
        int text = 0x00F8F7F4;
        _ = DwmSetWindowAttribute(hwnd, 35, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, 36, ref text, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
