using BGAPI2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.DeviceSystems
{
	public enum BaumerStatus
	{
		Uninitiated,
		Ready,
		Stopped,
		Capturing
	}

	public class BaumerSystemParameters
	{
		public int BatchCaptureCount { get; set; }
		//public int SleepAfterEachCapture { get; set; }
		//public int SleepBeforeEachCapture { get; set; }
		//public int SleepBeforeCaptureBatch { get; set; }
		public double ExposureMax { get; set; }
		public double ExposureValue { get; set; }
		public double ExposureMin { get; set; }
		public readonly int ImagesPerCar;
		public readonly int CarsPerGroup;
		public string BasePath { get; set; }

		public int CarCount = 0;
		public int CarImageCount = 0;
		public int GroupCount = 0;

		public BaumerSystemParameters() : this(10)
		{
		}

		public void SetBasePath()
		{
			int count = 0;
			BasePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "output"), DateTime.Today.ToString("yyyy-MM-dd"));
			string basePath = BasePath;
			while (Directory.Exists(basePath))
			{
				basePath = string.Format("{1}-{0:D3}", count++, BasePath);
			} //while (Directory.Exists(BasePath));

			Directory.CreateDirectory(basePath);
			BasePath = basePath;

		}

		public BaumerSystemParameters(
			int _BatchCaptureCount)
		{
			SetBasePath();

			ImagesPerCar = 10;
			CarsPerGroup = 10;

			if (_BatchCaptureCount > BaumerSystem.InternalBufferCount)
				BatchCaptureCount = BaumerSystem.InternalBufferCount;
			else
				BatchCaptureCount = _BatchCaptureCount;

			//SleepAfterEachCapture = _SleepAfterEachCapture;
			//SleepBeforeEachCapture = _SleepBeforeCaptureBatch;
			//SleepBeforeCaptureBatch = _SleepBeforeCaptureBatch;

		}
	}
	class BaumerSystem
	{
		#region Variables
		//DECLARATIONS OF VARIABLES
		BGAPI2.ImageProcessor imgProcessor = null;

		BGAPI2.SystemList systemList = null;
		BGAPI2.System mSystem = null;
		string sSystemID = "";

		BGAPI2.InterfaceList interfaceList = null;
		BGAPI2.Interface mInterface = null;
		string sInterfaceID = "";

		BGAPI2.DeviceList deviceList = null;
		BGAPI2.Device mDevice = null;
		string sDeviceID = "";

		DataStreamList datastreamList = null;
		BGAPI2.DataStream mDataStream = null;
		string sDataStreamID = "";

		BufferList bufferList = null;
		BGAPI2.Buffer mBuffer = null;
		#endregion

		String sExposureNodeName = "";
		public static int InternalBufferCount = 10;
		public int TotalImageShot { get; private set; }

		public BaumerSystem()
		{
			Status = BaumerStatus.Uninitiated;
			Parameters = new BaumerSystemParameters();
			TotalImageShot = 0;
		}

		~BaumerSystem()
		{
			if(Status != BaumerStatus.Stopped || Status != BaumerStatus.Uninitiated)
				StopBaumerCam();
		}
		public BaumerStatus Status { get; private set; }
		public BaumerSystemParameters Parameters { get; private set; }
		public bool IsProcessing { get; private set; }
		public delegate void ImageFileWrittenDelegate(string path);
		public event ImageFileWrittenDelegate ImageFileWritten;
		bool LoadImageProcessor()
		{
			//LOAD IMAGE PROCESSOR
			try
			{
				IsProcessing = true;
				imgProcessor = ImageProcessor.Instance;
				Helpers.Log.LogThisInfo("ImageProcessor version:    {0} \n", imgProcessor.GetVersion());
				if (imgProcessor.NodeList.GetNodePresent("DemosaicingMethod") == true)
				{
					imgProcessor.NodeList["DemosaicingMethod"].Value = "NearestNeighbor"; // NearestNeighbor, Bilinear3x3, Baumer5x5
					Helpers.Log.LogThisInfo("    Demosaicing method:    {0} \n", (string)imgProcessor.NodeList["DemosaicingMethod"].Value);
				}
				Helpers.Log.LogThisInfo("\n");
				IsProcessing = false;
				return true;
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
				IsProcessing = false;
				return false;
			}

		}

		void OpenFirstSystemInCamList()
		{
			//COUNTING AVAILABLE SYSTEMS (TL producers)
			//OPEN THE FIRST SYSTEM IN THE LIST WITH A CAMERA CONNECTED
			try
			{
				IsProcessing = true;
				systemList = SystemList.Instance;
				systemList.Refresh();
				Helpers.Log.LogThisInfo("5.1.2   Detected systems:  {0}\n", systemList.Count);

				foreach (KeyValuePair<string, BGAPI2.System> sys_pair in BGAPI2.SystemList.Instance)
				{
					Helpers.Log.LogThisInfo("SYSTEM\n");
					Helpers.Log.LogThisInfo("######\n\n");

					try
					{
						sys_pair.Value.Open();
						Helpers.Log.LogThisInfo("5.1.3   Open next system \n");
						Helpers.Log.LogThisInfo("  5.2.1   System Name:     {0}\n", sys_pair.Value.FileName);
						Helpers.Log.LogThisInfo("          System Type:     {0}\n", sys_pair.Value.TLType);
						Helpers.Log.LogThisInfo("          System Version:  {0}\n", sys_pair.Value.Version);
						Helpers.Log.LogThisInfo("          System PathName: {0}\n\n", sys_pair.Value.PathName);
						sSystemID = sys_pair.Key;
						Helpers.Log.LogThisInfo("        Opened system - NodeList Information \n");
						Helpers.Log.LogThisInfo("          GenTL Version:   {0}.{1}\n\n", (long)sys_pair.Value.NodeList["GenTLVersionMajor"].Value, (long)sys_pair.Value.NodeList["GenTLVersionMinor"].Value);


						Helpers.Log.LogThisInfo("INTERFACE LIST\n");
						Helpers.Log.LogThisInfo("##############\n\n");

						try
						{
							interfaceList = sys_pair.Value.Interfaces;
							//COUNT AVAILABLE INTERFACES
							interfaceList.Refresh(100); // timeout of 100 msec

							//Helpers.Log.LogThisInfo("5.1.4   Detected interfaces: {0}\n", interfaceList.Count);
							////INTERFACE INFORMATION
							//foreach (KeyValuePair<string, BGAPI2.Interface> ifc_pair in interfaceList)
							//{
							//    Helpers.Log.LogThisInfo("  5.2.2   Interface ID:      {0}\n", ifc_pair.Value.Id);
							//    Helpers.Log.LogThisInfo("          Interface Type:    {0}\n", ifc_pair.Value.TLType);
							//    Helpers.Log.LogThisInfo("          Interface Name:    {0}\n\n", ifc_pair.Value.DisplayName);
							//}
						}
						catch (BGAPI2.Exceptions.IException ex)
						{
							Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
							Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
							Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
						}


						Helpers.Log.LogThisInfo("INTERFACE\n");
						Helpers.Log.LogThisInfo("#########\n\n");

						//OPEN THE NEXT INTERFACE IN THE LIST
						try
						{
							foreach (KeyValuePair<string, BGAPI2.Interface> ifc_pair in interfaceList)
							{
								try
								{
									Helpers.Log.LogThisInfo("5.1.5   Open interface \n");
									Helpers.Log.LogThisInfo("  5.2.2   Interface ID:      {0}\n", ifc_pair.Key);
									Helpers.Log.LogThisInfo("          Interface Type:    {0}\n", ifc_pair.Value.TLType);
									Helpers.Log.LogThisInfo("          Interface Name:    {0}\n", ifc_pair.Value.DisplayName);
									ifc_pair.Value.Open();
									//search for any camera is connetced to this interface
									deviceList = ifc_pair.Value.Devices;
									deviceList.Refresh(100);
									if (deviceList.Count == 0)
									{
										Helpers.Log.LogThisInfo("5.1.13   Close interface ({0} cameras found) \n\n", deviceList.Count);
										ifc_pair.Value.Close();
									}
									else
									{
										sInterfaceID = ifc_pair.Key;
										Helpers.Log.LogThisInfo("  \n");
										Helpers.Log.LogThisInfo("        Opened interface - NodeList Information \n");
										if (ifc_pair.Value.TLType == "GEV")
										{
											long iIPAddress = (long)ifc_pair.Value.NodeList["GevInterfaceSubnetIPAddress"].Value;
											Helpers.Log.LogThisInfo("          GevInterfaceSubnetIPAddress: {0}.{1}.{2}.{3}\n", (iIPAddress & 0xff000000) >> 24,
																															(iIPAddress & 0x00ff0000) >> 16,
																															(iIPAddress & 0x0000ff00) >> 8,
																															(iIPAddress & 0x000000ff));
											long iSubnetMask = (long)ifc_pair.Value.NodeList["GevInterfaceSubnetMask"].Value;
											Helpers.Log.LogThisInfo("          GevInterfaceSubnetMask:      {0}.{1}.{2}.{3}\n", (iSubnetMask & 0xff000000) >> 24,
																															(iSubnetMask & 0x00ff0000) >> 16,
																															(iSubnetMask & 0x0000ff00) >> 8,
																															(iSubnetMask & 0x000000ff));
										}
										if (ifc_pair.Value.TLType == "U3V")
										{
											//Helpers.Log.LogThisInfo("          NodeListCount:               {0}\n", ifc_pair.Value.NodeList.Count);
										}
										Helpers.Log.LogThisInfo("  \n");
										break;
									}
								}
								catch (BGAPI2.Exceptions.ResourceInUseException ex)
								{
									Helpers.Log.LogThisInfo(" Interface {0} already opened \n", ifc_pair.Key);
									Helpers.Log.LogThisInfo(" ResourceInUseException {0} \n", ex.GetErrorDescription());
								}
							}
						}
						catch (BGAPI2.Exceptions.IException ex)
						{
							Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
							Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
							Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
						}

						//if a camera is connected to the system interface then leave the system loop
						if (sInterfaceID != "")
						{
							break;
						}
					}
					catch (BGAPI2.Exceptions.ResourceInUseException ex)
					{
						Helpers.Log.LogThisInfo(" System {0} already opened \n", sys_pair.Key);
						Helpers.Log.LogThisInfo(" ResourceInUseException {0} \n", ex.GetErrorDescription());
					}
				}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}

			if (sSystemID == "")
			{
				Helpers.Log.LogThisInfo(" No System found \n");
				Helpers.Log.LogThisInfo(" Input any number to close the program:\n");
				//Console.Read();
				IsProcessing = false;
				return;
			}
			else
			{
				mSystem = systemList[sSystemID];
			}

			if (sInterfaceID == "")
			{
				Helpers.Log.LogThisInfo(" No Interface of TLType 'GEV' found \n");
				Helpers.Log.LogThisInfo("\nEnd\nInput any number to close the program:\n");
				//Console.Read();
				mSystem.Close();
				IsProcessing = false;
				return;
			}
			else
			{
				mInterface = interfaceList[sInterfaceID];
			}
			IsProcessing = false;
		}

		void OpenFirstCamera()
		{
			Helpers.Log.LogThisInfo("DEVICE LIST\n");
			Helpers.Log.LogThisInfo("###########\n\n");

			try
			{
				IsProcessing = true;
				//COUNTING AVAILABLE CAMERAS
				deviceList = mInterface.Devices;
				deviceList.Refresh(100);

				//Helpers.Log.LogThisInfo("5.1.6   Detected devices:         {0}\n", deviceList.Count);
				////DEVICE INFORMATION BEFORE OPENING
				//foreach (KeyValuePair<string, BGAPI2.Device> dev_pair in deviceList)
				//{
				//	Helpers.Log.LogThisInfo("  5.2.3   Device DeviceID:        {0}\n", dev_pair.Key);
				//	Helpers.Log.LogThisInfo("          Device Model:           {0}\n", dev_pair.Value.Model);
				//	Helpers.Log.LogThisInfo("          Device SerialNumber:    {0}\n", dev_pair.Value.SerialNumber);
				//	Helpers.Log.LogThisInfo("          Device Vendor:          {0}\n", dev_pair.Value.Vendor);
				//	Helpers.Log.LogThisInfo("          Device TLType:          {0}\n", dev_pair.Value.TLType);
				//	Helpers.Log.LogThisInfo("          Device AccessStatus:    {0}\n", dev_pair.Value.AccessStatus);
				//	Helpers.Log.LogThisInfo("          Device UserID:          {0}\n\n", dev_pair.Value.DisplayName);
				//}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}

			Helpers.Log.LogThisInfo("DEVICE\n");
			Helpers.Log.LogThisInfo("######\n\n");

			//OPEN THE FIRST CAMERA IN THE LIST
			try
			{
				foreach (KeyValuePair<string, BGAPI2.Device> dev_pair in deviceList)
				{
					try
					{
						Helpers.Log.LogThisInfo("5.1.7   Open first device \n");
						Helpers.Log.LogThisInfo("          Device DeviceID:        {0}\n", dev_pair.Value.Id);
						Helpers.Log.LogThisInfo("          Device Model:           {0}\n", dev_pair.Value.Model);
						Helpers.Log.LogThisInfo("          Device SerialNumber:    {0}\n", dev_pair.Value.SerialNumber);
						Helpers.Log.LogThisInfo("          Device Vendor:          {0}\n", dev_pair.Value.Vendor);
						Helpers.Log.LogThisInfo("          Device TLType:          {0}\n", dev_pair.Value.TLType);
						Helpers.Log.LogThisInfo("          Device AccessStatus:    {0}\n", dev_pair.Value.AccessStatus);
						Helpers.Log.LogThisInfo("          Device UserID:          {0}\n\n", dev_pair.Value.DisplayName);
						dev_pair.Value.Open();
						sDeviceID = dev_pair.Key;
						Helpers.Log.LogThisInfo("        Opened device - RemoteNodeList Information \n");
						Helpers.Log.LogThisInfo("          Device AccessStatus:    {0}\n", dev_pair.Value.AccessStatus);

						//SERIAL NUMBER
						if (dev_pair.Value.RemoteNodeList.GetNodePresent("DeviceSerialNumber") == true)
							Helpers.Log.LogThisInfo("          DeviceSerialNumber:     {0}\n", (string)dev_pair.Value.RemoteNodeList["DeviceSerialNumber"].Value);
						else if (dev_pair.Value.RemoteNodeList.GetNodePresent("DeviceID") == true)
							Helpers.Log.LogThisInfo("          DeviceID (SN):          {0}\n", (string)dev_pair.Value.RemoteNodeList["DeviceID"].Value);
						else
							Helpers.Log.LogThisInfo("          SerialNumber:           Not Available \n");

						//DISPLAY DEVICEMANUFACTURERINFO
						if (dev_pair.Value.RemoteNodeList.GetNodePresent("DeviceManufacturerInfo") == true)
							Helpers.Log.LogThisInfo("          DeviceManufacturerInfo: {0}\n", (string)dev_pair.Value.RemoteNodeList["DeviceManufacturerInfo"].Value);

						//DISPLAY DEVICEFIRMWAREVERSION OR DEVICEVERSION
						if (dev_pair.Value.RemoteNodeList.GetNodePresent("DeviceFirmwareVersion") == true)
							Helpers.Log.LogThisInfo("          DeviceFirmwareVersion:  {0}\n", (string)dev_pair.Value.RemoteNodeList["DeviceFirmwareVersion"].Value);
						else if (dev_pair.Value.RemoteNodeList.GetNodePresent("DeviceVersion") == true)
							Helpers.Log.LogThisInfo("          DeviceVersion:          {0}\n", (string)dev_pair.Value.RemoteNodeList["DeviceVersion"].Value);
						else
							Helpers.Log.LogThisInfo("          DeviceVersion:          Not Available \n");

						if (dev_pair.Value.TLType == "GEV")
						{
							Helpers.Log.LogThisInfo("          GevCCP:                 {0}\n", (string)dev_pair.Value.RemoteNodeList["GevCCP"].Value);
							Helpers.Log.LogThisInfo("          GevCurrentIPAddress:    {0}.{1}.{2}.{3}\n", ((long)dev_pair.Value.RemoteNodeList["GevCurrentIPAddress"].Value & 0xff000000) >> 24, ((long)dev_pair.Value.RemoteNodeList["GevCurrentIPAddress"].Value & 0x00ff0000) >> 16, ((long)dev_pair.Value.RemoteNodeList["GevCurrentIPAddress"].Value & 0x0000ff00) >> 8, ((long)dev_pair.Value.RemoteNodeList["GevCurrentIPAddress"].Value & 0x000000ff));
							Helpers.Log.LogThisInfo("          GevCurrentSubnetMask:   {0}.{1}.{2}.{3}\n", ((long)dev_pair.Value.RemoteNodeList["GevCurrentSubnetMask"].Value & 0xff000000) >> 24, ((long)dev_pair.Value.RemoteNodeList["GevCurrentSubnetMask"].Value & 0x00ff0000) >> 16, ((long)dev_pair.Value.RemoteNodeList["GevCurrentSubnetMask"].Value & 0x0000ff00) >> 8, ((long)dev_pair.Value.RemoteNodeList["GevCurrentSubnetMask"].Value & 0x000000ff));
						}
						Helpers.Log.LogThisInfo("          \n");
						break;
					}
					catch (BGAPI2.Exceptions.ResourceInUseException ex)
					{
						Helpers.Log.LogThisInfo(" Device {0} already opened  \n", dev_pair.Key);
						Helpers.Log.LogThisInfo(" ResourceInUseException {0} \n", ex.GetErrorDescription());
					}
					catch (BGAPI2.Exceptions.AccessDeniedException ex)
					{
						Helpers.Log.LogThisInfo(" Device {0} already opened \n", dev_pair.Key);
						Helpers.Log.LogThisInfo(" AccessDeniedException {0} \n", ex.GetErrorDescription());
					}
				}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}

			if (sDeviceID == "")
			{
				Helpers.Log.LogThisInfo(" No Device found \n");
				Helpers.Log.LogThisInfo("\nEnd\nInput any number to close the program:\n");
				//Console.Read();
				mInterface.Close();
				mSystem.Close();
				IsProcessing = false;
				return;
			}
			else
			{
				mDevice = deviceList[sDeviceID];
			}

			if (mDevice.GetRemoteNodeList().GetNodePresent("ExposureTime"))
			{
				sExposureNodeName = "ExposureTime";
			}
			else if (mDevice.GetRemoteNodeList().GetNodePresent("ExposureTimeAbs"))
			{
				sExposureNodeName = "ExposureTimeAbs";
			}

			//get current value and limits
			Parameters.ExposureValue = (double)mDevice.RemoteNodeList[sExposureNodeName].Value;
			Parameters.ExposureMin = (double)mDevice.RemoteNodeList[sExposureNodeName].Min;
			Parameters.ExposureMax = (double)mDevice.RemoteNodeList[sExposureNodeName].Max;

			IsProcessing = false;
		}

		void AcquisitionStart()
		{
			Helpers.Log.LogThisInfo("DEVICE PARAMETER SETUP\n");
			Helpers.Log.LogThisInfo("######################\n\n");

			IsProcessing = true;
			try
			{
				//SET TRIGGER MODE OFF (FreeRun)
				mDevice.RemoteNodeList["TriggerMode"].Value = "On";
				Helpers.Log.LogThisInfo("         TriggerMode:             {0}\n", (string)mDevice.RemoteNodeList["TriggerMode"].Value);
				Helpers.Log.LogThisInfo("  \n");
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}


			Helpers.Log.LogThisInfo("DATA STREAM LIST\n");
			Helpers.Log.LogThisInfo("################\n\n");

			try
			{
				//COUNTING AVAILABLE DATASTREAMS
				datastreamList = mDevice.DataStreams;
				datastreamList.Refresh();

				//Helpers.Log.LogThisInfo("5.1.8   Detected datastreams:     {0}\n", datastreamList.Count);
				////DATASTREAM INFORMATION BEFORE OPENING
				//foreach (KeyValuePair<string, BGAPI2.DataStream> dst_pair in datastreamList)
				//{
				//    Helpers.Log.LogThisInfo("  5.2.4   DataStream ID:          {0}\n\n", dst_pair.Key);
				//}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}


			Helpers.Log.LogThisInfo("DATA STREAM\n");
			Helpers.Log.LogThisInfo("###########\n\n");

			//OPEN THE FIRST DATASTREAM IN THE LIST
			try
			{
				foreach (KeyValuePair<string, BGAPI2.DataStream> dst_pair in datastreamList)
				{
					Helpers.Log.LogThisInfo("5.1.9   Open first datastream \n");
					Helpers.Log.LogThisInfo("          DataStream ID:          {0}\n\n", dst_pair.Key);
					dst_pair.Value.Open();
					sDataStreamID = dst_pair.Key;
					Helpers.Log.LogThisInfo("        Opened datastream - NodeList Information \n");
					Helpers.Log.LogThisInfo("          StreamAnnounceBufferMinimum:  {0}\n", dst_pair.Value.NodeList["StreamAnnounceBufferMinimum"].Value);
					if (dst_pair.Value.TLType == "GEV")
					{
						Helpers.Log.LogThisInfo("          StreamDriverModel:            {0}\n", dst_pair.Value.NodeList["StreamDriverModel"].Value);
					}
					Helpers.Log.LogThisInfo("  \n");
					break;
				}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}

			if (sDataStreamID == "")
			{
				Helpers.Log.LogThisInfo(" No DataStream found \n");
				Helpers.Log.LogThisInfo("\nEnd\nInput any number to close the program:\n");
				//Console.Read();
				mDevice.Close();
				mInterface.Close();
				mSystem.Close();
				IsProcessing = false;
				return;
			}
			else
			{
				mDataStream = datastreamList[sDataStreamID];
			}


			Helpers.Log.LogThisInfo("BUFFER LIST\n");
			Helpers.Log.LogThisInfo("###########\n\n");

			try
			{
				//BufferList
				bufferList = mDataStream.BufferList;

				// 4 buffers using internal buffer mode
				for (int i = 0; i < InternalBufferCount; i++)
				{
					mBuffer = new BGAPI2.Buffer();
					bufferList.Add(mBuffer);
					mBuffer.QueueBuffer();
				}
				Helpers.Log.LogThisInfo("5.1.10   Announced buffers:       {0} using {1} [bytes]\n", bufferList.AnnouncedCount, mBuffer.MemSize * bufferList.AnnouncedCount);
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}
			Helpers.Log.LogThisInfo("\n");

			Helpers.Log.LogThisInfo("CAMERA START\n");
			Helpers.Log.LogThisInfo("############\n\n");

			mDataStream.RegisterNewBufferEvent(BGAPI2.Events.EventMode.EVENT_HANDLER);
			System.Console.Write("        Register Event Mode to:   {0}\n\n", mDataStream.EventMode.ToString());
			mDataStream.NewBufferEvent += new BGAPI2.Events.DataStreamEventControl.NewBufferEventHandler(mDataStream_NewBufferEvent);

			//START DATASTREAM ACQUISITION
			try
			{
				mDataStream.StartAcquisition();
				Helpers.Log.LogThisInfo("5.1.12   DataStream started \n");
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}

			//START CAMERA
			try
			{
				mDevice.RemoteNodeList["AcquisitionStart"].Execute();
				Helpers.Log.LogThisInfo("5.1.12   {0} started \n", mDevice.Model);
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
				Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
				Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
			}
		}
		
		public void DoCapture()
		{
			if (Parameters.GroupCount <= 0) Parameters.GroupCount = 1;
			if (Parameters.CarCount >= Parameters.CarsPerGroup)
			{
				Parameters.CarCount = 0;
				Parameters.GroupCount++;
			}
			Parameters.CarImageCount = 1;
			Parameters.CarCount++;

			string grpCarPath = Path.Combine(Parameters.BasePath, string.Format("Group-{0:D6}\\car-{1:D4}", Parameters.GroupCount, Parameters.CarCount));
			Directory.CreateDirectory(grpCarPath);
			mDataStream.BufferList.FlushAllToInputQueue();
		}
		//EVENT HANDLER
		void mDataStream_NewBufferEvent(object sender, BGAPI2.Events.NewBufferEventArgs mDSEvent)
		{
			if (Parameters.CarImageCount <= 0 || Parameters.CarImageCount > InternalBufferCount || Parameters.CarImageCount > Parameters.BatchCaptureCount) return;

			Helpers.Log.LogThisInfo(" [event of {0}] ", ((BGAPI2.DataStream)sender).Parent.Model); // device
			Status = BaumerStatus.Capturing;
			int i = 0;
			IsProcessing = true;

			//mDevice.RemoteNodeList["TriggerMode"].Value = "Off";
			Helpers.Log.LogThisInfo("==>> Trigger Mode: " + mDevice.RemoteNodeList["TriggerMode"].Value);

			try
			{
				BGAPI2.Buffer mBufferFilled = null;
				mBufferFilled = mDSEvent.BufferObj;
				if (mBufferFilled == null)
				{
					System.Diagnostics.Debug.WriteLine("Error: Buffer Timeout after 1000 msec");
				}
				else if (mBufferFilled.IsIncomplete == true)
				{
					Helpers.Log.LogThisError("Error: Image is incomplete, Trial: {0}", i);
					// queue buffer again
					mBufferFilled.QueueBuffer();
				}
				else
				{
					int w = (int)mBufferFilled.Width, h = (int)mBufferFilled.Height;

					System.Drawing.Bitmap bb = Helpers.Utility.GetGrayBitmap(w, h, w * h, mBufferFilled.MemPtr);

					string grpCarPath = Path.Combine(Parameters.BasePath, string.Format("Group-{0:D6}\\car-{1:D4}", Parameters.GroupCount, Parameters.CarCount));
					string destFilePath = Path.Combine(grpCarPath, string.Format("car-image-{0:D4}.bmp", Parameters.CarImageCount++));
					Task.Factory.StartNew(() =>
					{
						bb.Save(destFilePath, System.Drawing.Imaging.ImageFormat.Bmp);
					});


					//event
					TotalImageShot++;
					if (ImageFileWritten != null)
						ImageFileWritten(destFilePath);

					mBufferFilled.QueueBuffer();
				}
			}
			catch (BGAPI2.Exceptions.LowLevelException exx)
			{
				Helpers.Log.LogThisError("-->ExceptionType:    {0} ", exx.GetType());
				Helpers.Log.LogThisError("-->Msg:    {0} ", exx.Message);
				Helpers.Log.LogThisError("-->ErrorDescription: {0} ", exx.GetErrorDescription());
				Helpers.Log.LogThisError("-->in function:      {0} ", exx.GetFunctionName());
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				Helpers.Log.LogThisError("-->ExceptionType:    {0} ", ex.GetType());
				Helpers.Log.LogThisError("-->Msg:    {0} ", ex.Message);
				Helpers.Log.LogThisError("-->ErrorDescription: {0} ", ex.GetErrorDescription());
				Helpers.Log.LogThisError("-->in function:      {0} ", ex.GetFunctionName());
			}
			Status = BaumerStatus.Ready;
			return;
		}

		public bool StartBaumerCam()
		{
			if (Status == BaumerStatus.Ready)
				return true;

			if (Status == BaumerStatus.Stopped)
				Parameters.SetBasePath();

			try
			{
				if (LoadImageProcessor())
				{
					OpenFirstSystemInCamList();
					OpenFirstCamera();
					AcquisitionStart();
				}
				else
				{
					IsProcessing = false;
					return false;
				}
			}
			catch (Exception ex)
			{
				Helpers.Log.LogThisError("General Exception: {0}", ex.Message);
				return false;
			}

			Status = BaumerStatus.Ready;
			IsProcessing = false;
			return true;
		}

		public void StopBaumerCam()
		{
			//STOP CAMERA
			if (mDevice != null)
			{
				try
				{
					IsProcessing = true;
					if (mDevice.RemoteNodeList.GetNodePresent("AcquisitionAbort") == true)
					{
						mDevice.RemoteNodeList["AcquisitionAbort"].Execute();
						Helpers.Log.LogThisInfo("5.1.12   {0} aborted\n", mDevice.Model);
					}

					mDevice.RemoteNodeList["AcquisitionStop"].Execute();
					Helpers.Log.LogThisInfo("5.1.12   {0} stopped\n", mDevice.Model);
					Helpers.Log.LogThisInfo("\n");

					String sExposureNodeName = "";
					if (mDevice.GetRemoteNodeList().GetNodePresent("ExposureTime"))
					{
						sExposureNodeName = "ExposureTime";
					}
					else if (mDevice.GetRemoteNodeList().GetNodePresent("ExposureTimeAbs"))
					{
						sExposureNodeName = "ExposureTimeAbs";
					}

					Helpers.Log.LogThisInfo("         ExposureTime:                   {0} [{1}]\n", (double)mDevice.RemoteNodeList[sExposureNodeName].Value, (string)mDevice.RemoteNodeList[sExposureNodeName].Unit);
					if (mDevice.TLType == "GEV")
					{
						if (mDevice.RemoteNodeList.GetNodePresent("DeviceStreamChannelPacketSize") == true)
							Helpers.Log.LogThisInfo("         DeviceStreamChannelPacketSize:  {0} [bytes]\n", (long)mDevice.RemoteNodeList["DeviceStreamChannelPacketSize"].Value);
						else
							Helpers.Log.LogThisInfo("         GevSCPSPacketSize:              {0} [bytes]\n", (long)mDevice.RemoteNodeList["GevSCPSPacketSize"].Value);
						Helpers.Log.LogThisInfo("         GevSCPD (PacketDelay):          {0} [tics]\n", (long)mDevice.RemoteNodeList["GevSCPD"].Value);
					}
					Helpers.Log.LogThisInfo("\n");
					Status = BaumerStatus.Stopped;
				}
				catch (BGAPI2.Exceptions.IException ex)
				{
					Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
					Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
					Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
				}
			}
			//STOP DataStream acquisition & release buffers
			if (mDataStream != null)
			{
				try
				{
					if (mDataStream.TLType == "GEV")
					{
						//DataStream Statistics
						Helpers.Log.LogThisInfo("         DataStream Statistics \n");
						Helpers.Log.LogThisInfo("           GoodFrames:            {0}\n", (long)mDataStream.NodeList["GoodFrames"].Value);
						Helpers.Log.LogThisInfo("           CorruptedFrames:       {0}\n", (long)mDataStream.NodeList["CorruptedFrames"].Value);
						Helpers.Log.LogThisInfo("           LostFrames:            {0}\n", (long)mDataStream.NodeList["LostFrames"].Value);
						Helpers.Log.LogThisInfo("           ResendRequests:        {0}\n", (long)mDataStream.NodeList["ResendRequests"].Value);
						Helpers.Log.LogThisInfo("           ResendPackets:         {0}\n", (long)mDataStream.NodeList["ResendPackets"].Value);
						Helpers.Log.LogThisInfo("           LostPackets:           {0}\n", (long)mDataStream.NodeList["LostPackets"].Value);
						Helpers.Log.LogThisInfo("           Bandwidth:             {0}\n", (long)mDataStream.NodeList["Bandwidth"].Value);
						Helpers.Log.LogThisInfo("\n");
					}
					if (mDataStream.TLType == "U3V")
					{
						//DataStream Statistics
						Helpers.Log.LogThisInfo("         DataStream Statistics \n");
						Helpers.Log.LogThisInfo("           GoodFrames:            {0}\n", (long)mDataStream.NodeList["GoodFrames"].Value);
						Helpers.Log.LogThisInfo("           CorruptedFrames:       {0}\n", (long)mDataStream.NodeList["CorruptedFrames"].Value);
						Helpers.Log.LogThisInfo("           LostFrames:            {0}\n", (long)mDataStream.NodeList["LostFrames"].Value);
						Helpers.Log.LogThisInfo("\n");
					}
					mDataStream.StopAcquisition();
					Helpers.Log.LogThisInfo("5.1.12   DataStream stopped \n");
					bufferList.DiscardAllBuffers();

					// RESET EVENT MODE TO DISABLED
					//=============================
					mDataStream.UnregisterNewBufferEvent();
					mDataStream.RegisterNewBufferEvent(BGAPI2.Events.EventMode.DISABLED);
					BGAPI2.Events.EventMode currentEventMode = mDataStream.EventMode;
					System.Console.Write("        Unregister Event Mode:    {0}\n", mDataStream.EventMode.ToString());

					Helpers.Log.LogThisInfo("RELEASE\n");
					Helpers.Log.LogThisInfo("#######\n\n");
					Helpers.Log.LogThisInfo("5.1.13   Releasing the resources\n");

					while (bufferList.Count > 0)
					{
						mBuffer = (BGAPI2.Buffer)bufferList.Values.First();
						bufferList.RevokeBuffer(mBuffer);
					}
					Helpers.Log.LogThisInfo("         buffers after revoke:    {0}\n", bufferList.Count);

					mDataStream.Close();
					mDevice.Close();
					mInterface.Close();
					mSystem.Close();

				}
				catch (BGAPI2.Exceptions.IException ex)
				{
					Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
					Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
					Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
				}
			}
			Helpers.Log.LogThisInfo("\n");


			Helpers.Log.LogThisInfo("\nEnd\n\n");
			Helpers.Log.LogThisInfo("Input any number to close the program:\n");
			Status = BaumerStatus.Stopped;
			IsProcessing = false;
		}

		public int SetExposure(double exposurevalue)
		{
			try
			{
				// check new value is within range
				if (exposurevalue < Parameters.ExposureMin)
					exposurevalue = Parameters.ExposureMin;

				if (exposurevalue > Parameters.ExposureMax)
					exposurevalue = Parameters.ExposureMax;

				mDevice.RemoteNodeList[sExposureNodeName].Value = exposurevalue;
				Parameters.ExposureValue = exposurevalue;

				//recheck new exposure is set
				System.Console.Write("          set value to:           {0}\n\n", (double)mDevice.RemoteNodeList[sExposureNodeName].Value);
				return (int)Parameters.ExposureValue;
			}
			catch (Exception ex)
			{
				return 0;
			}
		}

		//public bool CaptureInBatch(int captureTrial = 50)
		//{
		//	Status = BaumerStatus.Capturing;
		//	BGAPI2.Buffer mBufferFilled = null;
		//	int i = 0;
		//	IsProcessing = true;
		//	if (Parameters.SleepBeforeCaptureBatch != 0)
		//	{
		//		System.Threading.Thread.Sleep(Parameters.SleepBeforeCaptureBatch);
		//	}

		//	//mDevice.RemoteNodeList["TriggerMode"].Value = "Off";
		//	Helpers.Log.LogThisInfo("==>> Trigger Mode: " + mDevice.RemoteNodeList["TriggerMode"].Value);

		//	if (Parameters.CarCount > Parameters.CarsPerGroup) { Parameters.CarCount = 1; Parameters.GroupCount++; }
		//	Parameters.CarImageCount = 1;

		//	string grpCarPath = Path.Combine(Parameters.BasePath, string.Format("Group-{0}\\car-{1}", Parameters.GroupCount, Parameters.CarCount++));
		//	Directory.CreateDirectory(grpCarPath);
		//	for (int x = 0; x < Parameters.BatchCaptureCount; x++)
		//	{
		//		for (i = 0; i < captureTrial; i++)
		//		{
		//			try
		//			{

		//				if (Parameters.SleepBeforeEachCapture != 0)
		//				{
		//					System.Threading.Thread.Sleep(Parameters.SleepBeforeEachCapture);
		//				}

		//				mDataStream.BufferList.FlushAllToInputQueue();

		//				//Helpers.Log.Logthis("Trial {0}", i);
		//				mBufferFilled = mDataStream.GetFilledBuffer(1000); // image polling timeout 1000 msec
		//				if (mBufferFilled == null)
		//				{
		//					System.Diagnostics.Debug.WriteLine("Error: Buffer Timeout after 1000 msec");
		//				}
		//				else if (mBufferFilled.IsIncomplete == true)
		//				{
		//					Helpers.Log.LogThisError("Error: Image is incomplete, Trial: {0}", i);
		//					// queue buffer again
		//					mBufferFilled.QueueBuffer();
		//				}
		//				else if (mBufferFilled.NewData)
		//				{
		//					//Helpers.Log.LogThisInfo(" Image {0, 5:d}@t={2, 5:d} received in memory address {1:X}\n", mBufferFilled.FrameID, (ulong)mBufferFilled.MemPtr, mBufferFilled.Timestamp);

		//					int w = (int)mBufferFilled.Width, h = (int)mBufferFilled.Height;

		//					System.Drawing.Bitmap bb = Helpers.Utility.GetGrayBitmap(w, h, w * h, mBufferFilled.MemPtr);

		//					string destFilePath = Path.Combine(grpCarPath, string.Format("car-image-{0}.bmp", Parameters.CarImageCount++));
		//					Task.Factory.StartNew(() =>
		//					{
		//						bb.Save(destFilePath, System.Drawing.Imaging.ImageFormat.Bmp);
		//					});


		//					//event
		//					TotalImageShot++;
		//					if (ImageFileWritten != null)
		//						ImageFileWritten(destFilePath);

		//					mBufferFilled.QueueBuffer();
		//					break;
		//				}
		//				//mDataStream.BufferList.FlushInputToOutputQueue();

		//				if (Parameters.SleepAfterEachCapture != 0)
		//				{
		//					System.Threading.Thread.Sleep(Parameters.SleepAfterEachCapture);
		//					//Task.Delay(Parameters.SleepAfterEachCapture);
		//				}
		//			}
		//			catch (BGAPI2.Exceptions.LowLevelException exx)
		//			{
		//				Helpers.Log.LogThisError("-->ExceptionType:    {0} ", exx.GetType());
		//				Helpers.Log.LogThisError("-->Msg:    {0} ", exx.Message);
		//				Helpers.Log.LogThisError("-->ErrorDescription: {0} ", exx.GetErrorDescription());
		//				Helpers.Log.LogThisError("-->in function:      {0} ", exx.GetFunctionName());
		//			}
		//			catch (BGAPI2.Exceptions.IException ex)
		//			{
		//				Helpers.Log.LogThisError("-->ExceptionType:    {0} ", ex.GetType());
		//				Helpers.Log.LogThisError("-->Msg:    {0} ", ex.Message);
		//				Helpers.Log.LogThisError("-->ErrorDescription: {0} ", ex.GetErrorDescription());
		//				Helpers.Log.LogThisError("-->in function:      {0} ", ex.GetFunctionName());
		//			}
		//		}
		//	}

		//	System.Console.Write("\n");
		//	Status = BaumerStatus.Ready;
		//	IsProcessing = false;
		//	return i <= captureTrial;
		//}

	}
}
