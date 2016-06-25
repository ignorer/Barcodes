using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BarUtils;

namespace BarWait {
  class HybridStage {
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

    private static IEnumerable<Tuple<double[], List<int>>> Split(FileInfo file, int tileSize, int contrastLevel) {
      var result = new List<Tuple<double[], List<int>>>();

      var sourceImage = new Bitmap(file.FullName.Substring(0, file.FullName.Length - 8)).Contrast(contrastLevel);
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

          using (var tile = Clone(sourceBytes, sourceData.Stride, left, top, tileSize, tileSize)) {
            foreach (var angle in new[] { 0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165 }) {
              using (var rotatedTile = tile.Rotate(angle)) {
                result.Add(new Tuple<double[], List<int>>(rotatedTile.GetFeatures(), types));
              }
            }
          }
        }
      }
      return result;
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

    public static void Process(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory, int tileSize,
        int[] contrastLevels) {
      foreach (var contrastLevel in contrastLevels) {
        var dir = outputDirectory.CreateSubdirectory(GeneratePackageName(contrastLevel));

        var answers = new List<int>();
        var features = new List<double[]>();
        var files = inputDirectory.GetFiles().Where(x => x.Name.EndsWith("_BAR.png")).ToList();
        var level = contrastLevel;
        var tileGroups = files.Select(x => Split(x, tileSize, level)).ToList();
        foreach (var tileGroup in tileGroups) {
          foreach (var tile in tileGroup) {
            if (tile.Item2.Contains(3)) {
              continue;
            }
            if (tile.Item2.Count == 0) {
              features.Add(tile.Item1);
              answers.Add(0);
            } else {
              foreach (var i in tile.Item2) {
                features.Add(tile.Item1);
                answers.Add(i);
              }
            }
          }
        }
        tileGroups.Clear();
        tileGroups = null;

        using (var file = new StreamWriter(dir.FullName + @"\features.txt")) {
          foreach (var featuresForObject in features) {
            foreach (var feature in featuresForObject) {
              var _ = feature.ToString("N",
                  new NumberFormatInfo {NumberDecimalSeparator = ".", NumberGroupSeparator = ""});
              file.Write((_.EndsWith(".00") ? _.Substring(0, _.Length - 3) : _) + " ");
            }
            file.WriteLine();
          }
        }
        using (var file = new StreamWriter(dir.FullName + @"\answers.txt")) {
          foreach (var answer in answers) {
            file.WriteLine(answer.ToString());
          }
        }
        features.Clear();
        features = null;
        answers.Clear();
        answers = null;
      }
    }
  }
}
