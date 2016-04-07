using BGAPI2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		public int SleepAfterEachCapture { get; set; }
		public int SleepBeforeEachCapture { get; set; }
		public int SleepBeforeCaptureBatch { get; set; }

		public BaumerSystemParameters() : this(4, 10, 0, 0)
		{
		}

		public BaumerSystemParameters(
			int _BatchCaptureCount, 
			int _SleepAfterEachCapture,
			int _SleepBeforeEachCapture,
			int _SleepBeforeCaptureBatch)
		{
			BatchCaptureCount = _BatchCaptureCount;
			SleepAfterEachCapture = _SleepAfterEachCapture;
			SleepBeforeEachCapture = _SleepBeforeCaptureBatch;
			SleepBeforeCaptureBatch = _SleepBeforeCaptureBatch;

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

		public static int InternalBufferCount = 4;

		public BaumerSystem() { Status = BaumerStatus.Uninitiated; Parameters = new BaumerSystemParameters(); }
		public BaumerStatus Status { get; private set; }
		public BaumerSystemParameters Parameters { get; private set; }
		public bool IsProcessing { get; private set; }
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

			IsProcessing = false;
		}

		void AcquisitionStart()
		{
			Helpers.Log.LogThisInfo("DEVICE PARAMETER SETUP\n");
			Helpers.Log.LogThisInfo("######################\n\n");

			try
			{
				IsProcessing = true;
				//SET TRIGGER MODE OFF (FreeRun)
				mDevice.RemoteNodeList["TriggerMode"].Value = "Off";
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

		public bool StartBaumerCam()
		{
			if (Status == BaumerStatus.Ready)
				return true;

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
				}
				catch (BGAPI2.Exceptions.IException ex)
				{
					Helpers.Log.LogThisInfo("ExceptionType:    {0} \n", ex.GetType());
					Helpers.Log.LogThisInfo("ErrorDescription: {0} \n", ex.GetErrorDescription());
					Helpers.Log.LogThisInfo("in function:      {0} \n", ex.GetFunctionName());
				}
			}
			Helpers.Log.LogThisInfo("\n");


			Helpers.Log.LogThisInfo("RELEASE\n");
			Helpers.Log.LogThisInfo("#######\n\n");

			//Release buffers
			Helpers.Log.LogThisInfo("5.1.13   Releasing the resources\n");
			try
			{
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

			Helpers.Log.LogThisInfo("\nEnd\n\n");
			Helpers.Log.LogThisInfo("Input any number to close the program:\n");
			Status = BaumerStatus.Stopped;
			IsProcessing = false;
		}

		string fmt = "output_{0}.bmp";
		int c = 0;
		public bool CaptureAndSaveSingleFrame(int captureTrial = 5)
		{
			Status = BaumerStatus.Capturing;
			BGAPI2.Buffer mBufferFilled = null;
			int i = 0;
			try
			{
				IsProcessing = true;

				mDataStream.BufferList.FlushAllToInputQueue();
				for (i = 0; i < captureTrial; i++)
				{
					mBufferFilled = mDataStream.GetFilledBuffer(1000); // image polling timeout 1000 msec
					if (mBufferFilled == null)
					{
						System.Console.Write("Error: Buffer Timeout after 1000 msec\n");
					}
					else if (mBuffer.IsIncomplete == true)
					{
						System.Console.Write("Error: Image is incomplete\n");
						// queue buffer again
						mBufferFilled.QueueBuffer();
					}
					else
					{
						Helpers.Log.LogThisInfo(" Image {0, 5:d}@t={2, 5:d} received in memory address {1:X}\n", mBufferFilled.FrameID, (ulong)mBufferFilled.MemPtr, mBufferFilled.Timestamp);

						//create an image object from the filled buffer and convert it
						//BGAPI2.Image mTransformImage = null;
						//BGAPI2.Image mImage = imgProcessor.CreateImage((uint)mBufferFilled.Width, (uint)mBufferFilled.Height, (string)mBufferFilled.PixelFormat, mBufferFilled.MemPtr, (ulong)mBufferFilled.MemSize);
						//System.Console.Write("  mImage.Pixelformat:             {0}\n", mImage.PixelFormat);
						//System.Console.Write("  mImage.Width:                   {0}\n", mImage.Width);
						//System.Console.Write("  mImage.Height:                  {0}\n", mImage.Height);
						//System.Console.Write("  mImage.Buffer:                  {0:X8}\n", (ulong)mImage.Buffer);

						//double fBytesPerPixel = (double)mImage.NodeList["PixelFormatBytes"].Value;
						//System.Console.Write("  Bytes per image:                {0}\n", (long)((uint)mImage.Width * (uint)mImage.Height * fBytesPerPixel));
						//System.Console.Write("  Bytes per pixel:                {0}\n", fBytesPerPixel);

						int w = (int)mBufferFilled.Width, h = (int)mBufferFilled.Height;

						System.Drawing.Bitmap bb = Helpers.Utility.GetGrayBitmap(w, h, w * h, mBufferFilled.MemPtr);

						mBufferFilled.QueueBuffer();
						mDataStream.CancelGetFilledBuffer();
						bb.Save(string.Format(fmt, c++), System.Drawing.Imaging.ImageFormat.Bmp);

						//System.Drawing.Bitmap bb = Helpers.Utility.GetGrayBitmap((int)mImage.Width, (int)mImage.Height, (int)(mImage.Width * (uint)mImage.Height * fBytesPerPixel), mImage.Buffer);

						//bb.Save("output.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
						//break;
						//COPY UNMANAGED IMAGEBUFFER TO A MANAGED BYTE ARRAY
						//imageBufferCopy = new byte[(uint)((uint)mImage.Width * (uint)mImage.Height * fBytesPerPixel)];
						//Marshal.Copy(mImage.Buffer, imageBufferCopy, 0, (int)((int)mImage.Width * (int)mImage.Height * fBytesPerPixel));
						//ulong imageBufferAddress = (ulong)mImage.Buffer;

						//// display first 6 pixel values of first 6 lines of the image
						////========================================================================
						//System.Console.Write("  Address\n");
						//for (int j = 0; j < 6; j++) // first 6 lines
						//{
						//	imageBufferAddress = (ulong)mImage.Buffer + (ulong)((int)mImage.Width * (int)j * fBytesPerPixel);
						//	System.Console.Write("  {0:X8} ", imageBufferAddress);
						//	for (int k = 0; k < (int)(6 * fBytesPerPixel); k++) // bytes of first 6 pixels of that line
						//	{
						//		System.Console.Write(" {0:X2}", imageBufferCopy[(uint)(mImage.Width * j * fBytesPerPixel) + k]); // byte 
						//	}
						//	System.Console.Write(" ...\n");
						//}

						//System.Console.Write(" \n");
						//if (mImage != null) mImage.Release();
						//if (mTransformImage != null) mTransformImage.Release();

						// queue buffer again
						break;
					}
				}
			}
			catch (BGAPI2.Exceptions.IException ex)
			{
				System.Console.Write("ExceptionType:    {0} \n", ex.GetType());
				System.Console.Write("ErrorDescription: {0} \n", ex.GetErrorDescription());
				System.Console.Write("in function:      {0} \n", ex.GetFunctionName());
			}
			System.Console.Write("\n");
			Status = BaumerStatus.Ready;
			IsProcessing = false;
			return i <= captureTrial;
		}

		void CaptureAndSaveFrames(int frameCount)
		{
			Status = BaumerStatus.Capturing;
			Status = BaumerStatus.Ready;
		}
	}
}
