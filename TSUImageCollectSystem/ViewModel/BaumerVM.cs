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
		public int TotalImageShot
		{ get { return _bs.TotalImageShot; } }

		public int TotalCarCount
		{ get { return (int)Math.Ceiling((double)TotalImageShot / _bs.Parameters.BatchCaptureCount); } }

		public int TotalGroupCount
		{ get { return _bs.Parameters.GroupCount; } }

		public int _ExposureInMs;
		public int ExposureInMs
		{
			get
			{ return _ExposureInMs; }
			set { _ExposureInMs = _bs.SetExposure(value); RaisePropertyChanged("ExposureInMs"); }
		}

		public string OutputPath
		{
			get { return _bs.Parameters.BasePath; }
			set {
				_bs.Parameters.BasePath = value;
				RaisePropertyChanged("OutputPath");
			}
		}

		public BaumerVM()
		{
			_bs = new BaumerSystem();
			_bs.ImageFileWritten += (path) =>
			{
				RaisePropertyChanged("TotalImageShot");
				RaisePropertyChanged("TotalCarCount");
				RaisePropertyChanged("TotalGroupCount");
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
					return false;
				}
				});

				Helpers.Log.LogThisInfo("Finished Enabling!");
				if (_bs.Status == BaumerStatus.Ready)
					ExposureInMs = (int)_bs.Parameters.ExposureValue;
				else
				{
					ExposureInMs = 0;
					Helpers.Utility.ShowError("Baumer System not initialized");
				}
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

			   StopBaumerEnabled = false;
			   CaptureBaumerEnabled = false;
			   //Send to LED
			   /*For now data sending */
			   //Messenger.Default.Send<Gardasoft.Controller.API.Model.Register.ChannelMode>(Gardasoft.Controller.API.Model.Register.ChannelMode.Continuous);
			   //Send to SICK
			   //Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.Capturing);
			   await Task.Factory.StartNew(() =>
			   {
				   //for (int i = 0; i < 50; i++ )
				   {
					   _bs.DoCapture();
					   //_bs.CaptureInBatch();
					   //_bs.CaptureAndSaveSingleFrame();
				   }
			   });
			   //Messenger.Default.Send<SickVM.Resp>(SickVM.Resp.CapturingFinished);
			   //Messenger.Default.Send<Gardasoft.Controller.API.Model.Register.ChannelMode>(Gardasoft.Controller.API.Model.Register.ChannelMode.Switched);
			   StopBaumerEnabled = true;
			   CaptureBaumerEnabled = true;
		   }/*, ()=> { return !_bs.IsProcessing && (_bs.Status == BaumerStatus.Ready && _bs.Status != BaumerStatus.Capturing); }*/);

			Messenger.Default.Register<SickVM.Cmds>(this, (cmd) =>
			{
				if (cmd == SickVM.Cmds.CarIncoming)
				{
					CaptureBaumerCommand.Execute(null);
				}
			});

			BrowseCommand = new RelayCommand(()=> 
			{
				System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
				fbd.ShowNewFolderButton = true;
				fbd.SelectedPath = _bs.Parameters.BasePath;
				fbd.Description = "Select Output Folder";
				if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					OutputPath = fbd.SelectedPath;
				}
			});


			System.Timers.Timer t = new System.Timers.Timer(1000);
			t.AutoReset = true;
			t.Elapsed += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine("{0}", e.SignalTime.ToShortTimeString());
			};
			t.Enabled = true;
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
