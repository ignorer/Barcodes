using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using BarWait.Properties;
using BarUtils;

namespace BarWait {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
      PathBox.Text = Settings.Default.RootPath;
      DatasetModeBox.Text = Settings.Default.Mode;
      TileSizeBox.Text = Settings.Default.TileSize.ToString();
      FirstStageFlag.IsChecked = Settings.Default.FirstStageEnabled;
      SecondStageFlag.IsChecked = Settings.Default.SecondStageEnabled;
      ThirdStageFlag.IsChecked = Settings.Default.ThirdStageEnabled;
    }

    private void Exit(object sender = null, RoutedEventArgs e = null) {
      Close();
    }

    private void MainWindowOnClosing(object sender, CancelEventArgs e) {
      Settings.Default.RootPath = PathBox.Text;
      Settings.Default.Mode = DatasetModeBox.Text;
      Settings.Default.TileSize = Convert.ToInt32(TileSizeBox.Text);
      if (FirstStageFlag.IsChecked != null) {
        Settings.Default.FirstStageEnabled = FirstStageFlag.IsChecked.Value;
      }
      if (SecondStageFlag.IsChecked != null) {
        Settings.Default.SecondStageEnabled = SecondStageFlag.IsChecked.Value;
      }
      if (ThirdStageFlag.IsChecked != null) {
        Settings.Default.ThirdStageEnabled = ThirdStageFlag.IsChecked.Value;
      }
      Settings.Default.Save();
    }

    private void ShowHelp(object sender = null, RoutedEventArgs e = null) {
      MessageBox.Show("F1 - help\nSpace - process\nEsc - exit", "Help", MessageBoxButton.OK,
          MessageBoxImage.Information, MessageBoxResult.OK);
    }

    private void RootPathTextBoxDoubleClick(object sender, MouseButtonEventArgs e) {
      var path = Utility.ShowFolderDialog("Choose folder with XMLs");
      if (path != null) {
        PathBox.Text = path;
      }
    }

    private void Process(object sender = null, RoutedEventArgs routedEventArgs = null) {
      DirectoryInfo inputDirectory;
      DirectoryInfo outputDirectory;

      // getting parameters
      var tileSize = Convert.ToInt32(TileSizeBox.Text);
      var mode = DatasetModeBox.Text == "Train" ? SecondStage.Mode.Train : SecondStage.Mode.Test;

      // validate paths
      try {
        inputDirectory = new DirectoryInfo(PathBox.Text + @"\Raw\" + DatasetModeBox.Text + @"\");
        outputDirectory = new DirectoryInfo(PathBox.Text + @"\Tiles\" + tileSize + @"\" + DatasetModeBox.Text + @"\");
        if (!inputDirectory.Exists) {
          throw new Exception("Invalid paths");
        }
        if (!outputDirectory.Exists) {
          outputDirectory.Create();
        }
      } catch (Exception) {
        Utility.Error("Invalid paths");
        return;
      }

      try {
        if (FirstStageFlag.IsChecked != null && FirstStageFlag.IsChecked.Value) {
          FirstStage.Process(inputDirectory);
        }
        HybridStage.Process(inputDirectory, outputDirectory, tileSize, new []{0, 100});
//        if (SecondStageFlag.IsChecked != null && SecondStageFlag.IsChecked.Value) {
//          SecondStage.Process(inputDirectory, outputDirectory, tileSize, mode, new[] {0, 100});
//        }
//        if (ThirdStageFlag.IsChecked != null && ThirdStageFlag.IsChecked.Value) {
//          ThirdStage.Process(outputDirectory);
//        }
      } catch (Exception e) {
        Utility.Error(e.Message);
      }
    }

    private void MainWindowOnKeyDown(object sender, KeyEventArgs e) {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftShift) ||
          Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) {
        return;
      }
      switch (e.Key) {
        case Key.Escape:
          Exit();
          break;
        case Key.F1:
          ShowHelp();
          break;
        case Key.Space:
          Process();
          break;
      }
    }
  }
}
