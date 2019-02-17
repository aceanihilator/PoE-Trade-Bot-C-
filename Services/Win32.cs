using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoE_Trade_Bot.Services
{
    public class Win32
    {
        private static uint X = 0;
        private static uint Y = 0;

        private static Process PoE_Process = null;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("User32.Dll")]
        private static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public static void DoMouseClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static void MoveTo(int x, int y)
        {
            X = Convert.ToUInt32(x);
            Y = Convert.ToUInt32(y);
            SetCursorPos(x, y);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr SetFocus(HandleRef hWnd);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void PoE_MainWindow()
        {
            Process[] processList = Process.GetProcesses();

            foreach (Process theProcess in processList)
            {
                string processName = theProcess.ProcessName;

                if (processName.ToLower().Contains("pathofexile"))
                {
                    PoE_Process = theProcess;
                    string mainWindowTitle = theProcess.MainWindowTitle;
                    SetForegroundWindow(theProcess.MainWindowHandle);
                    break;
                }
            }

        }

        public static bool IsPoERun()
        {
            Process[] processList = Process.GetProcesses();

            foreach (Process theProcess in processList)
            {
                string processName = theProcess.ProcessName;

                if (processName.ToLower().Contains("pathofexile"))
                {
                    PoE_Process = theProcess;

                    break;
                }
            }

            if (PoE_Process == null)
                return false;
            else
                return true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        // Declare some keyboard keys as constants with its respective code
        // See Virtual Code Keys: https://msdn.microsoft.com/en-us/library/dd375731(v=vs.85).aspx
        public const int KEYEVENTF_EXTENDEDKEY = 0x1; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x2; //Key up flag
        public const int VK_CONTROL = 0x11; //Right Control key code
        public const int VK_SHIFT = 0x10; //Right Control key code

        // Simulate a key press event
        private static void DownCtrl()
        {
            keybd_event(VK_CONTROL, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
        }

        private static void UpCtrl()
        {
            keybd_event(VK_CONTROL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        public static void CtrlMouseClick()
        {
            DownCtrl();
            Thread.Sleep(100);
            DoMouseClick();
            Thread.Sleep(100);
            UpCtrl();
        }

        public static void SendKeyInPoE(string key)
        {
            var proc = PoE_Process;
            if (proc != null)
            {
                var handle = proc.MainWindowHandle;
                SetForegroundWindow(handle);
                SendKeys.SendWait(key);
            }
        }

        public static void ChatCommand(string command)
        {
            var proc = PoE_Process;
            if (proc != null)
            {
                var handle = proc.MainWindowHandle;
                SetForegroundWindow(handle);

                SendKeys.SendWait("{ENTER}");
                foreach (char c in command)
                {
                    SendKeys.SendWait(c.ToString());
                }
                SendKeys.SendWait("{ENTER}");

            }
        }

        public static void SetTextClipboard(string text)
        {
            OpenClipboard(GetOpenClipboardWindow());
            var ptr = Marshal.StringToHGlobalUni(text);
            SetClipboardData(CF_UNICODETEXT, ptr);
            CloseClipboard();
            Marshal.FreeHGlobal(ptr);
        }
        public static void SetTextClipboard()
        {
            OpenClipboard(IntPtr.Zero);
            CloseClipboard();
        }

        #region Win32

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern int GlobalSize(IntPtr hMem);

        [DllImport("user32.dll")]
        static extern IntPtr GetOpenClipboardWindow();

        private const uint CF_UNICODETEXT = 13;

        #endregion

        public static string GetText()
        {
            IntPtr intptr;

            if (OpenClipboard(GetOpenClipboardWindow()))
            {

                intptr = GetClipboardData(CF_UNICODETEXT);
                CloseClipboard();

                return Marshal.PtrToStringAuto(intptr);

            }
            return null;
        }

        internal static void ShiftClick()
        {
            keybd_event(VK_SHIFT, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
            Thread.Sleep(100);
            DoMouseClick();
            Thread.Sleep(100);
            keybd_event(VK_SHIFT, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        internal static void SendNumber_PoE(int number)
        {
            string str = "" + number;

            foreach (Char c in str)
            {
                SendKeys.SendWait(c.ToString());
            }
        }
    }
}
