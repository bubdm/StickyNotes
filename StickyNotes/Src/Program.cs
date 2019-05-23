using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using System.Net;

namespace StickyNotes
{
	class Program
	{
		public static Hooker HookerInstance = new Hooker();
		public static MainWindow WndMain;
		public static Dictionary<string, StickyWindow> Wnds = new Dictionary<string, StickyWindow>();

		[STAThread]
		static void Main(string[] args)
		{
			// Sciter needs this for drag'n'drop support; STAThread is required for OleInitialize succeess
			int oleres = PInvokeWindows.OleInitialize(IntPtr.Zero);
			Debug.Assert(oleres == 0);

			// Create the window
			var wnd = WndMain = new MainWindow();
			wnd.CreateMainWindow(1, 1);
			wnd.Title = "Sciter-based desktop TemplateDesktopGadgets";
			WndMain.CreateTaskbarIcon();

			// Prepares SciterHost and then load the page
			var host = new BaseHost();
			host.Setup(wnd);
			host.AttachEvh(new HostEvh());
			host.SetupPage("index.html");

			HookerInstance.SetMessageHook();

			LoadData();

			// Run message loop
			PInvokeUtils.RunMsgLoop();

			HookerInstance.ClearHook();
		}

		public static void LoadData()
		{
		}

		public static void Exit()
		{
#if WINDOWS
			foreach(var wnd in Program.Wnds.Values)
				wnd.Destroy();
			WndMain.Destroy();

			Thread.Sleep(200);
			Environment.Exit(0);
			//PInvoke.User32.PostQuitMessage(0);
#else
			AppKit.NSApplication.SharedApplication.Terminate(null);
#endif
		}

		/*public static void RunHooker()
		{
			string hookexe = Environment.Is64BitOperatingSystem ? @"\Hook\64\Hooker.exe" : @"\Hook\32\Hooker.exe";
			hookexe = AppDomain.CurrentDomain.BaseDirectory + hookexe;
			Debug.Assert(System.IO.File.Exists(hookexe));

			var p = Process.Start(new ProcessStartInfo()
			{
				FileName = hookexe,
				WindowStyle = ProcessWindowStyle.Hidden
			});

			Debug.Assert(p.HasExited==false);
		}*/
	}
}