using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BarGod.Properties;
using BarUtils;
using Color = System.Windows.Media.Color;
using SolidBrush = System.Windows.Media.SolidColorBrush;
using Image = System.Windows.Controls.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace BarGod {
  public partial class MainWindow : Window {
    private struct ImageInfo {
      public int MipmapLevel { get; set; }
      public Dictionary<int, List<Tuple<List<double>, Rectangle>>> ProcessResult { get; set; }
    }

    private string _filename = "";
    
    public MainWindow() {
      InitializeComponent();
      InterpreterField.Text = Settings.Default.PythonInterpreterPath;
      ScriptField.Text = Settings.Default.PythonScriptPath;
      ModelField.Text = Settings.Default.ModelPath;
      DataField.Text = Settings.Default.DataPath;
    }

    private void SaveSettings(object sender = null, CancelEventArgs cancelEventArgs = null) {
      Settings.Default.PythonInterpreterPath = InterpreterField.Text;
      Settings.Default.PythonScriptPath = ScriptField.Text;
      Settings.Default.ModelPath = ModelField.Text;
      Settings.Default.DataPath = DataField.Text;
      Settings.Default.Save();
    }

    private List<Tuple<List<double>, Rectangle>> ProcessImage(Bitmap bitmap, int size) {
      var result = new List<Tuple<List<double>, Rectangle>>();
      var width = bitmap.Width;
      var height = bitmap.Height;

      var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
      var bitmapBytes = new byte[data.Height * data.Stride];
      Marshal.Copy(data.Scan0, bitmapBytes, 0, bitmapBytes.Length);
      bitmap.UnlockBits(data);

      var image = new byte[height * width * 3];
      for (int i = 0; i < height; ++i) {
        for (int j = 0; j < width; j++) {
          var index = i * data.Stride + j * 3;
          image[i * width + j] = (byte) (0.3 * bitmapBytes[index + 2] + 0.59 * bitmapBytes[index + 1] +
                                         0.11 * bitmapBytes[index]);
        }
      }

      int size2 = (int) (size * (1 - 0.5));
      int n = (height - size) / size2 + 1;
      int m = (width - size) / size2 + 1;
      var tile = new byte[size][];
      for (int i = 0; i < size; ++i) {
        tile[i] = new byte[size];
      }
      for (int i = 0; i < n; ++i) {
        for (int j = 0; j < m; ++j) {
          var left = size2 * j;
          var top = size2 * i;
          for (int y = top; y < top + size; ++y) {
            Array.Copy(image, y * width + left, tile[y - top], 0, size);
          }
          result.Add(new Tuple<List<double>, Rectangle>(new List<double>(), new Rectangle(left, top, size, size)));
          result.Last().Item1.AddRange(Features.StandardDeviationFeatures(tile));
          result.Last().Item1.AddRange(Features.LocalBinaryPatternFeatures(tile));
          result.Last().Item1.AddRange(Features.StructureTensorFeatures(tile));
        }
      }

      return result;
    }

    private void FullProcess(Bitmap sourceImage) {
      var contrastLevels = new[] {0, 100};
      var sizes = new[] {64};
      var mipmapLevels = new[] {0};

      var contrastToBitmapInfo = new Dictionary<int, ImageInfo>();
      foreach (var contrastLevel in contrastLevels) {
        // process images
        using (var bitmap = sourceImage.Contrast(contrastLevel)) {
          foreach (var mipmapLevel in mipmapLevels) {
            var k = (int) Math.Pow(2, mipmapLevel);
            using (var mipmap = bitmap.ResizeImage(bitmap.Width / k, bitmap.Height / k)) {
              contrastToBitmapInfo[contrastLevel] = new ImageInfo {
                MipmapLevel = mipmapLevel,
                ProcessResult = new Dictionary<int, List<Tuple<List<double>, Rectangle>>>()
              };
              foreach (var size in sizes) {
                var processImageResult = ProcessImage(mipmap, size);
                contrastToBitmapInfo[contrastLevel].ProcessResult[size] = processImageResult;
              }
            }
          }
        }

        // dump data
        foreach (var size in sizes) {
          using (var writer = new StreamWriter(DataField.Text + @"/features_" + contrastLevel + "_" + size + ".txt")) {
            foreach (var bitmapInfo in contrastToBitmapInfo[contrastLevel].ProcessResult[size]) {
              foreach (var feature in bitmapInfo.Item1) {
                var _ = feature.ToString("N",
                    new NumberFormatInfo {NumberDecimalSeparator = ".", NumberGroupSeparator = ""});
                writer.Write((_.EndsWith(".00") ? _.Substring(0, _.Length - 3) : _) + " ");
              }
              writer.WriteLine();
            }
          }
        }
      }

      // call python script
      var process = Process.Start(InterpreterField.Text, ScriptField.Text + " " + ModelField.Text + " " + DataField.Text);
      process.WaitForExit();
      if (process.ExitCode != 0) {
        Utility.Error("Error in python script launch.\nError code: " + process.ExitCode);
        return;
      }

      var colors = new[] {Color.FromArgb(50, 255, 0, 0), Color.FromArgb(50, 0, 255, 0)};
      var rectangles = new List<Tuple<Rectangle, Color>>();
      foreach (var contrastLevel in contrastLevels) {
        foreach (var size in sizes) {
          using (var reader = new StreamReader(DataField.Text + @"\answers_" + contrastLevel + "_" + size + ".txt")) {
            var answers =
                reader.ReadToEnd()
                    .Replace("\r\n", "\n")
                    .Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToList();

            var index = 0;
            foreach (var mipmapLevel in mipmapLevels) {
              var items =
                  contrastToBitmapInfo.Where(x => x.Value.MipmapLevel == mipmapLevel)
                      .Select(x => x.Value)
                      .First()
                      .ProcessResult[size];
              foreach (var item in items) {
                if (answers[index] != 0) {
                  var rect = item.Item2;
                  rect.Height = (int) (rect.Height / Math.Pow(2, mipmapLevel));
                  rect.Width = (int) (rect.Width / Math.Pow(2, mipmapLevel));
                  rectangles.Add(new Tuple<Rectangle, Color>(rect, colors[answers[index] - 1]));
                }

                ++index;
              }
            }
          }
        }
      }

      var multiplier = ImageField.ActualHeight / sourceImage.Height;
      var left = (ImageGrid.ActualWidth - ImageField.ActualWidth) / 2;
      var top = (ImageGrid.ActualHeight - ImageField.ActualHeight) / 2;
      foreach (var rectangle in rectangles) {
        DrawingField.Children.Add(new Polygon {
          Points =
            new PointCollection(
                new[] {
                  new Point(rectangle.Item1.Left, rectangle.Item1.Top),
                  new Point(rectangle.Item1.Left, rectangle.Item1.Bottom),
                  new Point(rectangle.Item1.Right, rectangle.Item1.Bottom),
                  new Point(rectangle.Item1.Right, rectangle.Item1.Top)
                }.Select(
                    point => new Point(point.X * multiplier + left, point.Y * multiplier + top))),
          Fill = new SolidColorBrush(rectangle.Item2)
        });
      }
    }

    private void FillImageFromFile(string filename) {
      try {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(filename, UriKind.Absolute);
        bitmapImage.EndInit();
        ImageField.Source = bitmapImage;
        DrawingField.Children.Clear();
        _filename = filename;
      } catch (Exception) {
        MessageBox.Show("Can't load image " + filename, "Error with loading", MessageBoxButton.OK,
            MessageBoxImage.Error, MessageBoxResult.OK);
      }
    }

    private void ImageFieldOnDrop(object sender, DragEventArgs e) {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
        return;
      }
      FillImageFromFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
    }

    private void MainWindowOnKeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Space:
          if (_filename != "" && new FileInfo(_filename).Exists) {
            FullProcess(new Bitmap(_filename));
          }
          break;
        case Key.Escape:
          SaveSettings();
          Close();
          break;
      }
    }

    private void RunButtonClick(object sender, RoutedEventArgs e) {
      if (_filename != "" && new FileInfo(_filename).Exists) {
        FullProcess(new Bitmap(_filename));
      }
    }
  }
}
