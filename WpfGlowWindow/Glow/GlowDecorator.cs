using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using WpfGlowWindow.Extensions;
using WpfGlowWindow.Import;

namespace WpfGlowWindow.Glow
{
    internal class GlowDecorator : IDisposable
    {
        #region [private]

        private IntPtr _parentWindowHndl;
        private Window _window;
        private SideGlow _topGlow;
        private SideGlow _leftGlow;
        private SideGlow _bottomGlow;
        private SideGlow _rightGlow;
        private readonly List<SideGlow> _glows = new List<SideGlow>();
        private Color _activeColor = Colors.Gray;
        private Color _inactiveColor = Colors.Gray;
        private bool _isAttached;
        private bool _isEnabled;
        private bool _setTopMost;

        #endregion

        #region [internal] API and Properties

        internal bool SetTopMost
        {
            get
            {
                return _setTopMost;
            }
            set
            {
                _setTopMost = value;
                AlignSideGlowTopMost();
            }
        }

        internal Color ActiveColor
        {
            get
            {
                return _activeColor;
            }

            set
            {
                _activeColor = value;
                foreach (SideGlow sideGlow in _glows)
                {
                    sideGlow.ActiveColor = _activeColor;
                }
            }
        }

        internal Color InactiveColor
        {
            get
            {
                return _inactiveColor;
            }

            set
            {
                _inactiveColor = value;
                foreach (SideGlow sideGlow in _glows)
                {
                    sideGlow.InactiveColor = _inactiveColor;
                }
            }
        }

        internal bool IsEnabled
        {
            get { return _isEnabled; }
        }

        internal void Attach(Window window, bool enable = true)
        {
            if (_isAttached)
            {
                return;
            }

            _isAttached = true;
            _window = window;
            _parentWindowHndl = window.GetHandle();

            _topGlow = new SideGlow(Dock.Top, _parentWindowHndl);
            _leftGlow = new SideGlow(Dock.Left, _parentWindowHndl);
            _bottomGlow = new SideGlow(Dock.Bottom, _parentWindowHndl);
            _rightGlow = new SideGlow(Dock.Right, _parentWindowHndl);

            _glows.Add(_topGlow);
            _glows.Add(_leftGlow);
            _glows.Add(_bottomGlow);
            _glows.Add(_rightGlow);

            User32.ShowWindow(_topGlow.Handle, ShowWindowStyles.SW_SHOWNOACTIVATE);
            User32.ShowWindow(_leftGlow.Handle, ShowWindowStyles.SW_SHOWNOACTIVATE);
            User32.ShowWindow(_bottomGlow.Handle, ShowWindowStyles.SW_SHOWNOACTIVATE);
            User32.ShowWindow(_rightGlow.Handle, ShowWindowStyles.SW_SHOWNOACTIVATE);

            _isEnabled = false;
            AlignSideGlowTopMost();
            Enable(true);

            HookOnce();
        }

        private bool _hooked;
        private void HookOnce()
        {
            if (_hooked) return;

            _hooked = true;
            HwndSource hwndSource = HwndSource.FromHwnd(_parentWindowHndl);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }
        }

