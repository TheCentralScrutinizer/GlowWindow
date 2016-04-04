using System;
using System.Windows.Interop;

namespace WpfGlowWindow.Extensions
{
    public static class WindowExt
    {
        public static IntPtr GetHandle(this System.Windows.Window window)
        {
            var helper = new WindowInteropHelper(window);
            return helper.Handle;
        }
    }
}
