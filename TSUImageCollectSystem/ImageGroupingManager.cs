using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace TSUImageCollectSystem
{	
	public delegate void FileAvailableHandler(string filePath, int fileRemainCount);
	class ImageGroupingManager
	{
		public int ImagesPerCar {
			get; set;
		}
		public int CarsPerGroup { get; set; }
		public string BasePath { get; private set; }

		int _carCount = 1;
		int _carImageCount = 1;
		int _groupCount = 1;

		//Thread locking mechanism
		ReaderWriterLockSlim _threadLocker;
		CancellationTokenSource _cancelTokenS = new CancellationTokenSource();

		const int IdleDelay = 100;//100ms
		Queue<string> _queueOfPath;
		long _fileRemainingCount;


		public void QueueImageFile(string fullPath)
		{
			if (!string.IsNullOrEmpty(fullPath))
			{
				Task.Factory.StartNew(() => 
				{
					_threadLocker.EnterWriteLock();
					_queueOfPath.Enqueue(fullPath);
					_threadLocker.ExitWriteLock();
					Interlocked.Increment(ref _fileRemainingCount);
				});
			}
		}


		public ImageGroupingManager(int carsPerGroup, int imagesPerCar)
		{
			CarsPerGroup = carsPerGroup;
			ImagesPerCar = imagesPerCar;
			_fileRemainingCount = 0;
			_queueOfPath = new Queue<string>();
			_threadLocker = new ReaderWriterLockSlim();

			BasePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "output"), DateTime.Today.ToString("yyyy-MM-dd"));
			Directory.CreateDirectory(BasePath);

			Task.Factory.StartNew(()=> 
			{
				DoBackgroundWork(_cancelTokenS.Token);
			}, _cancelTokenS.Token);
		}

		private string GetNextCarPath()
		{
			if(_carCount == CarsPerGroup) { _carCount = 1; _groupCount++; }
			if(_carImageCount > ImagesPerCar) { _carCount++; _carImageCount = 1; }
			string grpCarPath = string.Format("Group-{0}\\car-{1}", _groupCount, _carCount);
			return Path.Combine(BasePath, grpCarPath);
		}

		private string GetNextImageName()
		{
			//if (_carImageCount == ImagesPerCar) { _carImageCount = 1; }

			string imgName = string.Format("car-image-{0}.bmp", _carImageCount++);
			return imgName;
		}

		private void DoBackgroundWork(CancellationToken ct)
		{
			while(!ct.IsCancellationRequested)
			{
				if(Interlocked.Read(ref _fileRemainingCount) == 0)
				{
					Task.Delay(IdleDelay);
					continue;
				}

				Interlocked.Decrement(ref _fileRemainingCount);
				_threadLocker.EnterWriteLock();
				try
				{
					string srcFilePath = _queueOfPath.Dequeue();
					string nextCarPath = GetNextCarPath();
					Directory.CreateDirectory(nextCarPath);
					string destFilePath = Path.Combine(nextCarPath, GetNextImageName());
					System.Diagnostics.Debug.WriteLine("Src:{0}, dest:{1}", srcFilePath, destFilePath);
					File.Move(srcFilePath, destFilePath);
					_threadLocker.ExitWriteLock();
				}
				catch (Exception ex)
				{
					Helpers.Log.LogThisError("#1-Exception in ImageGrouping Manager. Ex: {0}", ex.Message);
					_threadLocker.ExitWriteLock();
				}
			}
		}
	}
}
