using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BarFfuu.Properties;
using BarUtils;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace BarFfuu {
  public partial class MainWindow : Window {
    private readonly Dictionary<object, int> _buttonToIndex = new Dictionary<object, int>();
    private List<Tuple<string, int, int>> _errors = new List<Tuple<string, int, int>>();
    private List<List<double>> _features = new List<List<double>>();
    private Button _current;

    public MainWindow() {
      InitializeComponent();
      PathBox.Text = Settings.Default.Root;
    }

    private void ShowHelp(object sender = null, RoutedEventArgs e = null) {
      MessageBox.Show("F1 - help\nEsc - exit", "Help", MessageBoxButton.OK,
          MessageBoxImage.Information, MessageBoxResult.OK);
    }

    private void Exit(object sender = null, RoutedEventArgs e = null) {
      Close();
    }

    private void MainWindowOnClosing(object sender, CancelEventArgs e) {
      Settings.Default.Root = PathBox.Text;
      Settings.Default.Save();
    }

    private void FillImageFromFile(string filename) {
      try {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filename, UriKind.Absolute);
        bitmap.EndInit();
        ImageField.Source = bitmap;
      } catch (Exception) {
        MessageBox.Show("Can't load image " + filename, "Error with loading", MessageBoxButton.OK, MessageBoxImage.Error,
            MessageBoxResult.OK);
      }
    }

    private void UpdateImage() {
      if (_current == null) {
        ActualBlock.Text = "Actual: ";
        PredictedBlock.Text = "Predicted: ";
        return;
      }
      FillImageFromFile(_errors[_buttonToIndex[_current]].Item1);
      ActualBlock.Text = "Actual: " + _errors[_buttonToIndex[_current]].Item2;
      PredictedBlock.Text = "Predicted: " + _errors[_buttonToIndex[_current]].Item3;
    }

    private void ImageButtonOnClick(object sender, RoutedEventArgs routedEventArgs) {
      _current = (Button) sender;
      UpdateImage();
    }

    private void Load(ICollection<string> filenames) {
      _buttonToIndex.Clear();
      ImageList.Items.Clear();
      foreach (var filename in filenames) {
        var button = new Button {Content = filename.Split('\\').Last(), Height = 20};
        button.Click += ImageButtonOnClick;
        _buttonToIndex[button] = _buttonToIndex.Count;
        ImageList.Items.Add(button);
      }
      if (filenames.Count > 0) {
        _current = (Button) _buttonToIndex.First().Key;
      }
      UpdateImage();
    }

    private void MainWindowOnKeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Escape:
          Exit();
          break;
        case Key.F1:
          ShowHelp();
          break;
      }
    }

    private void RootPathTextBoxDoubleClick(object sender, MouseButtonEventArgs e) {
      var path = Utility.ShowFolderDialog("Choose folder with XMLs");
      if (path != null) {
        PathBox.Text = path;
      }
    }

    private void Process(object sender, RoutedEventArgs e) {
      var featuresFile = new FileInfo(PathBox.Text + @"\features.txt");
      var errorsFile = new FileInfo(PathBox.Text + @"\errors.txt");
      if (!featuresFile.Exists || !errorsFile.Exists) {
        Utility.Error("Invalid path");
        return;
      }

      using (var stream = new StreamReader(errorsFile.FullName)) {
        _errors =
            stream.ReadToEnd()
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(x => x.Length > 0)
                .Select(x => x.Split(' '))
                .Select(
                    tmp =>
                        new Tuple<string, int, int>(PathBox.Text + @"\" + tmp[0], Convert.ToInt32(tmp[1]),
                            Convert.ToInt32(tmp[2])))
                .ToList();
      }
      using (var stream = new StreamReader(featuresFile.FullName)) {
        _features =
            stream.ReadToEnd()
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(
                    line =>
                        line.Split(' ')
                            .Where(x => x.Length > 0)
                            .Select(x => Convert.ToDouble(x, new NumberFormatInfo {NumberDecimalSeparator = "."}))
                            .ToList())
                .ToList();
      }

      Load(_errors.Select(x => PathBox.Text + @"\" + x.Item1).ToList());
    }

    private void CopyFeaturesToClipboard(object sender, RoutedEventArgs e) {
      if (_current == null) {
        return;
      }
      var text = _features[Convert.ToInt32(_current.Content.ToString().Split('.')[0])].Aggregate("",
          (current, feature) => current + string.Format("{0}\r\n", feature));
      Clipboard.SetText(text);
    }
  }
}
