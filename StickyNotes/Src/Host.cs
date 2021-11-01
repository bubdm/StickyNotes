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
using System.Reflection;
using PInvoke;

namespace StickyNotes
{
	class HostEvh : SciterEventHandler
	{
		private static List<HostEvh> _instances = new List<HostEvh>();
		private StickyWindow _wnd;

		public HostEvh() { _instances.Add(this); }

		public void Host_Dbg(SciterValue[] args)
		{
			var json = args[0].ToJSONString();
		}

		public SciterValue Host_CreateStickyWindow(SciterValue[] args)
		{
			var wnd = new StickyWindow();
			wnd.CreateMainWindow(320, 320, SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_POPUP | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ALPHA);
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

			var view = wnd.CallFunction("View_LoadNote", args[0]);
			var aa = view.ToJSONString();
			Debug.Assert(!view.IsUndefined);

			wnd.Show();
			bool res = User32.SetForegroundWindow(wnd._hwnd);
			new Win32Hwnd(wnd._hwnd).FocusAndActivate();
			host.InvokePost(() =>
			{
				new Win32Hwnd(wnd._hwnd).FocusAndActivate();
				wnd.Show();
				res = User32.SetForegroundWindow(wnd._hwnd);
			});
			return view;
		}

		public SciterValue Host_GetTodayCards()
		{
			var cards = new SpaceRepetition().TodayCards();
			return SciterValue.FromObject(cards);
		}

		public void Host_ReviewCard(SciterValue[] args)
		{
			new SpaceRepetition().ReviewAttempt(args[0].Get(0), args[0].Get(false));
		}

		public void Host_AddCard(SciterValue[] args)
		{
			new SpaceRepetition().AddCard(args[0].Get(""), args[1].Get(""));
		}

		public SciterValue Host_NewGUID() => new SciterValue(Guid.NewGuid().ToString());
		public SciterValue Host_AppVersion()
		{
			var result = new SciterValue();
			result["ver"] = new SciterValue(Consts.Version);
			result["dt"] = new SciterValue(File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location));
			return result;
		}
		public void Host_EmulateMoveWnd() => _wnd.EmulateMoveWnd();
		public void Host_Quit() => Program.Exit();
		public SciterValue Host_ReadFile(SciterValue[] args)
		{
			string path = args[0].Get("");
			if(File.Exists(path))
			{
				string data = File.ReadAllText(path);
				return new SciterValue(data);
			}
			return SciterValue.Undefined;
		}

		public void Host_WriteFile(SciterValue[] args)
		{
			string path = args[0].Get("");
			string data = args[1].Get("");
			File.WriteAllText(path, data);
		}


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

		private static List<BaseHost> _instances = new List<BaseHost>();

		public BaseHost()
		{
			_instances.Add(this);

		#if !DEBUG
			_archive.Open(SciterAppResource.ArchiveResource.resources);
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
			string path = Path.GetDirectoryName(Consts.APP_EXE) + "/../../res/" + page_from_res_folder;
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