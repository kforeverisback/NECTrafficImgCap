using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Trace = System.Diagnostics.Debug;
namespace TSUImageCollectSystem.DeviceSystems
{
	class channel_data_16b
	{
		public string content;
		//public char[] content = new char[6];
		public float scale_factor;
		public float scale_factor_offset;
		public UInt32 start_angle;
		public UInt32 steps;
		public UInt32 amnt_data;
		public UInt32[] data = new uint[256]; //MAX 512 DATA
		public channel_data_16b Copy()
		{
			channel_data_16b cd = (channel_data_16b)MemberwiseClone();
			cd.content = string.Copy(content);
			data.CopyTo(cd.data, 0);
			return cd;
		}
	}

	class SICKSystem
	{
		Socket _sickSock;
		IPEndPoint _ipAndPort;
		CancellationTokenSource _cancelTokenS;

		/// <summary>
		/// 
		/// </summary>
		//***************************************
		public channel_data_16b m_reference = new channel_data_16b();
		channel_data_16b m_current_data = new channel_data_16b();
		channel_data_16b m_previous_data = new channel_data_16b();
		//****************************************
		public bool SensorSystemAvailable { get; private set; }
		public bool SetReference { get; set; }
		public bool WillNotify { get; set; }
		public int MaxPointCount { get; set; }
		public int Threshold { get; set; }

		public int Port { get; set; }

		public delegate void SICKCarIncomingNotiHandler();
		public event SICKCarIncomingNotiHandler SICKCarIncoming;

		public delegate void SICKDataNotificationHandler(channel_data_16b data);
		public event SICKDataNotificationHandler SICKDataNotification;

		//public event SICKDataNotificationHandler SICKReferenceSet;

		public SICKSystem()
		{
			Threshold = 0x20;
			Port = Helpers.Args.DefaultArgs.Port;
			MaxPointCount = Helpers.Args.DefaultArgs.DataCheckCount;
			SetReference = false;
			WillNotify = true;
		}

		~SICKSystem()
		{ Disconnect(); }

		public Task<bool> Connect(string ipAddress)
		{
			return Task<bool>.Factory.StartNew(() =>
			{
				if (_sickSock == null)
				{
					_sickSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}
				else if (!_sickSock.Connected)
				{
					_sickSock.Dispose();
					_sickSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}

				lock (_sickSock)
				{
					if (SensorSystemAvailable)
						return true;
					_cancelTokenS = new CancellationTokenSource();
					//IPAddress[] ipaddr = Dns.GetHostAddresses("169.254.3.172");
					IPAddress[] ipaddr = Dns.GetHostAddresses(ipAddress);
					//IPAddress[] ipaddr = Dns.GetHostAddresses("168.169.170.200");
					if (ipaddr.Length > 0)
					{
						IPAddress ip = ipaddr[0];
						_ipAndPort = new IPEndPoint(ip, Port);
						try
						{
							_sickSock.Connect(_ipAndPort);
						}
						catch (SocketException sex)
						{
							Helpers.Log.LogThisWarn("Socket Exception: {0}", sex.Message);
							return false;
						}
						catch (Exception ex)
						{
							Helpers.Log.LogThisWarn("Exception: {0}", ex.Message);
							return false;
						}
						Task.Factory.StartNew(() =>
						{
							DoSensorDataProcessing(_cancelTokenS.Token, _sickSock);
						}, _cancelTokenS.Token);

						return true;
					}
					else
					{
						return false;
					}
				}
				
			});

		}

		public void Disconnect()
		{
			
			_cancelTokenS?.Cancel();
			//Thread.Sleep(100);
			//_sickSock.Disconnect(true);
			SensorSystemAvailable = false;
			if(_sickSock != null && _sickSock.Connected)
			{
				_sickSock?.Disconnect(true);
			}
		}

		static byte[] get_encoded_bytes(string str)
		{
			return Encoding.ASCII.GetBytes("\x02" + str + "\x03");
		}

		static channel_data_16b get_only_1_scan_data(string data)
		{
			channel_data_16b chdata = new channel_data_16b();
			string newData = data.Substring(84);
			string[] splitLines = newData.Split(" ".ToCharArray());
			chdata.content = splitLines[0];

			float.TryParse(splitLines[1], out chdata.scale_factor);
			float.TryParse(splitLines[2], out chdata.scale_factor_offset);
			chdata.start_angle = Convert.ToUInt32(splitLines[3], 16);
			chdata.steps = Convert.ToUInt32(splitLines[4], 16);
			chdata.amnt_data = Convert.ToUInt32(splitLines[5], 16);

			if (chdata.amnt_data != 0)
			{
				for (int i = 0; i < chdata.amnt_data; i++)
				{
					chdata.data[i] = Convert.ToUInt32(splitLines[6 + i], 16);
				}
			}
			return chdata;
		}

		private class ConsecutiveFieldEvalCheck
		{

			int number_of_3E = 0;
			CancellationTokenSource m_ct = new CancellationTokenSource();

			public bool CheckIsCar(byte[] data, int max_data_count)
			{
				bool is3E = data[64] == '3' && data[65] == 'E';
				Trace.WriteLine("Checking...");
				if (is3E)
				{
					m_ct.Cancel();
					Task.Factory.StartNew(async () =>
					{
						await Task.Delay(25, m_ct.Token);
						Trace.WriteLine("After delay...");
						if (!m_ct.Token.IsCancellationRequested)
						{
							this.number_of_3E = 0;
							Trace.WriteLine("NofE Reset to zero...");
						}
					}, m_ct.Token).Start();
					number_of_3E++;
					Trace.WriteLine("NofE: {0}", number_of_3E);
					Trace.WriteLineIf(data[64] == '3' && data[65] == 'E' && number_of_3E == max_data_count, "Condition fulfilled...");
					return is3E && number_of_3E == max_data_count;
				}
				return false;
			}

		}

