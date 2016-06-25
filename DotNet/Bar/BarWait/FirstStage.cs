using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;

namespace BarWait {
  static class FirstStage {
    const int POINTS_LEN = 4;

    private static XDocument Load(string path) {
      var reader = new StreamReader(path);
      var doc = new XDocument(XDocument.Load(reader));
      reader.Close();
      return doc;
    }

    private static List<Tuple<Point[], int>> Parse(XDocument doc) {
      var result = new List<Tuple<Point[], int>>();

      var regions = doc.Element("regions");
      if (regions == null) {
        return result;
      }
      foreach (var region in regions.Elements("region")) {
        var points = new List<Point>();
        foreach (var point in region.Elements("point")) {
          if (points.Count < POINTS_LEN) {
            points.Add(new Point(Convert.ToDouble(point.Attribute("x").Value), Convert.ToDouble(point.Attribute("y").Value)));
          }
        }
        var type = Convert.ToInt32(region.Attribute("type").Value);
        result.Add(new Tuple<Point[], int>(points.ToArray(), type));
      }
      return result;
    }

    private static Bitmap GenerateBitmap(string path, List<Tuple<Point[], int>> regions) {
      int height;
      int width;
      using (var bitmap = new Bitmap(path)) {
        height = bitmap.Height;
        width = bitmap.Width;
      }

      var newBitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
      var bitmapData = newBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
          newBitmap.PixelFormat);
      var bytes = new byte[bitmapData.Stride * height];

      foreach (var region in regions) {
        var v = new Tuple<double, double>[region.Item1.Length];
        var points = region.Item1;
        for (int i = 0; i < region.Item1.Length; ++i) {
          v[i] = new Tuple<double, double>(points[(i + 1) % POINTS_LEN].X - points[i].X, points[(i + 1) % POINTS_LEN].Y - points[i].Y);
        }

        for (int y = 0; y < height; ++y) {
          for (int x = 0; x < width; ++x) {
            var success = true;
            for (int i = 0; i < v.Length; ++i) {
              double vx = x - points[i].X, vy = y - points[i].Y;
              if (v[i].Item1 * vy - v[i].Item2 * vx > 0) {
                success = false;
                break;
              }
            }
            if (success) {
              if (region.Item2 == 1) {
                bytes[y * bitmapData.Stride + x * 3 + 2] = 255;
              } else {
                bytes[y * bitmapData.Stride + x * 3 + 1] = 255;
              }
            }
          }
        }
      }

      Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
      newBitmap.UnlockBits(bitmapData);
      return newBitmap;
    }

    private static void Save(Image bitmap, string filename) {
      bitmap.Save(filename);
    }

    public static void Process(DirectoryInfo inputDirectory) {
      foreach (var fileInfo in inputDirectory.GetFiles()) {
        if (fileInfo.Name.EndsWith("BAR.png") || fileInfo.Name.EndsWith(".xml")) {
          continue;
        }
        var path = fileInfo.FullName;
        var doc = Load(path + ".xml");
        var regions = Parse(doc);
        using (var bitmap = GenerateBitmap(path, regions)) {
          Save(bitmap, path + "_BAR.png");
        }
      }
    }
  }
}
