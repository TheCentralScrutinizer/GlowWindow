using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WpfGlowWindow.Extensions;
using WpfGlowWindow.Glow;
using WpfGlowWindow.Import;

namespace WpfGlowWindow
{
    public class GlowWindow : Window
    {
        #region private

        private IntPtr _handle;
        private GlowDecorator _glowDecorator;
        private Grid _rootGrid;
        private Border _border;
        private object _userContent;
        private ContentPresenter _internalPresenter;
        private Caption _caption;

        private bool _replacing;

        private readonly SolidColorBrush _activeBrush;
        private readonly SolidColorBrush _inactiveBrush;

        private bool _mouseDown;
        private Point _mouseDownPoint = new Point(0, 0);

        private readonly DelegateCommand _close;

        #endregion

        public GlowWindow()
        {
            MinWidth = 200;
            MinHeight = 100;
            Width = 480;
            Height = 320;

            _close = new DelegateCommand(CloseWindow);
            _activeBrush = new SolidColorBrush(Colors.DodgerBlue);
            _inactiveBrush = new SolidColorBrush(Colors.DarkGray);

            BuildWindow();

            SetCaption("Glow Window");
        }

        private void CloseWindow(object obj)
        {
            Close();
        }

        public void SetCaption(string caption)
        {
            _caption.Title = caption;
        }

        #region Content management

        public new object Content
        {
            get
            {
                return _userContent;
            }
            set
            {
                _userContent = value;
                UpdateContent();
            }
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (_replacing)
            {
                return;
            }

            _userContent = newContent;
            UpdateContent();
        }

        private void UpdateContent()
        {
            if (_replacing)
            {
                return;
            }

            _replacing = true;
            if (!Equals(base.Content, _border))
            {
                base.Content = _border;
            }

            _internalPresenter.Content = _userContent;
            _replacing = false;
        }

        #endregion

        #region Initialization

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateContent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _handle = this.GetHandle();
            HwndSource hwndSource = HwndSource.FromHwnd(_handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }

            RemoveNonClientArea(_handle);
            ApplyGlowDecorator();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case (int) WindowsMessages.WM_ACTIVATE:
                    int action = ((int) wParam) & 0xF;
                    bool isActive = action == 1 || action == 2;

                    _border.BorderBrush = (isActive ? _activeBrush : _inactiveBrush);
                    _caption.Background = _border.BorderBrush;
                    break;
                case (int)WindowsMessages.WM_SIZING:
                    // Actually not needed: you can use MinWidth/MinHeight,
                    // but in case you could use this code to force min width/height
                    // Resizing(wParam, lParam);
                    break;
                case (int)WindowsMessages.WM_EXITSIZEMOVE:
                    Cursor = Cursors.Arrow;
                    OnWmExitSizeMove();
                    break;
            }

            return IntPtr.Zero;
        }

        private void RemoveNonClientArea(IntPtr handle)
        {
            uint styles = User32.GetWindowLong(handle, GetWindowLongFlags.GWL_STYLE);
            styles &= ~((uint)(WindowStyles.WS_CAPTION | WindowStyles.WS_SIZEBOX));
            User32.SetWindowLong(handle, GetWindowLongFlags.GWL_STYLE, styles);
        }

        private void ApplyGlowDecorator()
        {
            _glowDecorator = new GlowDecorator();
            _glowDecorator.Attach(this);

            _glowDecorator.ActiveColor = _activeBrush.Color;
            _glowDecorator.InactiveColor = _inactiveBrush.Color;
            _glowDecorator.Activate(true);
            _glowDecorator.EnableResize(true);
        }

        #endregion

        #region private

        private void BuildWindow()
        {
            _border = new Border
            {
                BorderThickness = new Thickness(1.0)
            };
            _rootGrid = new Grid();
            _border.Child = _rootGrid;

            _rootGrid.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto});
            _rootGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(1.0, GridUnitType.Star)});

            _caption = new Caption();
            _caption.CaptionMouseDown += HandleCaptionMouseDown;
            _caption.CaptionMouseUp += HandleCaptionMouseUp;
            _caption.CaptionMouseMove += HandleCaptionMouseMove;
            _caption.Background = _inactiveBrush;
            _caption.CmdClose = _close;
            Grid.SetRow(_caption, 0);
            _rootGrid.Children.Add(_caption);

            _internalPresenter = new ContentPresenter();
            Grid.SetRow(_internalPresenter, 1);
            _rootGrid.Children.Add(_internalPresenter);
        }

        private void HandleCaptionMouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(this);
            Point diff = new Point(point.X - _mouseDownPoint.X, point.Y - _mouseDownPoint.Y);
            if (WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed && (Math.Abs(diff.X) > 4 || Math.Abs(diff.Y) > 4) && _mouseDown)
            {
                Point pos = point;
                Point absCoord = PointToScreen(pos);
                double perc = pos.X / ActualWidth;

                WindowState = WindowState.Normal;
                Left = absCoord.X - ActualWidth * perc;
                Top = absCoord.Y - pos.Y;
                DragMove();
            }
        }

        private void HandleCaptionMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPoint = new Point(0, 0);
            _mouseDown = false;
        }

        private void HandleCaptionMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 1)
                {
                    _mouseDownPoint = e.GetPosition(this);
                    _mouseDown = true;
                    if (WindowState != WindowState.Maximized)
                    {
                        DragMove();
                    }
                }
                else if (e.ClickCount == 2 &&
                          ResizeMode != ResizeMode.NoResize &&
                          ResizeMode != ResizeMode.CanMinimize)
                {
                    MaximizeExecute();
                    _mouseDown = false;
                }
            }
        }

        private void MaximizeExecute()
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
            }
        }

        private RECT? _sizingInitialRect;

        protected virtual void OnWmExitSizeMove()
        {
            _sizingInitialRect = null;
        }

        protected virtual void Resizing(IntPtr wParam, IntPtr lParam)
        {
            if (_sizingInitialRect == null)
            {
                RECT tmpRect = new RECT();
                User32.GetWindowRect(_handle, ref tmpRect);
                _sizingInitialRect = tmpRect;
            }

            RECT r = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
            if (r.right - r.left <= MinWidth)
            {
                if ((int)wParam == (int)WindowSizeEdges.WMSZ_LEFT ||
                (int)wParam == (int)WindowSizeEdges.WMSZ_TOPLEFT ||
                    (int)wParam == (int)WindowSizeEdges.WMSZ_BOTTOMLEFT)
                {
                    r.left = (int)(_sizingInitialRect.Value.right - MinWidth);
                }
                else
                {
                    r.right = (int)(r.left + MinWidth);
                }
            }

            if (r.bottom - r.top <= MinHeight)
            {
                if ((int)wParam == (int)WindowSizeEdges.WMSZ_TOP ||
                (int)wParam == (int)WindowSizeEdges.WMSZ_TOPLEFT ||
                    (int)wParam == (int)WindowSizeEdges.WMSZ_TOPRIGHT)
                {
                    r.top = (int)(_sizingInitialRect.Value.bottom - MinHeight);
                }
                else
                {
                    r.bottom = (int)(r.top + MinHeight);
                }
            }

            Marshal.StructureToPtr(r, lParam, false);
        }

        #endregion
    }
}