		bool checkIsCar()
		{
			uint midPoint = m_reference.amnt_data / 2;
			//Dr. Aslan
			if (MaxPointCount <= 0) MaxPointCount = (int)m_reference.amnt_data;
			int point = 0;
			for (int i = 0; i < m_reference.amnt_data && point != MaxPointCount; i++)
			{
				int diff = (int)m_reference.data[i] > (int)m_current_data.data[i] ? (int)m_reference.data[i] - (int)m_current_data.data[i] : 0;
				//int diff = (int)m_reference.data[i] > (int)m_current_data.data[i] ? (int)m_reference.data[i] - (int)m_current_data.data[i] : -(int)m_reference.data[i] + (int)m_current_data.data[i];
				if (diff > Threshold)
					point++;
			}
			return point >= MaxPointCount;
		}

		public const string CMD_device_intent = "sRN DeviceIdent";
		public const string CMD_scandata_once = "sRN LMDscandata";
		public const string CMD_scandata_cont = "sEN LMDscandata 1";
		ConsecutiveFieldEvalCheck _field_eval_check = new ConsecutiveFieldEvalCheck();
		private void DoSensorDataProcessing(CancellationToken ct, Socket sickSock)
		{
			//while (!ct.IsCancellationRequested)
			{
				try
				{
					//sickSock.Connect(ip);
					byte[] m_read_bytes = new byte[8 * 1024];
					byte[] sentBytes = get_encoded_bytes(CMD_device_intent);
					int sentBytesCount = sickSock.Send(sentBytes);
					if (sentBytesCount != sentBytes.Length)
						return;
					int readBytesCount = sickSock.Receive(m_read_bytes);

					if ((readBytesCount < 0 || m_read_bytes[1] != 's' || m_read_bytes[2] != 'R' || m_read_bytes[3] != 'A')) //sRA means device reads the values and successfully acknowledges
					{
						return;
					}

					SensorSystemAvailable = true;

					sentBytes = get_encoded_bytes(CMD_scandata_once);
					sentBytesCount = sickSock.Send(sentBytes);

					readBytesCount = sickSock.Receive(m_read_bytes);
					int scanDataSize = readBytesCount;

					//This portion is for reference...
					//Not required now...
					//if (m_read_bytes[1] == 's' && m_read_bytes[2] == 'R' && m_read_bytes[3] == 'A')
					//{
					//	//auto cc = m_read_bytes.data() + 84;
					//	m_reference = get_only_1_scan_data(Encoding.ASCII.GetString(m_read_bytes));
					//	if (SICKReferenceSet != null)
					//		SICKReferenceSet(m_reference);
					//	//int midPoint = m_reference.amnt_data / 2;
					//	//memcpy(m_ref_data, m_reference.data.get()[midPoint - 2], 4 * sizeof(u32));
					//	//JLOG_D(cc);
					//}
					//Reference Set

					sentBytes = get_encoded_bytes(CMD_scandata_cont);
					sentBytesCount = sickSock.Send(sentBytes);//sEN LMDscandata 1

					readBytesCount = sickSock.Receive(m_read_bytes, sentBytes.Length, SocketFlags.None);
					//sEA LMDscandata 1
					int consecutive_3E_count = 0;
					do
					{
						//readBytesCount = sickSock.Receive(m_read_bytes);
						readBytesCount = sickSock.Receive(m_read_bytes, scanDataSize, SocketFlags.None);
						//if (!ec)
						{
							//string&& resp = get_response(m_read_bytes, readBytes);
							//std::cout << resp << endl;
							if (readBytesCount > 84 && m_read_bytes[1] == 's' && m_read_bytes[2] == 'S' && m_read_bytes[3] == 'N')
							{
								//m_current_data = get_only_1_scan_data(Encoding.ASCII.GetString(m_read_bytes));
								if (WillNotify)
								{
									bool is3EOutputDetected = m_read_bytes[64] == '3' && m_read_bytes[65] == 'E';
									SICKDataNotification?.Invoke(m_current_data);

									if (is3EOutputDetected)
									{
										if (++consecutive_3E_count == MaxPointCount)
											SICKCarIncoming?.Invoke();
									}
									else if(consecutive_3E_count != 0)
									{
										Helpers.Log.LogThisInfo("Consecutive count: {0}", consecutive_3E_count);
										consecutive_3E_count = 0;
									}
									//if (SICKCarIncoming != null && checkIsCar())
									//{
									//	SICKCarIncoming();
									//}

									//if (_field_eval_check.CheckIsCar(m_read_bytes, MaxPointCount))
									//{
									//	SICKCarIncoming?.Invoke();
									//}
								}

								//This portion is for reference...
								//Not required now...
								//if (SetReference)
								//{
								//	m_reference = m_current_data.Copy();
								//	if (SICKReferenceSet != null)
								//		SICKReferenceSet(m_reference);
								//	SetReference = false;

								//}
								//Reference set
							}
						}
					} while (SensorSystemAvailable && !ct.IsCancellationRequested);

				}
				catch (SocketException sex)
				{
					Helpers.Log.LogThisError("EXCEPTION: {0}" + sex.Message);
				}
				catch (Exception ex)
				{
					Helpers.Log.LogThisWarn("Exception: {0}", ex.Message);
					//throw;
				}
			}
		}
	}
}
