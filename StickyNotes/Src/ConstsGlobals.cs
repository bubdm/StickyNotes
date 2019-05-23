using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNotes
{
	static partial class Consts
	{
		public const string AppName = "Sticky Notes";
		//public const EProduct ProductID = EProduct.DESIGNARSENAL;
		public static readonly string APP_EXE = Process.GetCurrentProcess().MainModule.FileName;

		static Consts()
		{
		}
	}
}