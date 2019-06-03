using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using StickyNotes;

partial class Script
{
	const string APPNAME = "StickyNotes";
	const string APPNAME_EXE = APPNAME + ".exe";
	const string CONFIG = "Release";

	public static void Main(string[] args)
	{
		if(Environment.OSVersion.Platform == PlatformID.Win32NT)
			CWD = @"D:\ProjetosSciter\StickyNotes\ReleaseInfo\";
		else
			CWD = "/Users/midiway/Documents/Omni/";

		Environment.CurrentDirectory = CWD;

		string exe;
		if(true)
		{
			//GitPush();
			exe = BuildAndDeploy();

			// Run with -test
			Console.WriteLine("### RUN + TESTS (WAITS FOR EXIT) ###");
			SpawnProcess(exe, "-test");
		}
		else
		{
			_upload_output = CWD + "ReleaseInfo/Latest/DesignArsenal.zip";
		}

		// Copy to Dropbox
		File.Copy(_upload_output, "D:\\Dropbox\\Apps\\" + Path.GetFileName(_upload_output));

		// Save version
		Console.WriteLine("### UPDATE INFO");
		using(WebClient wb = new WebClient())
		{
			var res = wb.DownloadString("https://ionsite.azurewebsites.net/Info/SetInfo?ep=" + Consts.ProductID + "&version=" + Consts.VersionInt);
			Debug.Assert(res == "OK");
		}

		// Print in debug
		Debug.WriteLine("### COMPLETED!");
	}

	static string BuildAndDeploy()
	{
		Console.WriteLine("### BUILD ###");
		if(Environment.OSVersion.Platform == PlatformID.Unix)
		{
			string DIR_PROJ = CWD + APPNAME + "/";

			SpawnProcess("sh", DIR_PROJ + "scripts/packOSX.sh");
			SpawnProcess("msbuild", DIR_PROJ + APPNAME + "OSX.csproj /t:Build /p:Configuration=Release");

			string DIR_RI = CWD + "ReleaseInfo/";
			string DIR_LATEST = DIR_RI + "Latest/";

			if(Directory.Exists(DIR_LATEST))
				Directory.Delete(DIR_LATEST, true);
			Directory.CreateDirectory(DIR_LATEST);

			string DIR_APP = DIR_PROJ + "bin/Release/" + APPNAME + ".app/";
			String DIR_RI_APP = DIR_LATEST + APPNAME + ".app/";
			Directory.Move(DIR_APP, DIR_RI_APP);

			_upload_output = DIR_RI + APPNAME + "OSX.zip";
			File.Delete(_upload_output);

			ZipFile.CreateFromDirectory(DIR_LATEST, _upload_output);

			return DIR_RI_APP + "Contents/MacOS/" + APPNAME;
		}
		else
		{
			string how = "Clean,Build";
			string proj = CWD + $"..\\{APPNAME}\\{APPNAME}Windows.csproj";
			Debug.Assert(File.Exists(proj));
			SpawnProcess(@"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe",
					$"{proj} /t:{how} /p:Configuration={CONFIG} /p:Platform=x64");
			
			var WORK_DIR = $"{CWD}TmpInput\\";

			// Delete and create these dirs
			if(Directory.Exists(WORK_DIR))
				Directory.Delete(WORK_DIR, true);
			Directory.CreateDirectory(WORK_DIR);

			// Copy \bin\Release to WORK_DIR
			string BIN_DIR = Path.GetFullPath(CWD + "..\\" + APPNAME + "\\bin\\Release");

			var files1 = Directory.EnumerateFiles(BIN_DIR, "*.exe", SearchOption.AllDirectories).ToList();
			var files2 = Directory.EnumerateFiles(BIN_DIR, "*.dll", SearchOption.AllDirectories).ToList();
			Debug.Assert(files1.Count + files2.Count > 0);
			foreach(var file in files1.Union(files2))
			{
				if(file.EndsWith(".vshost.exe"))
					continue;
				string subpath = file.Substring(BIN_DIR.Length);
				string outpath = WORK_DIR + subpath;
				Directory.CreateDirectory(Path.GetDirectoryName(outpath));
				File.Copy(file, outpath);
			}
			
			// Rename dir
			var latest_dir = CWD + $"Latest\\";
			if(Directory.Exists(latest_dir))
				Directory.Delete(latest_dir, true);
			Directory.Move(WORK_DIR, latest_dir);

			// ZIP
			string zipfile = CWD + APPNAME + "Win.zip";
			File.Delete(zipfile);
			ZipFile.CreateFromDirectory(latest_dir, zipfile);
			_upload_output = zipfile;

			return latest_dir + APPNAME_EXE;
		}
	}
}