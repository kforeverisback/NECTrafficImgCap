using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSUImageCollectSystem.Helpers
{
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Runtime.InteropServices;
	using System.Text.RegularExpressions;
	static class Utility
	{
		static public void ShowInfo(string txt)
		{
			System.Windows.MessageBox.Show(txt, "Successful", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
		}
		static public void ShowError(string txt)
		{
			System.Windows.MessageBox.Show(txt, "!!Error!!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
		}
		static public void ShowWarning(string txt)
		{
			System.Windows.MessageBox.Show(txt, "!!WARNING!!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
		}
		public static bool IsTextAllowed(string text)
		{
			Regex regex = new Regex("[^0-9.-\\.]+"); //regex that matches disallowed text
			return !regex.IsMatch(text);
		}

		public static string GetTimeStamp()
		{
			return DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
		}

		public static int Clamp(int value, int min, int max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}

		static Color[] _saved_entries;
		public static Bitmap GetGrayBitmap(int width, int height, IntPtr ptr0)
		{
			byte[] bytesImg = new byte[width * height];
			Marshal.Copy(ptr0, bytesImg, 0, width * height);

			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
			ColorPalette palette = bmp.Palette;
			Color[] _entries = palette.Entries;
			if (_saved_entries == null)
			{
				_saved_entries = new Color[_entries.Length];
				for (int i = 0; i < 256; i++)
				{
					Color b = new Color();
					b = Color.FromArgb((byte)i, (byte)i, (byte)i);
					_entries[i] = b;
				}
				Array.Copy(_entries, _saved_entries, _entries.Length);
			}
			else
			{
				Array.Copy(_saved_entries, _entries, _entries.Length);
			}

			bmp.Palette = palette;

			BitmapData bdata =  bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

			IntPtr ptr = bdata.Scan0;
			Marshal.Copy(bytesImg, 0, ptr, width * height);
			bmp.UnlockBits(bdata);

			return bmp;
		}
	}
}
