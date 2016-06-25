using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FileDialog = Microsoft.Win32.FileDialog;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace BarUtils {
  public static class Utility {
    private static TimeSpan _timeSpan = new TimeSpan(0);
    private static DateTime _lastStart = new DateTime();
    private static string _lastPath = "C:\\";

    public static void NotImplementedHandler(string message = "") {
      MessageBox.Show(message != "" ? message : "This feature hasn't been implemented yet", "Not implemented",
          MessageBoxButton.OK, MessageBoxImage.Hand, MessageBoxResult.OK);
    }

    public static void DebugInfo(string message) {
      MessageBox.Show(message, "Debug info", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);
    }

    public static string ShowFolderDialog(string description = "Choose folder", string path = null) {
      var dialog = new FolderBrowserDialog {
        Description = description,
        RootFolder = Environment.SpecialFolder.MyComputer,
        SelectedPath = path != null ? null : _lastPath,
        ShowNewFolderButton = false
      };

      var result = dialog.ShowDialog();
      _lastPath = dialog.SelectedPath;
      return result == DialogResult.OK ? dialog.SelectedPath : null;
    }

    public static string[] ShowFileDialog(string title = "Choose image", string path = null, string filter = "") {
      FileDialog dialog = new OpenFileDialog {
        Title = title,
        InitialDirectory = path != null ? null : _lastPath,
        Filter = filter
      };
      var result = dialog.ShowDialog();

      if (result != true) {
        return null;
      }
      var info = new FileInfo(dialog.FileName);
      if (info.Directory != null) {
        _lastPath = info.Directory.FullName;
      }
      return dialog.FileNames;
    }

    public static string[] ShowChooseImageDialog(string path = null) {
      return ShowFileDialog("Choose image", path ?? _lastPath, "Images |*.tiff;*.gif;*.png;*.bmp;*.jpg;*.jpeg");
    }

    public static void Error(string message) {
      MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
    }

    public static void DropClock() {
      _timeSpan = TimeSpan.Zero;
    }

    public static void StartClock() {
      _lastStart = DateTime.Now;
    }

    public static void StopClock() {
      _timeSpan += DateTime.Now - _lastStart;
    }

    public static TimeSpan GetTimeSpan() {
      return _timeSpan;
    }

    public static Bitmap Contrast(this Bitmap sourceBitmap, int threshold) {
      if (threshold == 0) {
        return sourceBitmap.Clone(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
            PixelFormat.Format24bppRgb);
      }
      var sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
      var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
      Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
      sourceBitmap.UnlockBits(sourceData);

      double contrastLevel = threshold / 100.0;
      var meanIntensity = 0.0;
      for (int i = 0; i < sourceBitmap.Height; ++i) {
        for (int j = 0; j < sourceBitmap.Width; ++j) {
          var index = i * sourceData.Stride + 3 * j;
          meanIntensity += 0.11 * pixelBuffer[index] + 0.59 * pixelBuffer[index + 1] + 0.3 * pixelBuffer[index + 2];
        }
      }
      meanIntensity = meanIntensity / (sourceBitmap.Height * sourceBitmap.Width) * 0.8;
      var k = 1 - Math.Abs(contrastLevel * contrastLevel * contrastLevel);

      for (int i = 0; i < sourceBitmap.Height; ++i) {
        for (int j = 0; j < sourceBitmap.Width; ++j) {
          var index = i * sourceData.Stride + 3 * j;
          var intensity = 0.11 * pixelBuffer[index] + 0.59 * pixelBuffer[index + 1] + 0.3 * pixelBuffer[index + 2];

          if (contrastLevel < 0) {
            var blue = meanIntensity + (pixelBuffer[index] - meanIntensity) * k;
            var green = meanIntensity + (pixelBuffer[index + 1] - meanIntensity) * k;
            var red = meanIntensity + (pixelBuffer[index + 2] - meanIntensity) * k;

            pixelBuffer[index] = (byte) (blue < 0 ? 0 : blue > 255 ? 255 : blue);
            pixelBuffer[index + 1] = (byte) (green < 0 ? 0 : green > 255 ? 255 : green);
            pixelBuffer[index + 2] = (byte) (red < 0 ? 0 : red > 255 ? 255 : red);
          } else {
            if (intensity < meanIntensity) {
              var blue = pixelBuffer[index] * k;
              var green = pixelBuffer[index] * k;
              var red = pixelBuffer[index] * k;

              pixelBuffer[index] = (byte) (blue < 0 ? 0 : blue > 255 ? 255 : blue);
              pixelBuffer[index + 1] = (byte) (green < 0 ? 0 : green > 255 ? 255 : green);
              pixelBuffer[index + 2] = (byte) (red < 0 ? 0 : red > 255 ? 255 : red);
            } else {
              var blue = 255 - (255 - pixelBuffer[index]) * k;
              var green = 255 - (255 - pixelBuffer[index + 1]) * k;
              var red = 255 - (255 - pixelBuffer[index + 2]) * k;
              
              pixelBuffer[index] = (byte)(blue < 0 ? 0 : blue > 255 ? 255 : blue);
              pixelBuffer[index + 1] = (byte) (green < 0 ? 0 : green > 255 ? 255 : green);
              pixelBuffer[index + 2] = (byte) (red < 0 ? 0 : red > 255 ? 255 : red);
            }
          }
        }
      }

//      for (int i = 0; i < pixelBuffer.Length; ++i) {
//        var value = pixelBuffer[i];
//        double newValue;
//        if (contrastLevel < 0) {
//          newValue = meanIntensity + (value - meanIntensity) * k;
//        } else {
//          if (pixelBuffer[i] < meanIntensity) {
//            newValue = value * k;
//          } else {
//            newValue = 255 - (255 - value) * k;
//          }
//        }
//        pixelBuffer[i] = (byte) (newValue < 0 ? 0 : newValue > 255 ? 255 : newValue);
//      }

      var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
      var resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),
          ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
      Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
      resultBitmap.UnlockBits(resultData);

      return resultBitmap;
    }

    public static Bitmap Rotate(this Bitmap sourceBitmap, double degrees) {
      var angle = degrees * Math.PI / 180.0;
      var cos = Math.Cos(-angle);
      var sin = Math.Sin(-angle);
      var width = sourceBitmap.Width;
      var height = sourceBitmap.Height;
      var xOffset = width / 2;
      var yOffset = height / 2;
      
      // copy image to byte array
      var sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
          PixelFormat.Format24bppRgb);
      var stride = sourceData.Stride;
      var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
      Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
      sourceBitmap.UnlockBits(sourceData);
      var resultBuffer = new byte[pixelBuffer.Length];
      
      // process rotation
      for (int row = 0; row < height; ++row) {
        var x = -xOffset * cos - (row - yOffset) * sin + xOffset;
        var y = -xOffset * sin + (row - yOffset) * cos + yOffset;
        var newIndex = row * stride;
        for (int col = 0; col < width; ++col, x += cos, y += sin, newIndex += 3) {
          if (y >= 0 && y < height && x >= 0 && x < width) {
            var index = (int) y * stride + (int) x * 3;
            resultBuffer[newIndex] = pixelBuffer[index];
            resultBuffer[newIndex + 1] = pixelBuffer[index + 1];
            resultBuffer[newIndex + 2] = pixelBuffer[index + 2];
          } else {
            resultBuffer[newIndex] = resultBuffer[newIndex + 1] = resultBuffer[newIndex + 2] = 255;
          }
        }
      }

      // create new bitmap
      var resultBitmap = new Bitmap(width, height);
      var resultData = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
          PixelFormat.Format24bppRgb);
      Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
      resultBitmap.UnlockBits(resultData);
      return resultBitmap;
    }

    public static Bitmap ResizeImage(this Bitmap image, int width, int height) {
      var destRect = new Rectangle(0, 0, width, height);
      var destImage = new Bitmap(width, height);

      destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

      using (var graphics = Graphics.FromImage(destImage)) {
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using (var wrapMode = new ImageAttributes()) {
          wrapMode.SetWrapMode(WrapMode.TileFlipXY);
          graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        }
      }

      return destImage;
    }
  }
}
