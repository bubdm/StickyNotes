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
using StickyNotes.Native;
//using Ion;
using PInvoke;

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
			#region Args handling
			bool arg_in_test = false;

			if(args.Length > 0)
			{
				if(args[0].StartsWith("-jumplist:"))
				{
					MainWindow.SendJumplistCmd(args[0].Substring(10));
					return;
				}
			}
			#endregion

			//UpdateControl.Setup();

			// Sciter needs this for drag'n'drop support; STAThread is required for OleInitialize succeess
			int oleres = PInvokeWindows.OleInitialize(IntPtr.Zero);
			Debug.Assert(oleres == 0);
			Debug.WriteLine("Sciter " + SciterX.Version);

			// Create the window
			var wnd = WndMain = new MainWindow();
			wnd.CreateMainWindow(1, 1);
			wnd.CreateTaskbarIcon();
			wnd.CreateJumplists();
			wnd.Title = MainWindow.WND_TITLE;

			// Prepares SciterHost and then load the page
			var host = new BaseHost();
			host.Setup(wnd);
			host.AttachEvh(new HostEvh());
			host.SetupPage("index.html");

			HookerInstance.SetMessageHook();

			if(!arg_in_test && SingleInstance.IsRunningAndAcquire())
			{
				Debug.WriteLine("ALREADY RUNNING!");
				return;
			}

			//MainWindow.SendJumplistCmd("BringToFront");
			//wnd.CreateNote();

			// Run message loop
			PInvokeUtils.RunMsgLoop();

			Exit();
		}

		public static void Exit()
		{
#if WINDOWS
			foreach(var wnd in Wnds.Values)
				wnd.Destroy();
			WndMain.Destroy();
			WndMain.Dispose();

			SingleInstance.Release();
			HookerInstance.ClearHook();

			Thread.Sleep(200);
			Environment.Exit(0);
			PInvoke.User32.PostQuitMessage(0);
#else
			AppKit.NSApplication.SharedApplication.Terminate(null);
#endif
		}
	}
}