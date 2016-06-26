using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.ViewModel
{
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Messaging;
	using GalaSoft.MvvmLight.Command;
	using Gardasoft.Controller.API.Exceptions;
	using Gardasoft.Controller.API.Interfaces;
	using Gardasoft.Controller.API.Managers;
	using Gardasoft.Controller.API.Model;
	using Gardasoft.Controller.API.EventsArgs;
	using DeviceSystems;
	class GardaVM : ViewModelBase
	{
		GardaSystem _gs;

		#region Binding Properties
		public List<string> ChannelModes
		{
			get;private set;
		}

		public List<IController> GardaControllers
		{
			get { return _gs.Controllers; }
		}

		public List<IChannel> GardaChannels
		{
			get { return _gs.Channels; }
		}

		int _SelectedControllers;
		public int SelectedController
		{
			get { return _SelectedControllers; }
			set {
				_SelectedControllers = value;
				RaisePropertyChanged("SelectedController");
				_gs.SetActiveController(value);
				SelectedMode = 1;
			}
		}

		int _SelectedMode;
		public int SelectedMode
		{
			get { return _SelectedMode; }
			set { _SelectedMode = value; RaisePropertyChanged("SelectedMode"); }
		}

		int _SelectedChannel;
		public int SelectedChannel
		{
			get { return _SelectedChannel; }
			set { _SelectedChannel = value; RaisePropertyChanged("SelectedChannel"); }
		}

		int _BrightnessValue;
		public int BrightnessValue
		{
			get { return _BrightnessValue; }
			set
			{
				_BrightnessValue = value;
				RaisePropertyChanged("BrightnessValue");
				_gs.ChangeBrightness(value);
			}
		}

		bool _searchBtnEnalbed;
		bool _CloseBtnEnalbed;
		bool _BrightneseEnabled;
		public bool SearchBtnEnalbed
		{
			get { return _searchBtnEnalbed; }
			set { _searchBtnEnalbed = value; RaisePropertyChanged("SearchBtnEnalbed"); }
		}

		public bool CloseBtnEnalbed
		{
			get { return _CloseBtnEnalbed; }
			set { _CloseBtnEnalbed = value; RaisePropertyChanged("CloseBtnEnalbed"); }
		}
		public bool BrightneseEnabled
		{
			get { return _BrightneseEnabled; }
			set { _BrightneseEnabled = value; RaisePropertyChanged("BrightneseEnabled"); }
		}
		//public bool PulseDelayEnabled
		//{
		//	get { return _PulseDelayEnabled; }
		//	set { _PulseDelayEnabled = value; RaisePropertyChanged("PulseDelayEnabled"); }
		//}
		//public bool PulseWidthEnabled
		//{
		//	get { return _PulseWidthEnabled; }
		//	set { _PulseWidthEnabled = value; RaisePropertyChanged("PulseWidthEnabled"); }
		//}
		#endregion

		#region Relay Commands
		public RelayCommand SearchGarda { get; private set; }
		public RelayCommand CloseGarda { get; private set; }
		#endregion

		void SetGardaSystemEvents()
		{
			_gs.ControllersAvailable += (s, e) =>
			{
				if (GardaControllers != null)
				{
					RaisePropertyChanged("GardaControllers");
					SelectedController = 0;
				}
			};

			_gs.ChannelsAvailable += (ss, ee) => 
			{
				if (GardaChannels != null && GardaChannels.Count != 0)
				{
					RaisePropertyChanged("GardaChannels");
					SelectedChannel = 0;
				}
			};

			_gs.ConnectionStatusChanged += (sss, eee) => 
			{
				if(eee.ControllerStatus == ControllerStatus.Connected)
				{
					BrightneseEnabled = true;
					SearchBtnEnalbed = false;
					CloseBtnEnalbed = true;
				}
				else
				{
					BrightneseEnabled = true;
					SearchBtnEnalbed = true;
					CloseBtnEnalbed = false;
					RaisePropertyChanged("GardaControllers");
				}
			};

			Messenger.Default.Register<Register.ChannelMode>(this, (cm) => 
			{
				if(cm == Register.ChannelMode.Continuous || cm == Register.ChannelMode.Switched)
				{
					_gs.SwitchMode(cm);
					SelectedMode = cm == Register.ChannelMode.Continuous ? 0 : 1;
				}
			});
		}

		//Register.ChannelMode chm = Register.ChannelMode.Switched;
		public GardaVM()
		{
			_gs = new GardaSystem();
			SetNoLedControlsEnabled(false);
			SetGardaSystemEvents();
			SearchGarda = new RelayCommand(() => 
			{
				_gs.SearchGardaSystem();
			});

			CloseGarda = new RelayCommand(()=> 
			{
				_gs.CloseGardaSystem();
				//if (chm == Register.ChannelMode.Continuous)
				//	chm = Register.ChannelMode.Switched;
				//else
				//	chm = Register.ChannelMode.Continuous;

				//_gs.SwitchMode(chm);
				//SelectedMode = chm == Register.ChannelMode.Continuous ? 0 : 1;
			});

			ChannelModes = new List<string>();
			ChannelModes.Add(Enum.GetName(typeof(Register.ChannelMode), Register.ChannelMode.Continuous));
			ChannelModes.Add(Enum.GetName(typeof(Register.ChannelMode), Register.ChannelMode.Switched));
		}

		void SetNoLedControlsEnabled(bool enable)
		{
			/*PulseWidthEnabled = PulseDelayEnabled =*/ BrightneseEnabled = CloseBtnEnalbed = enable;
			SearchBtnEnalbed = !enable;
		}
	}
}
