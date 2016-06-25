using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Microsoft.Win32;
using BarUtils;

namespace BarTroll {
  public partial class MainWindow {
    private enum BarcodeType {
      None,
      One,
      Two
    }

    private struct Region {
      public readonly BarcodeType Type;
      public readonly List<Point> Points;
      public readonly Polygon Polygon;

      public Region(BarcodeType type, IEnumerable<Point> points, Polygon polygon) {
        Type = type;
        Points = points.ToList();
        Polygon = polygon;
      }
    }

    private List<string> _filenames = new List<string>();
    private readonly Dictionary<object, int> _buttonToIndex = new Dictionary<object, int>();
    private int _current = -1;
    private BarcodeType _currentType = BarcodeType.None;
    private double _currentWidth, _currentHeight;
    private readonly List<Point> _points = new List<Point>();
    private readonly List<Region> _regions = new List<Region>();

    public MainWindow() {
      InitializeComponent();
    }

    private void FillImageFromFile(string filename) {
      try {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filename, UriKind.Absolute);
        bitmap.EndInit();
        _currentHeight = bitmap.PixelHeight;
        _currentWidth = bitmap.PixelWidth;
        ImageField.Source = bitmap;
        _currentType = BarcodeType.None;
        _points.Clear();
        _regions.Clear();
        MainCanvas.Children.Clear();
      } catch (Exception) {
        MessageBox.Show("Can't load image " + filename, "Error with loading", MessageBoxButton.OK,
            MessageBoxImage.Error, MessageBoxResult.OK);
      }
    }

    private void UpdateImage() {
      FillImageFromFile(_filenames[_current]);
    }

    private void LoadFilelist(IEnumerable<string> filenames) {
      _filenames = filenames.ToList();
      _buttonToIndex.Clear();
      ImageList.Items.Clear();
      var counter = 0;
      foreach (var filename in _filenames) {
        var b = new Button { Content = filename.Split('\\').Last(), Height = 40, Focusable = false };
        b.Click += ListBoxButtonClick;
        _buttonToIndex[b] = counter;
        ImageList.Items.Add(b);
        ++counter;
      }
      _current = 0;
      FillImageFromFile(_filenames[_current]);
    }

    private void ListBoxButtonClick(object sender, RoutedEventArgs e) {
      _current = _buttonToIndex[sender];
      FillImageFromFile(_filenames[_buttonToIndex[sender]]);
    }

    private void Open(object sender = null, RoutedEventArgs e = null) {
      FileDialog dialog = new OpenFileDialog();
      dialog.Title = "Choose image";
      dialog.Filter = "Images |*.tiff;*.tif;*.gif;*.png;*.bmp;*.jpg;*.jpeg";
      var result = dialog.ShowDialog();

      if (result == true) {
        var filename = dialog.FileName;
        LoadFilelist(new[] { filename });
      }
    }

    private void Export(object sender = null, RoutedEventArgs e = null) {
      if (_regions.Count == 0) {
        return;
      }
      var doc = new XDocument();
      var regions = new XElement("regions");
      foreach (var region in _regions) {
        var regionTag = new XElement("region");
        if (region.Type != BarcodeType.One && region.Type != BarcodeType.Two) {
          continue;
        }
        regionTag.Add(new XAttribute("type", region.Type == BarcodeType.One ? "1" : "2"));
        foreach (var point in region.Points) {
          var pointTag = new XElement("point");
          pointTag.Add(new XAttribute("x", point.X.ToString("####")));
          pointTag.Add(new XAttribute("y", point.Y.ToString("####")));
          regionTag.Add(pointTag);
        }
        regions.Add(regionTag);
      }
      doc.Add(regions);
      doc.Save(_filenames[_current] + ".xml");
    }

    private void ImageFieldOnDrop(object sender, DragEventArgs e) {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
        return;
      }
      LoadFilelist((string[])e.Data.GetData(DataFormats.FileDrop));
    }

    private void ImageListOnDrop(object sender, DragEventArgs e) {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
        return;
      }
      LoadFilelist((string[])e.Data.GetData(DataFormats.FileDrop));
    }

    private void ImageFieldClick(object sender = null, MouseButtonEventArgs e = null) {
      if (_currentType == BarcodeType.None) {
        return;
      }
      var pos = Mouse.GetPosition(ImageField);
      _points.Add(pos);

      if (_points.Count != 4) {
        return;
      }
      var polygon = new Polygon {
        Points =
          new PointCollection(
              _points.Select(
                  x =>
                      new Point(x.X + (ImageGrid.ActualWidth - ImageField.ActualWidth) / 2,
                          x.Y + (ImageGrid.ActualHeight - ImageField.ActualHeight) / 2))),
        Fill =
          new SolidColorBrush(_currentType == BarcodeType.One
              ? new Color { R = 255, A = 127 }
              : new Color { G = 255, A = 127 })
      };
      _regions.Add(new Region(_currentType,
          _points.Select(
              x =>
                  new Point(x.X * _currentWidth / ImageField.ActualWidth, x.Y * _currentHeight / ImageField.ActualHeight)),
          polygon));
      MainCanvas.Children.Add(_regions.Last().Polygon);
      _points.Clear();
      _currentType = BarcodeType.None;
    }

    private void Exit(object sender = null, RoutedEventArgs e = null) {
      Close();
    }

    private void ShowHelp(object sender = null, RoutedEventArgs e = null) {
      MessageBox.Show(
          "F1 - help\nCtrl+Q - open\nCtrl+E - export\nCtrl+W - previous\nCtrl+S - next\n" +
          "Ctrl+Space - export and next\nCtrl+1 - select 1D-barcode\nCtrl+2 - select 2D-barcode\n" +
          "Ctrl+Z - undo\nEsc - exit", "Help", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
    }

    private void MainWindowOnKeyDown(object sender, KeyEventArgs e) {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
        switch (e.Key) {
          case Key.W:
            if (_current <= 0) {
              return;
            }
            --_current;
            UpdateImage();
            break;
          case Key.S:
            if (_current >= _filenames.Count - 1) {
              return;
            }
            ++_current;
            UpdateImage();
            break;
          case Key.D1:
            _currentType = BarcodeType.One;
            _points.Clear();
            break;
          case Key.D2:
            _currentType = BarcodeType.Two;
            _points.Clear();
            break;
          case Key.Space:
            Export();
            if (_current >= _filenames.Count - 1) {
              return;
            }
            ++_current;
            UpdateImage();
            break;
          case Key.E:
            Export();
            break;
          case Key.Q:
            Open();
            break;
          case Key.Z:
            if (_currentType == BarcodeType.None) {
              if (_regions.Count > 0) {
                MainCanvas.Children.Remove(_regions.Last().Polygon);
                _regions.RemoveAt(_regions.Count - 1);
              }
            } else {
              _currentType = BarcodeType.None;
              _points.Clear();
            }
            break;
        }
      } else {
        switch (e.Key) {
          case Key.Escape:
            Exit();
            break;
          case Key.F1:
            ShowHelp();
            break;
        }
      }
    }
  }
}