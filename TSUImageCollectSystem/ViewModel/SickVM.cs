using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.ViewModel
{
	using DeviceSystems;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Messaging;
	using GalaSoft.MvvmLight.Command;
	class SickVM : ViewModelBase
	{
		public enum Cmds
		{
			CarIncoming,

		}
		public enum Resp
		{
			Capturing,
			CapturingFinished
		}

		SICKSystem _ss;

		public RelayCommand SetReference { get; set; }
		public RelayCommand StartSickSensor { get; set; }
		public RelayCommand StopSickSensor { get; set; }

		bool _SICKStartBtnEnabled { get; set; }
		public bool SICKStartBtnEnabled
		{
			get { return _SICKStartBtnEnabled; }
			set { _SICKStartBtnEnabled = value; RaisePropertyChanged("SICKStartBtnEnabled"); }
		}

		bool _SICKStopBtnEnabled { get; set; }
		public bool SICKStopBtnEnabled
		{
			get { return _SICKStopBtnEnabled; }
			set { _SICKStopBtnEnabled = value; RaisePropertyChanged("SICKStopBtnEnabled"); }
		}

		bool _SICKReferenceBtnEnabled { get; set; }
		public bool SICKReferenceBtnEnabled
		{
			get { return _SICKReferenceBtnEnabled; }
			set { _SICKReferenceBtnEnabled = value; RaisePropertyChanged("SICKReferenceBtnEnabled"); }
		}

		public int DataCountUsed
		{
			get { return _ss.MaxPointCount; }
			set
			{
				if(value <= RefDataAmount && value > 0)
				{
					_ss.MaxPointCount = value;
				}
				RaisePropertyChanged("DataCountUsed");
			} }


		public int DelayBetweenCars
		{
			get; set;
		}
		public int RefDataAmount { get; private set; }

		public SickVM()
		{
			_ss = new SICKSystem();
			_ss.SICKReferenceSet += (refData) =>
			{
				RefDataAmount = (int)refData.amnt_data;
				DataCountUsed = (int)Math.Floor(RefDataAmount / 2.00)+1;
				RaisePropertyChanged("RefDataAmount");
			};

			_ss.SICKCarIncoming += () => 
			{
				Messenger.Default.Send<SickVM.Cmds>(Cmds.CarIncoming);
				_ss.WillNotify = false;
				Task.Factory.StartNew(()=> 
				{
					System.Threading.Thread.Sleep(DelayBetweenCars);
					_ss.WillNotify = true;
				});
			};

			//Messenger.Default.Register<SickVM.Resp>(this, (r) => 
			//{
			//	switch(r)
			//	{
			//		case Resp.Capturing:
			//			_ss.WillNotify = false;
			//			break;
			//		case Resp.CapturingFinished:
			//			_ss.WillNotify = true;
			//			break;
			//	}
			//});

			RefDataAmount = 0;
			DataCountUsed = 0;
			SICKReferenceBtnEnabled = SICKStopBtnEnabled = false;
			SICKStartBtnEnabled = true;
			DelayBetweenCars = 2000;
			StartSickSensor = new RelayCommand(() =>
			{
				SICKStartBtnEnabled = false;
				if(_ss.Connect())
				{
					SICKReferenceBtnEnabled = SICKStopBtnEnabled = true;
				}
				else
				{
					SICKStartBtnEnabled = true;
				}
			});

			StopSickSensor = new RelayCommand(() =>
			{
				SICKReferenceBtnEnabled = SICKStopBtnEnabled = false;
				_ss.Disconnect();
				SICKReferenceBtnEnabled = SICKStopBtnEnabled = false;
				SICKStartBtnEnabled = true;
			});

			SetReference = new RelayCommand(() =>
			{
				_ss.SetReference = true;
				System.Threading.Thread.Sleep(10);
			});
		}
	}
}
