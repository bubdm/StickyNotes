using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace StickyNotes
{
	class HostEvh : SciterEventHandler
	{
		public StickyWindow _wnd;

		public void Host_Dbg()
		{
		}

		public void Host_CreateStickyWindow(SciterValue[] args)
		{
			var wnd = new StickyWindow();
			wnd.CreateMainWindow(400, 300, SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_POPUP | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ALPHA);
			wnd.CenterTopLevelWindow();
			wnd.HideTaskbarIcon();
			wnd.Icon = Properties.Resources.note;

			var evh = new HostEvh();
			evh._wnd = wnd;

			var host = new BaseHost();
			host.Setup(wnd);
			host.AttachEvh(evh);
			host.SetupPage("stickynote.html");

			var guid = args[0].Get("");
			Debug.Assert(guid.Length > 0);
			Program.Wnds.Add(guid, wnd);

			wnd.CallFunction("View_LoadNote", args[0]);
			wnd.Show();
			wnd.SetTopmost(true);
		}

		public SciterValue Host_NewGUID() => new SciterValue(Guid.NewGuid().ToString());
		public void Host_EmulateMoveWnd() => _wnd.EmulateMoveWnd();


#if WINDOWS
		public SciterValue Host_IsRegistryRun()
		{
			RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
			return new SciterValue(rkApp.GetValue(Consts.AppName) is string);
		}

		public void Host_RunRegistry(SciterValue[] args)
		{
			RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if(args[0].Get(false))
				rkApp.SetValue(Consts.AppName, "\"" + Consts.APP_EXE + "\"");
			else
				rkApp.DeleteValue(Consts.AppName, false);
		}
#endif
	}

	// This base class overrides OnLoadData and does the resource loading strategy
	// explained at http://misoftware.com.br/Bootstrap/Dev
	//
	// - in DEBUG mode: resources loaded directly from the file system
	// - in RELEASE mode: resources loaded from by a SciterArchive (packed binary data contained as C# code in ArchiveResource.cs)
	class BaseHost : SciterHost
	{
		protected static SciterX.ISciterAPI _api = SciterX.API;
		protected SciterArchive _archive = new SciterArchive();
		protected SciterWindow _wnd;

		public BaseHost()
		{
		#if !DEBUG
			_archive.Open(SciterSharpAppResource.ArchiveResource.resources);
		#endif
		}

		public void Setup(SciterWindow wnd)
		{
			_wnd = wnd;
			SetupWindow(wnd._hwnd);
		}

		public void SetupPage(string page_from_res_folder)
		{
		#if DEBUG
			string path = Environment.CurrentDirectory + "/../../res/" + page_from_res_folder;
			Debug.Assert(File.Exists(path));
            path = path.Replace('\\', '/');
			path = Path.GetFullPath(path);
			Debug.Assert(File.Exists(path));

			string url = "file://" + path;
		#else
			string url = "archive://app/" + page_from_res_folder;
		#endif

			bool res = _wnd.LoadPage(url);
			Debug.Assert(res);
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("archive://app/"))
			{
				// load resource from SciterArchive
				string path = sld.uri.Substring(14);
				byte[] data = _archive.Get(path);
				if(data!=null)
					_api.SciterDataReady(_wnd._hwnd, sld.uri, data, (uint) data.Length);
			}

			// call base to ensure LibConsole is loaded
			return base.OnLoadData(sld);
		}
	}
}