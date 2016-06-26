using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Numerics;
using BarUtils;

namespace BarWait {
  static class ThirdStage {
    private static void ProcessPackage(DirectoryInfo package) {
      var featuresForAllTiles = new List<List<double>>();

      foreach (var fileInfo in package.GetFiles()) {
        if (fileInfo.Extension != ".png") {
          continue;
        }

        var tile = new Bitmap(fileInfo.FullName);
        var width = tile.Width;
        var height = tile.Height;
        var tileData = tile.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);
        var stride = tileData.Stride;
        var tileBytes = new byte[stride * height];
        Marshal.Copy(tileData.Scan0, tileBytes, 0, tileBytes.Length);
        tile.UnlockBits(tileData);
        tile.Dispose();

        var image = new byte[height][];
        for (int y = 0; y < height; ++y) {
          image[y] = new byte[width];
          for (int x = 0; x < width; ++x) {
            var index = y * stride + x * 3;
            image[y][x] = (byte) (0.3 * tileBytes[index + 2] + 0.59 * tileBytes[index + 1] + 0.11 * tileBytes[index]);
          }
        }

        var features = new List<double>();
        features.AddRange(Features.StandardDeviationFeatures(image));
        features.AddRange(Features.LocalBinaryPatternFeatures(image));
        features.AddRange(Features.StructureTensorFeatures(image));
        featuresForAllTiles.Add(features);
      }

      using (var writer = new StreamWriter(package.FullName + "\\features.txt")) {
        foreach (var features in featuresForAllTiles) {
          foreach (var feature in features) {
            var _ = feature.ToString("N",
                new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "" });
            writer.Write((_.EndsWith(".00") ? _.Substring(0, _.Length - 3) : _) + " ");
          }
          writer.WriteLine();
        }
      }
    }

    public static void Process(DirectoryInfo outputDirectory) {
      foreach (var directoryInfo in outputDirectory.GetDirectories()) {
        ProcessPackage(directoryInfo);
      }
    }
  }
}
