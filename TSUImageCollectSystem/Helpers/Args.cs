using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.Helpers
{
	using Mono.Options;
	using System.IO;
	public class Args
	{
		OptionSet _opset;
		string[] _args;
		StringBuilder sb = new StringBuilder();

		static Args _defaultArgs = new Args(Environment.GetCommandLineArgs());
		public static Args DefaultArgs
		{
			get { return _defaultArgs; }
		}

		private Args(string[] arg)
		{
			_args = arg;
			IPAddress = "192.168.0.2";
			ShotPerCar = 15;
			Exposure = 250;
			DelayOfCar = 1500;
			Port = 2111;
			DataCheckCount = 7;
			CaptureDelay = 0;
			TriggerDelay = 0;
			OutputPath = "";
			TriggerMode = true;

			InitOpset();
			_opset.Parse(arg);
			sb.Append("Usage: TSUImageCollectSystem [OPTIONS]\n");
			sb.Append("Options:\n");
			_opset.WriteOptionDescriptions(new StringWriter(sb));
			sb.Append("\nOptions can be in ANY order\n");
			sb.Append(@"Example:
TSUImageCollectSystem --ip=192.168.100.2 --shots=15 --sdelay=1500
TSUImageCollectSystem -e=4000 --cdelay=50 --output=E:\
TSUImageCollectSystem -p=2112 -i=192.168.0.2 --check=10");
		}

		public string AllArgsAndDesc
		{
			get { return sb.ToString(); }
		}

		public string OutputPath
		{ get; private set; }

		public string IPAddress
		{ get; private set; }

		public int CaptureDelay
		{ get; private set; }

		public int DataCheckCount
		{ get; private set; }

		public int ShotPerCar
		{ get; private set; }

		public int Exposure
		{ get; private set; }

		public int TriggerDelay
		{ get; private set; }

		public bool TriggerMode
		{ get; private set; }

		public int DelayOfCar
		{ get; private set; }

		public int Port
		{ get; private set; }

		public void InitOpset()
		{
			_opset = new OptionSet()
				{
					{
						"o|output=","Output Path where the images will be stored",
						(val)=>
						{
							OutputPath = val;
						}
					},
					{
						"i|ip=","SICK sensor IP Address",
						(val)=>
						{
							IPAddress = val;
						}
					},
					{
						"p|port=","SICK sensor IP Port",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								Port = vl;
							}
						}
					},
					{
						"s|shots=","Image capture per car",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								ShotPerCar = vl;
							}
						}
					},
					{
						"d|sdelay=","Delay(ms) between Cars (for SICK Sensor)",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								DelayOfCar = vl;
							}
						}
					},
					{
						"b|cdelay=","Delay(ms) before Baumer capture command",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								CaptureDelay = vl;
							}
						}
					},
					{
						"c|check=","Number of data check for car detection",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								DataCheckCount = vl;
							}
						}
					},
					{
						"e|exposure=","Exposure(us) for Baumer Camera",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								Exposure = vl;
							}
						}
					},
					{
						"t|tdelay=","Triggering delay(us) for Baumer Camera",
						(string val)=>
						{
							int vl;
							if(Int32.TryParse(val, out vl))
							{
								TriggerDelay = vl;
							}
						}
					},
					{
						"trigoff","Turn off Trigger Mode",
						(v)=>
						{
							TriggerMode = false;
						}
					}
				};
		}
	}
}
