using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace Shared_Data
{
    public static class BitmapLibrary
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        public static Size GetMonitorSize()
        {
            SetProcessDPIAware();
            nint hwnd = Process.GetCurrentProcess().MainWindowHandle;
            using Graphics g = Graphics.FromHwnd(hwnd);
            return new Size((int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height);
        }

        public static void SaveScreenshot(string path, ImageFormat format, Size size = default) => TakeScreenshot(size: size).Save(path, format);


        public static Bitmap TakeScreenshot(Point xPoint = default, Point yPoint = default, Size size = default)
        {
            Size screenshotSize = size == default ? GetMonitorSize() : size;
            Bitmap bmp = new(screenshotSize.Width, screenshotSize.Height);
            using Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(xPoint, yPoint, bmp.Size);
            return bmp;
        }
    }
}