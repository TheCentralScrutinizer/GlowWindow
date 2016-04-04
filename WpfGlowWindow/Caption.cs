using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfGlowWindow
{
    internal class Caption : Control
    {
        #region private

        private Grid _layoutRoot;

        #endregion

        public event MouseButtonEventHandler CaptionMouseDown;
        public event MouseButtonEventHandler CaptionMouseUp;
        public event MouseEventHandler CaptionMouseMove;

        #region Constructors

        static Caption()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (Caption),
                new FrameworkPropertyMetadata(typeof (Caption)));
        }

        public Caption()
        {
            DefaultStyleKey = typeof (Caption);
        }

        #endregion

        #region OnApplyTemplate

        public override void OnApplyTemplate()
        {
            Cleanup();

            base.OnApplyTemplate();

            _layoutRoot = GetTemplateChild("LayoutRoot") as Grid;

            Setup();
        }

        private void Setup()
        {
            if (_layoutRoot != null)
            {
                _layoutRoot.MouseDown += HandleMouseDown;
                _layoutRoot.MouseUp += HandleMouseUp;
                _layoutRoot.MouseMove += HandleMouseMove;
            }
        }

        private void Cleanup()
        {
            if (_layoutRoot != null)
            {
                _layoutRoot.MouseDown -= HandleMouseDown;
                _layoutRoot.MouseUp -= HandleMouseUp;
                _layoutRoot.MouseMove -= HandleMouseMove;
            }
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CaptionMouseDown != null)
            {
                CaptionMouseDown(this, e);
            }
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (CaptionMouseMove != null)
            {
                CaptionMouseMove(this, e);
            }
        }

        private void HandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CaptionMouseUp != null)
            {
                CaptionMouseUp(this, e);
            }
        }

        #endregion

        #region Title (type: string)

        /// <summary>
        /// Identifies the Title dependency property.
        /// </summary>
        public static DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof (string),
            typeof (Caption),
            null);

        /// <summary>
        /// Title backing field
        /// </summary>
        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        #endregion

        #region CmdClose (type: DelegateCommand)

        /// <summary>
        /// Identifies the CmdClose dependency property.
        /// </summary>
        public static DependencyProperty CmdCloseProperty = DependencyProperty.Register(
            "CmdClose",
            typeof (DelegateCommand),
            typeof (Caption),
            null);

        /// <summary>
        /// CmdClose backing field
        /// </summary>
        public DelegateCommand CmdClose
        {
            get { return (DelegateCommand) GetValue(CmdCloseProperty); }
            set { SetValue(CmdCloseProperty, value); }
        }

        #endregion
    }

}
