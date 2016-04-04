namespace WpfGlowWindow
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            GlowWindow w = new GlowWindow();
            w.Show();
        }
    }
}
