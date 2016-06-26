using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.DeviceSystems
{

	using Gardasoft.Controller.API.Exceptions;
	using Gardasoft.Controller.API.Interfaces;
	using Gardasoft.Controller.API.Managers;
	using Gardasoft.Controller.API.Model;
	using Gardasoft.Controller.API.EventsArgs;
	public class GardaSystem
	{
		/// <summary>
		/// The current instance of the ControllerManager
		/// </summary>
		private ControllerManager _controllerManager;
		private IChannel _activeChannel;
		private IController _activeController;

		public GardaSystem()
		{
			_controllerManager = ControllerManager.Instance();
			_controllerManager.DeviceDiscoveryStatusChanged += (s, e) =>
			{
				if ( ControllersAvailable != null)
				{
					Controllers = _controllerManager.Controllers;
					ControllersAvailable(s, e);
				}
			};
		}

		~GardaSystem()
		{

		}

		public void SwitchMode(Register.ChannelMode cm)
		{
			if (_activeChannel != null && _activeChannel.Registers["ChannelMode"] != null)
			{
				_activeChannel.Registers["ChannelMode"].CurrentValue = cm;
				//BrightnessValueRangeChanged(max, min);
				//trackBarBrightness.Maximum = 100;
				_activeChannel.Registers.Refresh();
			}
		}

		public void ChangeBrightness(int value)
		{
			if (InitializedSystem && _activeChannel.Registers["ChannelMode"].CurrentValue is Register.ChannelMode )
			{
				Register.ChannelMode ccm = (Register.ChannelMode)_activeChannel.Registers["ChannelMode"].CurrentValue;
				if(ccm == Register.ChannelMode.Continuous)
				{
					_activeChannel.Registers["Brightness"].CurrentValue = value;
				}
			}
		}

		public void SetActiveController(int index)
		{
			if (_controllerManager.Controllers.Count > index)
			{
				_activeController = _controllerManager.Controllers[index];
				//_activeController.ConnectionStatusChanged += (s, e) =>
				//{
				//	if (ConnectionStatusChanged != null)
				//		ConnectionStatusChanged(s, e);
				//};

				_activeController.StatusChanged += (ss, ee) =>
				{
					ConnectionStatusChanged(ss, ee);
				};
				if (_activeController != null)
					Task.Factory.StartNew(() =>
					{
						_activeController.Open();
						if (_activeController.IsTriniti)
						{
							if (_activeController.Channels.Count > 0 && ChannelsAvailable != null)
							{
								Channels = _activeController.Channels;
								ChannelsAvailable(_activeController, null);

								//This is the default channel ALWZ
								_activeChannel = _activeController.Channels[0];
								RegisterChannelProperties(_activeChannel);
								// Force immediate update of values 
								_activeChannel.Registers.Refresh();
								InitializedSystem = true;
							}

						}

					});
			}

		}

		void RegisterChannelProperties(IChannel chnl)
		{
			if (chnl.Registers.Contains("Brightness"))
			{
				chnl.Registers["Brightness"].PropertyChanged += GardaSystem_BrightnessPropChanged;
				//chnl.Registers["PulseWidth"].PropertyChanged += GardaSystem_PluseWidthPropChanged; 
				//chnl.Registers["PulseDelay"].PropertyChanged += GardaSystem_PulseDelayPropChanged; 
				chnl.Registers["ChannelMode"].PropertyChanged += GardaSystem_ChannelModePropChanged;

			}
		}

		void UnregisterChannelProperties(IChannel chnl)
		{
			if (chnl.Registers.Contains("Brightness"))
			{
				chnl.Registers["Brightness"].PropertyChanged -= GardaSystem_BrightnessPropChanged;
				//chnl.Registers["PulseWidth"].PropertyChanged += GardaSystem_PluseWidthPropChanged; 
				//chnl.Registers["PulseDelay"].PropertyChanged += GardaSystem_PulseDelayPropChanged; 
				chnl.Registers["ChannelMode"].PropertyChanged -= GardaSystem_ChannelModePropChanged;

			}
		}

		private void GardaSystem_ChannelModePropChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void GardaSystem_PulseDelayPropChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void GardaSystem_PluseWidthPropChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void GardaSystem_BrightnessPropChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (_activeChannel != null)
			{
				try
				{
					//labelBrightness.Text =
					//	((float)_activeChannel.Registers["Brightness"].CurrentValue).ToString("0.00");
					//trackBarBrightness.Value = Convert.ToInt32(_activeChannel.Registers["Brightness"].CurrentValue);
				}
				catch (Exception)
				{
					_activeChannel.Registers["Brightness"].CurrentValue = 0;
				}

			}
		}

		public List<IController> Controllers { get; private set; }
		public List<IChannel> Channels { get; private set; }
		public bool InitializedSystem { get; private set; }


		public void SearchGardaSystem()
		{
			Task.Factory.StartNew(() =>
			{
				_controllerManager.DiscoverControllers();
			});

		}

		public void CloseGardaSystem()
		{
			InitializedSystem = false;
			SwitchMode(Register.ChannelMode.Switched);
			UnregisterChannelProperties(_activeChannel);
			_activeController.Close();
		}

		#region EVENTS
		public
		event EventHandler<Gardasoft.Device.Discovery.DeviceDiscoveryStatusChangedEventArgs> ControllersAvailable;
		public event EventHandler ChannelsAvailable;
		public event EventHandler<ControllerStatusChangedEventArgs> ConnectionStatusChanged;
		public delegate void BrightnessValueRangeChangedDelegate(int max, int min);
		//public event BrightnessValueRangeChangedDelegate BrightnessValueRangeChanged;
		#endregion;

	}
}
