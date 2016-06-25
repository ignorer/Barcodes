using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using BarUtils;
using Microsoft.SqlServer.Server;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace BarWait {
  static class SecondStage {
    private static readonly Random _random = new Random();
    private static Mode _mode;

    public enum Mode {
      Train,
      Test
    }

    private static List<int> ProcessTile(byte[] bytes, int top, int left, int tileSize, int stride) {
      var result = new List<int>();
      int r = 0;
      int g = 0;
      for (int y = top; y < top + tileSize; ++y) {
        for (int x = left; x < left + tileSize; ++x) {
          if (bytes[y * stride + x * 3 + 2] == 255) {
            ++r;
          }
          if (bytes[y * stride + x * 3 + 1] == 255) {
            ++g;
          }
        }
      }
      if (r > tileSize * tileSize * 0.7) {
        result.Add(1);
      } else {
        if (r > tileSize * tileSize * 0.1) {
          return new[] {3}.ToList();
        }
      }
      if (g > tileSize * tileSize * 0.7) {
        result.Add(2);
      } else {
        if (g > tileSize * tileSize * 0.1) {
          return new[] {3}.ToList();
        }
      }
      return result;
    }

    private static Bitmap Clone(byte[] sourceBytes, int sourceStride, int left, int top, int width, int height) {
      var tile = new Bitmap(width, height);
      var tileData = tile.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
          PixelFormat.Format24bppRgb);
      var stride = tileData.Stride;
      var tileBytes = new byte[tileData.Stride * height];

      for (int i = 0; i < height; ++i) {
        for (int j = 0; j < width; ++j) {
          tileBytes[i * stride + j * 3 + 0] = sourceBytes[(i + top) * sourceStride + 3 * (j + left) + 0];
          tileBytes[i * stride + j * 3 + 1] = sourceBytes[(i + top) * sourceStride + 3 * (j + left) + 1];
          tileBytes[i * stride + j * 3 + 2] = sourceBytes[(i + top) * sourceStride + 3 * (j + left) + 2];
        }
      }
      Marshal.Copy(tileBytes, 0, tileData.Scan0, tileBytes.Length);

      tile.UnlockBits(tileData);
      return tile;
    }

    private static IEnumerable<Tuple<Bitmap, List<int>>> Split(FileInfo file, int tileSize) {
      var result = new List<Tuple<Bitmap, List<int>>>();

      var sourceImage = new Bitmap(file.FullName.Substring(0, file.FullName.Length - 8));
      var barTexture = new Bitmap(file.FullName);
      var width = barTexture.Width;
      var height = barTexture.Height;

      var barTextureData = barTexture.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
          PixelFormat.Format24bppRgb);
      var barTextureStride = barTextureData.Stride;
      var barTextureBytes = new byte[barTextureStride * height];
      Marshal.Copy(barTextureData.Scan0, barTextureBytes, 0, barTextureBytes.Length);
      barTexture.UnlockBits(barTextureData);
      barTexture.Dispose();

      var sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
          PixelFormat.Format24bppRgb);
      var stride = sourceData.Stride;
      var sourceBytes = new byte[stride * sourceData.Height];
      Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length);
      sourceImage.UnlockBits(sourceData);
      sourceImage.Dispose();

      for (int i = 0; i < height / tileSize - 1; ++i) {
        for (int j = 0; j < width / tileSize - 1; ++j) {
          var left = j * tileSize;
          var top = i * tileSize;
          var types = ProcessTile(barTextureBytes, top, left, tileSize, stride);

          if (types.Count > 0 || _random.NextDouble() < 0.5) {
            var tile = Clone(sourceBytes, sourceData.Stride, left, top, tileSize, tileSize);
            result.AddRange(
                new[] {0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180}.Select(
                    angle => new Tuple<Bitmap, List<int>>(tile.Rotate(angle), types)));
          }
        }
      }
      return result;
    }

    private static string GenerateFilename(int tileNumber) {
      var result = "";
      for (int i = 0; i < 6 - tileNumber.ToString().Length; ++i) {
        result += "0";
      }
      return result + tileNumber + ".png";
    }

    private static string GeneratePackageName(int contrastLevel) {
      if (contrastLevel == 0) {
        return "000";
      }
      if (contrastLevel > 0 && contrastLevel < 100) {
        return "0" + contrastLevel;
      }
      if (contrastLevel == 100) {
        return "100";
      }
      return "";
    }

    private static void SaveTile(DirectoryInfo dir, Bitmap tile, ICollection<int> answers, int type,
        int[] contrastLevels) {
      foreach (var contrastLevel in contrastLevels) {
        using (var tileToOutput = tile.Contrast(contrastLevel)) {
          tileToOutput.Save(dir + @"\" + GeneratePackageName(contrastLevel) + @"\" + GenerateFilename(answers.Count));
        }
      }
      tile.Dispose();
      answers.Add(type);
    }

    public static void Process(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory, int tileSize, Mode mode,
        int[] contrastLevels) {
      var answers = new List<int>();
      _mode = mode;

      foreach (var dir in outputDirectory.GetDirectories()) {
        foreach (var file in dir.GetFiles()) {
          if (Regex.IsMatch(file.Name, @"0*\d*\.png") || Regex.IsMatch(file.Name, @"answers\.txt") ||
              Regex.IsMatch(file.Name, @"features\.txt")) {
            file.Delete();
          }
        }
      }
      foreach (var contrastLevel in contrastLevels) {
        outputDirectory.CreateSubdirectory(GeneratePackageName(contrastLevel));
      }

      foreach (var tile
          in from fileInfo in inputDirectory.GetFiles()
            where fileInfo.Name.EndsWith("_BAR.png")
            select Split(fileInfo, tileSize)
            into tiles
            from tile in tiles
            select tile) {
        if (tile.Item2.Contains(3)) {
          continue;
        }
        if (tile.Item2.Count == 0) {
          SaveTile(outputDirectory, tile.Item1, answers, 0, contrastLevels);
        } else {
          foreach (var i in tile.Item2) {
            SaveTile(outputDirectory, tile.Item1, answers, i, contrastLevels);
          }
        }
      }

      foreach (var directoryInfo in outputDirectory.GetDirectories()) {
        using (var file = new StreamWriter(directoryInfo.FullName + "\\answers.txt", true)) {
          foreach (var answer in answers) {
            file.WriteLine(answer.ToString());
          }
        }
      }
    }
  }
}
