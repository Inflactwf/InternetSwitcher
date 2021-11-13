﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Internet_Switcher
{
    class UserActivityHook
    {
        private const int WH_MOUSE_LL = 14;

        private const int WH_KEYBOARD_LL = 13;

        private const int WH_MOUSE = 7;

        private const int WH_KEYBOARD = 2;

        private const int WM_MOUSEMOVE = 512;

        private const int WM_LBUTTONDOWN = 513;

        private const int WM_RBUTTONDOWN = 516;

        private const int WM_MBUTTONDOWN = 519;

        private const int WM_LBUTTONUP = 514;

        private const int WM_RBUTTONUP = 517;

        private const int WM_MBUTTONUP = 520;

        private const int WM_LBUTTONDBLCLK = 515;

        private const int WM_RBUTTONDBLCLK = 518;

        private const int WM_MBUTTONDBLCLK = 521;

        private const int WM_MOUSEWHEEL = 522;

        private const int WM_KEYDOWN = 256;

        private const int WM_KEYUP = 257;

        private const int WM_SYSKEYDOWN = 260;

        private const int WM_SYSKEYUP = 261;

        private const byte VK_SHIFT = 16;

        private const byte VK_CAPITAL = 20;

        private const byte VK_NUMLOCK = 144;

        //private int hMouseHook;

        private int hKeyboardHook;

        //private static UserActivityHook.HookProc MouseHookProcedure;

        private static UserActivityHook.HookProc KeyboardHookProcedure;

        public UserActivityHook()
        {
            this.Start();
        }

        public UserActivityHook(bool InstallKeyboardHook)
        {
            this.Start(InstallKeyboardHook);
        }

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, ExactSpelling = false)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        ~UserActivityHook()
        {
            this.Stop(true, false);
        }

        [DllImport("user32", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, ExactSpelling = false)]
        private static extern short GetKeyState(int vKey);

        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0 && (this.KeyDown != null || this.KeyUp != null || this.KeyPress != null))
            {
                UserActivityHook.KeyboardHookStruct MyKeyboardHookStruct = (UserActivityHook.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(UserActivityHook.KeyboardHookStruct));
                if (this.KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    KeyEventArgs e = new KeyEventArgs((Keys)MyKeyboardHookStruct.vkCode);
                    this.KeyDown(this, e);
                    handled = (handled || e.Handled);
                }
                if (this.KeyPress != null && wParam == WM_KEYDOWN)
                {
                    bool isDownShift = ((UserActivityHook.GetKeyState(16) & 128) == 128 ? true : false);
                    bool isDownCapslock = (UserActivityHook.GetKeyState(20) != 0 ? true : false);
                    byte[] keyState = new byte[256];
                    UserActivityHook.GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (UserActivityHook.ToAscii(MyKeyboardHookStruct.vkCode, MyKeyboardHookStruct.scanCode, keyState, inBuffer, MyKeyboardHookStruct.flags) == 1)
                    {
                        char key = (char)inBuffer[0];
                        if (isDownCapslock ^ isDownShift && char.IsLetter(key))
                        {
                            key = char.ToUpper(key);
                        }
                        KeyPressEventArgs e = new KeyPressEventArgs(key);
                        this.KeyPress(this, e);
                        handled = (handled || e.Handled);
                    }
                }
                if (this.KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {
                    KeyEventArgs e = new KeyEventArgs((Keys)MyKeyboardHookStruct.vkCode);
                    this.KeyUp(this, e);
                    handled = (handled || e.Handled);
                }
            }
            if (handled)
            {
                return 1;
            }
            return UserActivityHook.CallNextHookEx(this.hKeyboardHook, nCode, wParam, lParam);
        }

        //private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        //{
        //    if (nCode >= 0 && this.OnMouseActivity != null)
        //    {
        //        UserActivityHook.MouseLLHookStruct mouseHookStruct = (UserActivityHook.MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(UserActivityHook.MouseLLHookStruct));
        //        MouseButtons button = MouseButtons.None;
        //        short mouseDelta = 0;
        //        int num = wParam;
        //        if (num == WM_LBUTTONDOWN)
        //        {
        //            button = MouseButtons.Left;
        //        }
        //        else if (num == WM_RBUTTONDOWN)
        //        {
        //            button = MouseButtons.Right;
        //        }
        //        else if (num == WM_MOUSEWHEEL)
        //        {
        //            mouseDelta = (short)(mouseHookStruct.mouseData >> 16 & 65535);
        //        }
        //        int clickCount = 0;
        //        if (button != MouseButtons.None)
        //        {
        //            clickCount = (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK ? 2 : 1);
        //        }
        //        MouseEventArgs e = new MouseEventArgs(button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
        //        this.OnMouseActivity(this, e);
        //    }
        //    return UserActivityHook.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
        //}

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
        private static extern int SetWindowsHookEx(int idHook, UserActivityHook.HookProc lpfn, IntPtr hMod, int dwThreadId);

        public void Start()
        {
            this.Start(true);
        }

        public void Start(bool InstallKeyboardHook)
        {
            //if (this.hMouseHook == 0 && InstallMouseHook)
            //{
            //    UserActivityHook.MouseHookProcedure = new UserActivityHook.HookProc(this.MouseHookProc);
            //    this.hMouseHook = UserActivityHook.SetWindowsHookEx(14, UserActivityHook.MouseHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
            //    if (this.hMouseHook == 0)
            //    {
            //        int errorCode = Marshal.GetLastWin32Error();
            //        this.Stop(true, false, false);
            //        throw new Win32Exception(errorCode);
            //    }
            //}
            if (this.hKeyboardHook == 0 && InstallKeyboardHook)
            {
                UserActivityHook.KeyboardHookProcedure = new UserActivityHook.HookProc(this.KeyboardHookProc);
                this.hKeyboardHook = UserActivityHook.SetWindowsHookEx(13, UserActivityHook.KeyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                if (this.hKeyboardHook == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    this.Stop(true, false);
                    throw new Win32Exception(errorCode);
                }
            }
        }

        public void Stop()
        {
            this.Stop(true, true);
        }

        public void Stop(bool UninstallKeyboardHook, bool ThrowExceptions)
        {
            //if (this.hMouseHook != 0 && UninstallMouseHook)
            //{
            //    int retMouse = UserActivityHook.UnhookWindowsHookEx(this.hMouseHook);
            //    this.hMouseHook = 0;
            //    if (retMouse == 0 && ThrowExceptions)
            //    {
            //        throw new Win32Exception(Marshal.GetLastWin32Error());
            //    }
            //}
            if (this.hKeyboardHook != 0 && UninstallKeyboardHook)
            {
                int retKeyboard = UserActivityHook.UnhookWindowsHookEx(this.hKeyboardHook);
                this.hKeyboardHook = 0;
                if (retKeyboard == 0 && ThrowExceptions)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        [DllImport("user32", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        public event KeyEventHandler KeyDown;

        public event KeyPressEventHandler KeyPress;

        public event KeyEventHandler KeyUp;

        //public event MouseEventHandler OnMouseActivity;

        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            public int vkCode;

            public int scanCode;

            public int flags;

            public int time;

            public int dwExtraInfo;

            public KeyboardHookStruct()
            {
            }
        }
        //[StructLayout(LayoutKind.Sequential)]
        //private class MouseHookStruct
        //{
        //    public UserActivityHook.POINT pt;

        //    public int hwnd;

        //    public int wHitTestCode;

        //    public int dwExtraInfo;

        //    public MouseHookStruct()
        //    {
        //    }
        //}
        //[StructLayout(LayoutKind.Sequential)]
        //private class MouseLLHookStruct
        //{
        //    public UserActivityHook.POINT pt;

        //    public int mouseData;

        //    public int flags;

        //    public int time;

        //    public int dwExtraInfo;

        //    public MouseLLHookStruct()
        //    {
        //    }
        //}
        //[StructLayout(LayoutKind.Sequential)]
        //private class POINT
        //{
        //    public int x;

        //    public int y;

        //    public POINT()
        //    {
        //    }
        //}
    }
}