        private WINDOWPOS _lastLocation;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!_isEnabled) return (IntPtr)0;

            switch (msg)
            {
                case (int)WindowsMessages.WM_WINDOWPOSCHANGED:
                    _lastLocation = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    WindowPosChanged(_lastLocation);
                    break;
                case (int)WindowsMessages.WM_SETFOCUS:
                    SetFocus();
                    break;
                case (int)WindowsMessages.WM_KILLFOCUS:
                    KillFocus();
                    break;
                case (int)WindowsMessages.WM_SIZE:
                    Size(wParam, lParam);
                    break;
            }

            return (IntPtr)0;
        }

        internal void Detach()
        {
            _isAttached = false;

            Show(false);

            UnregisterEvents();
        }

        /// <summary>
        /// Enables or disables the glow effect on the window
        /// </summary>
        /// <param name="enable">Enable mode</param>
        internal void Enable(bool enable)
        {
            if (_isEnabled && !enable)
            {
                Show(false);
                UnregisterEvents();
            }
            else if (!_isEnabled && enable)
            {
                RegisterEvents();
                if (_window != null)
                {
                    UpdateLocations(new WINDOWPOS
                    {
                        x = (int)_window.Left,
                        y = (int)_window.Top,
                        cx = (int)_window.ActualWidth,
                        cy = (int)_window.ActualHeight,
                        flags = (int)SetWindowPosFlags.SWP_SHOWWINDOW
                    });

                    UpdateSizes((int)_window.ActualWidth, (int)_window.ActualHeight);
                }
            }

            _isEnabled = enable;
        }

        internal void EnableResize(bool enable)
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.ExternalResizeEnable = enable;
            }
        }

        #endregion

        #region [private]

        private void DestroyGlows()
        {
            _parentWindowHndl = IntPtr.Zero;

            CloseGlows();

            _window = null;
        }

        private void HandleWindowVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Show(_window.IsVisible);
        }

        private void RegisterEvents()
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.MouseDown += HandleSideMouseDown;
            }

            if (_window != null)
            {
                _window.IsVisibleChanged += HandleWindowVisibleChanged;
            }
        }

        private void UnregisterEvents()
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.MouseDown -= HandleSideMouseDown;
            }

            if (_window != null)
            {
                _window.IsVisibleChanged -= HandleWindowVisibleChanged;
            }
        }

        private void HandleSideMouseDown(object sender, SideGlowResizeArgs e)
        {
            if (e.Mode == HitTest.HTNOWHERE || e.Mode == HitTest.HTCAPTION)
            {
                return;
            }

            User32.SendMessage(_parentWindowHndl, (uint)WindowsMessages.WM_SYSCOMMAND, (IntPtr)e.Mode.ToInt(), IntPtr.Zero);
        }

        private void CloseGlows()
        {
            foreach (var sideGlow in _glows)
            {
                sideGlow.Close();
            }

            _glows.Clear();

            _topGlow = null;
            _bottomGlow = null;
            _leftGlow = null;
            _rightGlow = null;
        }

        private void Show(bool show)
        {
            foreach (SideGlow sideGlow in _glows) sideGlow.Show(show);
        }

        private void UpdateZOrder()
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.UpdateZOrder();
            }
        }

        private void UpdateFocus(bool isFocused)
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.ParentWindowIsFocused = isFocused;
            }
        }

        private void UpdateSizes(int width, int height)
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.SetSize(width, height);
            }
        }

        private void UpdateLocations(WINDOWPOS location)
        {
            foreach (SideGlow sideGlow in _glows)
            {
                sideGlow.SetLocation(location);
            }

            if ((location.flags & (int)SetWindowPosFlags.SWP_HIDEWINDOW) != 0)
            {
                Show(false);
            }
            else if ((location.flags & (int)SetWindowPosFlags.SWP_SHOWWINDOW) != 0)
            {
                Show(true);
                UpdateZOrder();
            }
        }

        private void AlignSideGlowTopMost()
        {
            if (_glows == null)
            {
                return;
            }

            foreach (SideGlow glow in _glows)
            {
                glow.IsTopMost = _setTopMost;
                glow.UpdateZOrder();
            }
        }

        #endregion

        #region [WM_events handlers]

        public void SetFocus()
        {
            if (!_isEnabled) return;
            UpdateFocus(true);
            UpdateZOrder();
        }

        public void KillFocus()
        {
            if (!_isEnabled) return;
            UpdateFocus(false);
            UpdateZOrder();
        }

        public void WindowPosChanged(WINDOWPOS location)
        {
            if (!_isEnabled) return;
            UpdateLocations(location);
        }

        public void Activate(bool isActive)
        {
            if (!_isEnabled) return;
            UpdateZOrder();
        }

        public void Size(IntPtr wParam, IntPtr lParam)
        {
            if (!_isEnabled) return;
            if ((int)wParam == 2 || (int)wParam == 1) // maximized/minimized
            {
                Show(false);
            }
            else
            {
                Show(true);
                int width = (int)User32.LoWord(lParam);
                int height = (int)User32.HiWord(lParam);
                UpdateSizes(width, height);
            }
        }

        #endregion

        #region Dispose

        private bool _isDisposed;

        /// <summary>
        /// IsDisposed status
        /// </summary>
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        /// <summary>
        /// Standard Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">True if disposing, false otherwise</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // release unmanaged resources
                }

                _isDisposed = true;

                Detach();
                DestroyGlows();

                UnregisterEvents();

                _window = null;
            }
        }

        #endregion
    }
}
