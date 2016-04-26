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

		public static Bitmap GetGrayBitmap(int width, int height, int stride, IntPtr ptr0)
		{
			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
			ColorPalette palette = bmp.Palette;
			Color[] _entries = palette.Entries;
			for (int i = 0; i < 256; i++)
			{
				Color b = new Color();
				b = Color.FromArgb((byte)i, (byte)i, (byte)i);
				_entries[i] = b;
			}
			bmp.Palette = palette;

			byte[] bytesImg = new byte[stride];
			Marshal.Copy(ptr0, bytesImg, 0, stride);

			BitmapData bdata =  bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			IntPtr ptr = bdata.Scan0;
			Marshal.Copy(bytesImg, 0, ptr, stride);
			bmp.UnlockBits(bdata);

			return bmp;
		}
	}
}
