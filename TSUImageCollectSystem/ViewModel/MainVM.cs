using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.ViewModel
{
	class MainVM
	{
		public BaumerVM BVM { get; private set; }

		public SickVM SVM { get; private set; }

		public GardaVM GVM { get; private set; }

		public MainVM()
		{
			BVM = new BaumerVM();
			SVM = new SickVM();
			GVM = new GardaVM();
		}
	}
}
