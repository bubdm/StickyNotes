using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;

namespace StickyNotes
{
	class Program
	{
		public static Hooker HookerInstance = new Hooker();
		public static Dictionary<string, StikyWindow> Wnds = new Dictionary<string, StikyWindow>();

		[STAThread]
		static void Main(string[] args)
		{
			// Sciter needs this for drag'n'drop support; STAThread is required for OleInitialize succeess
			int oleres = PInvokeWindows.OleInitialize(IntPtr.Zero);
			Debug.Assert(oleres == 0);
			
			// Create the window
			var wnd = new MainWindow();
			wnd.CreateMainWindow(1, 1);
			wnd.Title = "Sciter-based desktop TemplateDesktopGadgets";

			// Prepares SciterHost and then load the page
			var host = new BaseHost();
			host.Setup(wnd);
			host.AttachEvh(new HostEvh());
			host.SetupPage("index.html");

			HookerInstance.SetMessageHook();

			// Run message loop
			PInvokeUtils.RunMsgLoop();

			HookerInstance.ClearHook();
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