using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SciterSharp;
using SciterSharp.Interop;
using PInvoke;

namespace StickyNotes
{
	class MainWindow : SciterWindow
	{
		private static NotifyIcon _ni;

		static uint WM_TASKBAR_CREATED = RegisterWindowMessage("TaskbarCreated");
		const   uint WM_APP = 0x8000;
		const	uint WM_NCPAINT = 0x0085;
		const	uint WM_DESKTOP_CHANGED = WM_APP + 99;
		const	uint WM_ENDSESSION = 22;
		const	uint WM_CLOSE = 16;
		const	uint WM_NCLBUTTONDOWN = 161;

		public void CreateNote()
		{
			var r = EvalScript("View.Proxy_AddNote()");
		}
		
		protected override bool ProcessWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, ref IntPtr lResult)
		{
			if(msg==WM_TASKBAR_CREATED)
			{
				Program.HookerInstance.SetMessageHook();
				return true;
			}

			if(msg == WM_DESKTOP_CHANGED)
			{
				if(wParam.ToInt32() == 0)
				{
					ShowIt(true);
					Debug.WriteLine("WM_DESKTOP_CHANGED show " + DateTime.Now);
				}
				else
				{
					ShowIt(false);
					Debug.WriteLine("WM_DESKTOP_CHANGED hide " + DateTime.Now);
				}
				return true;
			}

			if(msg == WM_ENDSESSION)
			{
				// system is shuting down, close app
				SendMessageW(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
				PostQuitMessage(0);
				return true;
			}
			
			return false;
		}

		public void CreateTaskbarIcon()
		{
			var menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem("Add Note", (e, a) => CreateNote()));
			menu.MenuItems.Add(new MenuItem("Quit", (e, a) => Program.Exit()));

			_ni = new NotifyIcon();
			_ni.Icon = Properties.Resources.note;
			_ni.Visible = true;
			_ni.ContextMenu = menu;
			_ni.Click += (s, e) =>
			{
				if((e as MouseEventArgs).Button == MouseButtons.Left)
					ShowIt(true);
			};
		}

		private void ShowIt(bool show)
		{
			foreach(var wnd in Program.Wnds.Values)
			{
				wnd.SetTopmost(show);
				wnd.Show();
			}
		}

		#region PInvoke stuff
		const uint WM_NCCALCSIZE = 0x0083;
		const uint WM_NCHITTEST = 0x0084;
		const uint WM_WINDOWPOSCHANGED = 0x0047;
		const uint WM_SIZE = 0x0005;

		const int HTERROR = (-2);
		const int HTTRANSPARENT = (-1);
		const int HTNOWHERE = 0;
		const int HTCLIENT = 1;
		const int HTCAPTION = 2;
		const int HTSYSMENU = 3;
		const int HTGROWBOX = 4;
		const int HTSIZE = HTGROWBOX;
		const int HTMENU = 5;
		const int HTHSCROLL = 6;
		const int HTVSCROLL = 7;
		const int HTMINBUTTON = 8;
		const int HTMAXBUTTON = 9;
		const int HTLEFT = 10;
		const int HTRIGHT = 11;
		const int HTTOP = 12;
		const int HTTOPLEFT = 13;
		const int HTTOPRIGHT = 14;
		const int HTBOTTOM = 15;
		const int HTBOTTOMLEFT = 16;
		const int HTBOTTOMRIGHT = 17;
		const int HTBORDER = 18;
		const int HTREDUCE = HTMINBUTTON;
		const int HTZOOM = HTMAXBUTTON;
		const int HTSIZEFIRST = HTLEFT;
		const int HTSIZELAST = HTBOTTOMRIGHT;

		enum WmSizeType
		{
			SIZE_MAXHIDE = 4,
			SIZE_MAXIMIZED = 2,
			SIZE_MAXSHOW = 3,
			SIZE_MINIMIZED = 1,
			SIZE_RESTORED = 0
		}

		static int LoWord(int dwValue)
		{
			return dwValue & 0xFFFF;
		}

		static int HiWord(int dwValue)
		{
			return (dwValue >> 16) & 0xFFFF;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct NCCALCSIZE_PARAMS
		{
			public PInvokeUtils.RECT rect0, rect1, rect2;
			public IntPtr lppos;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MARGINS
		{
			public int leftWidth;
			public int rightWidth;
			public int topHeight;
			public int bottomHeight;
		}

		enum SetWindowPosWindow : int
		{
			HWND_BOTTOM = 1,
			HWND_NOTOPMOST = -2,
			HWND_TOP = 0,
			HWND_TOPMOST = -1,
		}

		[Flags]
		enum SetWindowPosFlags : uint
		{
			SWP_NOSIZE = 0x0001,
			SWP_NOMOVE = 0x0002,
			SWP_NOZORDER = 0x0004,
			SWP_NOREDRAW = 0x0008,
			SWP_NOACTIVATE = 0x0010,
			SWP_FRAMECHANGED = 0x0020,  /* The frame changed: send WM_NCCALCSIZE */
			SWP_SHOWWINDOW = 0x0040,
			SWP_HIDEWINDOW = 0x0080,
			SWP_NOCOPYBITS = 0x0100,
			SWP_NOOWNERZORDER = 0x0200,  /* Don't do owner Z ordering */
			SWP_NOSENDCHANGING = 0x0400,  /* Don't send WM_WINDOWPOSCHANGING */
			SWP_DRAWFRAME = SWP_FRAMECHANGED,
			SWP_NOREPOSITION = SWP_NOOWNERZORDER,
			SWP_DEFERERASE = 0x2000,
			SWP_ASYNCWINDOWPOS = 0x4000,
		}

		[DllImport("user32.dll")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

		[DllImport("user32.dll")]
		static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("dwmapi.dll")]
		static extern int DwmDefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr result);

		[DllImport("dwmapi.dll")]
		static extern int DwmIsCompositionEnabled(out bool enabled);

		[DllImport("dwmapi.dll")]
		static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

		[DllImport("user32.dll")]
		static extern bool IsZoomed(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hwnd, out PInvokeUtils.RECT lpRect);

		[DllImport("user32.dll")]
		static extern bool ScreenToClient(IntPtr hWnd, ref PInvokeUtils.POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern void PostQuitMessage(int nExitCode);

		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


		#region Windows 10 SetWindowCompositionAttribute
		internal enum AccentState
		{
			ACCENT_DISABLED = 0,
			ACCENT_ENABLE_GRADIENT = 1,
			ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
			ACCENT_ENABLE_BLURBEHIND = 3,
			ACCENT_INVALID_STATE = 4
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct AccentPolicy
		{
			public AccentState AccentState;
			public int AccentFlags;
			public int GradientColor;
			public int AnimationId;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct WindowCompositionAttributeData
		{
			public WindowCompositionAttribute Attribute;
			public IntPtr Data;
			public int SizeOfData;
		}

		internal enum WindowCompositionAttribute
		{
			// ...
			WCA_ACCENT_POLICY = 19
			// ...
		}

		[DllImport("user32.dll")]
		internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
		#endregion
		#endregion
	}
}