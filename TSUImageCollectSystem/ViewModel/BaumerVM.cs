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

		public int TotalCarCount
		{ get; private set; }

		public string LastCarFolderName
		{ get; private set; }

		const int _max_capture_count = 25, _min_capture_count = 1;
		public int BatchCaptureCount
		{
			get { return BSParameters.BatchCaptureCount; }
			set { BSParameters.BatchCaptureCount = Helpers.Utility.Clamp(value, _min_capture_count, _max_capture_count); RaisePropertyChanged("BatchCaptureCount"); }
		}

		public int ExposureInMs
		{
			get
			{ return (int)BSParameters.ExposureValue; }
			set { _bs.SetExposure(value); RaisePropertyChanged("ExposureInMs"); }
		}

		public int TriggerDelay
		{
			get
			{ return (int)BSParameters.TriggerDelay; }
			set { _bs.SetTriggerDelay(value); RaisePropertyChanged("TriggerDelay"); }
		}

		public int _CaptureDelay;
		public int CaptureDelay
		{
			get
			{ return _CaptureDelay; }
			set { _CaptureDelay = value; RaisePropertyChanged("CaptureDelay"); }
		}

		public string OutputPath
		{
			get { return _bs.Parameters.BasePath; }
			set
			{
				_bs.Parameters.BasePath = value;
				RaisePropertyChanged("OutputPath");
			}
		}

		public BaumerVM()
		{
			_bs = new BaumerSystem();
			_bs.CarFolderCreated += (folderName) =>
			{
				TotalCarCount++;
				LastCarFolderName = folderName;
				RaisePropertyChanged("TotalCarCount");
				RaisePropertyChanged("LastCarFolderName");
				//_grpManager.QueueImageFile(path);
			};
			//_grpManager = new ImageGroupingManager(carsPerGroup: 10, imagesPerCar: _bs.Parameters.BatchCaptureCount);

			StartBaumerEnabled = true; StopBaumerEnabled = CaptureBaumerEnabled = false;
			StartBaumerCommand = new RelayCommand(async () =>
			{
				StartBaumerEnabled = false;

				StartBaumerEnabled = !await Task.Factory.StartNew<bool>(() =>
				{
					try
					{
						return _bs.StartBaumerCam();
					}
					catch (Exception ex)
					{
						Helpers.Log.LogThisWarn("Exception...", ex);
						return false;
					}
				});

				Helpers.Log.LogThisInfo("Finished Enabling!");
				//if (_bs.Status == BaumerStatus.Ready)
				//{

				//	ExposureInMs = (int)_bs.Parameters.ExposureValue;
				//	TriggerDelay = (int)_bs.Parameters.TriggerDelay;
				//}
				//else
				//{
				//	ExposureInMs = -1;
				//	TriggerDelay = -1;
				//	Helpers.Utility.ShowError("Baumer System not initialized");
				//}
				StopBaumerEnabled = !StartBaumerEnabled;
				CaptureBaumerEnabled = !StartBaumerEnabled;
			}/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Stopped || _bs.Status == BaumerStatus.Uninitiated); }*/);

			StopBaumerCommand = new RelayCommand(async () =>
		   {
			   CaptureBaumerEnabled = false;
			   StopBaumerEnabled = false;
			   await Task.Factory.StartNew(() => { _bs.StopBaumerCam(); });
			   StartBaumerEnabled = true;
			   CaptureBaumerEnabled = false;
		   }/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);

			CaptureBaumerCommand = new RelayCommand(async () =>
		   {
			   if (_bs.Status != BaumerStatus.Ready) return;

			   
			   //Send to LED
			   /*For now data sending */
			   //Messenger.Default.Send<Gardasoft.Controller.API.Model.Register.ChannelMode>(Gardasoft.Controller.API.Model.Register.ChannelMode.Continuous);
			   //Send to SICK
			   //Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.Capturing);
			   await Task.Factory.StartNew(async () =>
			   {
				   //for (int i = 0; i < 50; i++ )
				   {
					   StopBaumerEnabled = false;
					   CaptureBaumerEnabled = false;
					   await Task.Delay(CaptureDelay);
					   _bs.DoCapture();
					   StopBaumerEnabled = true;
					   CaptureBaumerEnabled = true;
					   //_bs.CaptureInBatch();
					   //_bs.CaptureAndSaveSingleFrame();
				   }
			   });
			   //Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.CapturingFinished);
			   //Messenger.Default.Send<Gardasoft.Controller.API.Model.Register.ChannelMode>(Gardasoft.Controller.API.Model.Register.ChannelMode.Switched);
			   
		   }/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);

			Messenger.Default.Register<SickVM.Cmds>(this, (cmd) =>
			{
				if (cmd == SickVM.Cmds.CarIncoming)
				{
					CaptureBaumerCommand.Execute(null);
				}
			});

			BrowseCommand = new RelayCommand(() =>
			{
				System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
				fbd.ShowNewFolderButton = true;
				fbd.SelectedPath = _bs.Parameters.BasePath;
				fbd.Description = "Select Output Folder";
				if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					OutputPath = fbd.SelectedPath;
				}
			});

			CaptureDelay = Helpers.Args.DefaultArgs.CaptureDelay;
			//ExposureInMs = Helpers.Args.DefaultArgs.Exposure;
			//System.Timers.Timer t = new System.Timers.Timer(1000);
			//t.AutoReset = true;
			//t.Elapsed += (s, e) =>
			//{
			//	System.Diagnostics.Debug.WriteLine("{0}", e.SignalTime.ToShortTimeString());
			//};
			//t.Enabled = true;
		}

		~BaumerVM()
		{
			//_bs.StopBaumerCam();
		}

		public RelayCommand StartBaumerCommand { get; private set; }
		public RelayCommand StopBaumerCommand { get; private set; }
		public RelayCommand CaptureBaumerCommand { get; private set; }
		public RelayCommand BrowseCommand { get; private set; }

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
		public bool StopBaumerEnabled
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
