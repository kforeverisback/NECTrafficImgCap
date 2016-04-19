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
		public UInt32 [] data = new uint[256]; //MAX 512 DATA
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
		CancellationTokenSource _cancelTokenS = new CancellationTokenSource();

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

		public delegate void SICKCarIncomingNotiHandler();
		public event SICKCarIncomingNotiHandler SICKCarIncoming;

		public delegate void SICKDataNotificationHandler(channel_data_16b data);
		public event SICKDataNotificationHandler SICKDataNotification;
		
		public event SICKDataNotificationHandler SICKReferenceSet;

		public SICKSystem()
		{
			Threshold = 0x20;
			SetReference = false;
			WillNotify = true; 
			_sickSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		~SICKSystem()
		{ Disconnect(); }

		public bool Connect()
		{
			if (SensorSystemAvailable)
				return true;
			IPAddress[] ipaddr = Dns.GetHostAddresses("169.254.3.172");
			if(ipaddr.Length > 0)
			{
				IPAddress ip = ipaddr[0];
				_ipAndPort = new IPEndPoint(ip, 2111);
				Task.Factory.StartNew(() => 
				{

					DoBackgroundWork(_cancelTokenS.Token, _ipAndPort);
				}, _cancelTokenS.Token);
				
				return true;
			}
			else
			{
				return false;
			}

		}

		public void Disconnect()
		{
			_cancelTokenS.Cancel();
			SensorSystemAvailable = false;
		}
		static string get_encoded_cmd(string str)
		{
			return "\x02" + str + "\x03";
		}

		static byte[] get_encoded_bytes(string str)
		{
			return Encoding.ASCII.GetBytes("\x02" + str + "\x03");
		}

		static channel_data_16b get_only_1_scan_data(string data)
		{
			channel_data_16b chdata = new channel_data_16b();
			string str;
			string newData = data.Substring(84);
			string []splitLines = newData.Split(" ".ToCharArray());
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
					chdata.data[i] = Convert.ToUInt32(splitLines[6+i], 16);
				}
			}
			return chdata;
		}

		bool checkIsCar()
		{
			uint midPoint = m_reference.amnt_data / 2;
			//Dr. Aslan
			if (MaxPointCount <= 0) MaxPointCount = (int)m_reference.amnt_data;
			int point = 0;
			for (int i = 0; i < m_reference.amnt_data && point != MaxPointCount; i++)
			{

				int diff = (int)m_reference.data[i] > (int)m_current_data.data[i] ? (int)m_reference.data[i] - (int)m_current_data.data[i] : -(int)m_reference.data[i] + (int)m_current_data.data[i];
				if (diff > Threshold)
					point++;
			}
			return point >= MaxPointCount;
		}

		private void DoBackgroundWork(CancellationToken ct, IPEndPoint ip)
		{
			//while (!ct.IsCancellationRequested)
			{
				try
				{
					_sickSock.Connect(ip);
					byte[] m_read_bytes = new byte[4096];
					byte[] sentBytes = get_encoded_bytes("sRN DeviceIdent");
					int sentBytesCount = _sickSock.Send(sentBytes);
					if (sentBytesCount != sentBytes.Length)
						return;
					int readBytesCount = _sickSock.Receive(m_read_bytes);

					if ((readBytesCount < 0 || m_read_bytes[1] != 's' || m_read_bytes[2] != 'R' || m_read_bytes[3] != 'A'))
					{
						return;
					}

					SensorSystemAvailable = true;

					sentBytes = get_encoded_bytes("sRN LMDscandata");
					sentBytesCount = _sickSock.Send(sentBytes);

					readBytesCount = _sickSock.Receive(m_read_bytes);
					int scanDataSize = readBytesCount;

					if (m_read_bytes[1] == 's' && m_read_bytes[2] == 'R' && m_read_bytes[3] == 'A')
					{
						//auto cc = m_read_bytes.data() + 84;
						m_reference = get_only_1_scan_data(Encoding.ASCII.GetString(m_read_bytes));
						if (SICKReferenceSet != null)
							SICKReferenceSet(m_reference);
						//int midPoint = m_reference.amnt_data / 2;
						//memcpy(m_ref_data, m_reference.data.get()[midPoint - 2], 4 * sizeof(u32));
						//JLOG_D(cc);
					}

					sentBytes = get_encoded_bytes("sEN LMDscandata 1");
					sentBytesCount = _sickSock.Send(sentBytes);

					readBytesCount = _sickSock.Receive(m_read_bytes, sentBytes.Length, SocketFlags.None);
					do
					{
						//readBytesCount = _sickSock.Receive(m_read_bytes);
						readBytesCount = _sickSock.Receive(m_read_bytes, scanDataSize, SocketFlags.None);
						//if (!ec)
						{
							//string&& resp = get_response(m_read_bytes, readBytes);
							//std::cout << resp << endl;
							if (readBytesCount > 84 && m_read_bytes[1] == 's' && m_read_bytes[2] == 'S' && m_read_bytes[3] == 'N' && !SetReference)
							{
								m_current_data = get_only_1_scan_data(Encoding.ASCII.GetString(m_read_bytes));
								if (WillNotify)
								{
									if (SICKDataNotification != null)
										SICKDataNotification(m_current_data);

									if (SICKCarIncoming != null && checkIsCar())
									{
										SICKCarIncoming();
									}
								}
								if (SetReference)
								{
									m_reference = m_current_data.Copy();
									if (SICKReferenceSet != null)
										SICKReferenceSet(m_reference);
									SetReference = false;

								}
							}
						}
					} while (SensorSystemAvailable && !ct.IsCancellationRequested);

				}
				catch (SocketException sex)
				{
					Helpers.Log.LogThisError("EXCEPTION: " + sex.Message);
				}
				catch (Exception ex)
				{

					throw;
				}
			}
		}
	}
}
