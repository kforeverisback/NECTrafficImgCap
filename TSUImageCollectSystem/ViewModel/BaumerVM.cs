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
		//ImageGroupingManager _grpManager;

		public BaumerVM	()
		{
			_bs = new BaumerSystem();
			_bs.ImageFileWritten += (path) => 
			{
				//_grpManager.QueueImageFile(path);
			};
			//_grpManager = new ImageGroupingManager(carsPerGroup: 10, imagesPerCar: _bs.Parameters.BatchCaptureCount);

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
				Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.Capturing);
				await Task.Factory.StartNew(() =>
				{
						//for (int i = 0; i < 50; i++ )
						{
						_bs.CaptureInBatch();
							//_bs.CaptureAndSaveSingleFrame();
						}
				});
				Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.CapturingFinished);
				StopBaumerEnabled = true;
				CaptureBaumerEnabled = true;
			}/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);

			Messenger.Default.Register<SickVM.Cmds>(this, (cmd) => 
			{
				if(cmd == SickVM.Cmds.CarIncoming)
				{
					CaptureBaumerCommand.Execute(null);
				}
			});
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

		public BaumerSystemParameters BSParameters { get { return _bs.Parameters; } }
	}
}
