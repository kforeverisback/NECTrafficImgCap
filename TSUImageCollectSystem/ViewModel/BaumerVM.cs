using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.ViewModel
{
	using DeviceSystems;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using GalaSoft.MvvmLight.Messaging;

	class BaumerVM : ViewModelBase
	{
		BaumerSystem _bs;
		public BaumerVM	()
		{
			_bs = new BaumerSystem();
			StartBaumerEnabled = true; StopBaumerEnabled = CaptureBaumerEnabled = false;
			StartBaumerCommand = new RelayCommand(async ()=>
			{
				StartBaumerEnabled = false;
				StartBaumerEnabled = !await Task.Factory.StartNew<bool>(() => {return  _bs.StartBaumerCam(); });
				StopBaumerEnabled = !StartBaumerEnabled;
				CaptureBaumerEnabled = !StartBaumerEnabled;
			}/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Stopped || _bs.Status == BaumerStatus.Uninitiated); }*/);

			StopBaumerCommand = new RelayCommand( async ()=> 
			{
				CaptureBaumerEnabled = false;
				StopBaumerEnabled = false;
				await Task.Factory.StartNew(() => { _bs.StopBaumerCam(); });
				StartBaumerEnabled = true;
				CaptureBaumerEnabled = false;
			}/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);

			CaptureBaumerCommand = new RelayCommand( async ()=> 
			{
				StopBaumerEnabled = false;
				CaptureBaumerEnabled = false;
				await Task.Factory.StartNew(() => 
				{
					for (int i = 0; i < 50; i++ )
					{
						_bs.CaptureAndSaveSingleFrame();
						Task.Delay(10);
					}
				});
				StopBaumerEnabled = true;
				CaptureBaumerEnabled = true;
			}/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);
		}

		~BaumerVM()
		{
			//_bs.StopBaumerCam();
		}

		public RelayCommand StartBaumerCommand { get; private set; }
		public RelayCommand StopBaumerCommand { get; private set; }
		public RelayCommand CaptureBaumerCommand { get; private set; }

		bool _StartBaumerEnabled { get; set; }
		bool _StopBaumerEnabled { get; set; }
		bool _CaptureBaumerEnabled { get; set; }
		public bool StartBaumerEnabled
		{
			get { return _StartBaumerEnabled; }
			set
			{
				_StartBaumerEnabled = value; RaisePropertyChanged("StartBaumerEnabled");
			}
		}
		public bool  StopBaumerEnabled
		{
			get { return _StopBaumerEnabled; }
			set { _StopBaumerEnabled = value; RaisePropertyChanged("StopBaumerEnabled"); }
		}
		public bool CaptureBaumerEnabled
		{
			get { return _CaptureBaumerEnabled; }
			set { _CaptureBaumerEnabled = value; RaisePropertyChanged("CaptureBaumerEnabled"); }
		}
	}
}
