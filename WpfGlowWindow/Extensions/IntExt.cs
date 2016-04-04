using WpfGlowWindow.Import;

namespace WpfGlowWindow.Extensions
{
    public static class IntExt
    {
        public static bool Is(this int i, WindowsMessages msg)
        {
            int wm = (int) msg;
            return (i == wm);
        }

        public static bool Is(this uint i, WindowsMessages msg)
        {
            uint wm = (uint) msg;
            return (i == wm);
        }
    }
}
