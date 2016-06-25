using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BarUtils {
  public static class Features {
    public static double[] StandardDeviationFeatures(double[][] image) {
      const int verticalPartCount = 2;
      const int horizontalPartCount = 2;

      var features = new List<double>();
      var subtileYSize = image.Length / verticalPartCount;
      var subtileXSize = image[0].Length / horizontalPartCount;

      for (int i = 0; i < verticalPartCount; ++i) {
        for (int j = 0; j < horizontalPartCount; ++j) {
          var average = 0.0;
          for (int y = 0; y < subtileYSize; ++y) {
            for (int x = 0; x < subtileXSize; ++x) {
              average += image[y + i * subtileYSize][x + j * subtileXSize];
            }
          }
          average /= subtileXSize * subtileYSize;

          var std = 0.0;
          for (int y = 0; y < subtileYSize; ++y) {
            for (int x = 0; x < subtileXSize; ++x) {
              std += (image[y + i * subtileYSize][x + j * subtileXSize] - average) *
                     (image[y + i * subtileYSize][x + j * subtileXSize] - average);
            }
          }
          features.Add(Math.Sqrt(std / (image.Length * image[0].Length)));
        }
      }
      return features.ToArray();
    }

    public static double[] LocalBinaryPatternFeatures(double[][] image) {
      var height = image.Length;
      var width = image[0].Length;
//      var features = new List<double>();
//      var histogram = new List<double>();
//      for (int i = 0; i < 256; ++i) {
//        histogram.Add(0);
//      }
      var histogram = new double[256];
      for (int i = 1; i < height - 1; ++i) {
        for (int j = 1; j < width - 1; ++j) {
          var index = (image[i - 1][j - 1] <= image[i][j] ? 0 : 1) * 128 +
                      (image[i - 1][j * 1] <= image[i][j] ? 0 : 1) * 64 +
                      (image[i - 1][j + 1] <= image[i][j] ? 0 : 1) * 32 +
                      (image[i * 1][j - 1] <= image[i][j] ? 0 : 1) * 16 +
                      (image[i * 1][j + 1] <= image[i][j] ? 0 : 1) * 8 +
                      (image[i + 1][j - 1] <= image[i][j] ? 0 : 1) * 4 +
                      (image[i + 1][j * 1] <= image[i][j] ? 0 : 1) * 2 + (image[i + 1][j + 1] <= image[i][j] ? 0 : 1);
          histogram[index] += 1;
        }
      }
      return histogram;

//      double average = 0;
//      double squareAverage = 0;
//      foreach (double value in histogram) {
//        average += value;
//        squareAverage += value * value;
//      }
//      average = (average - (histogram[0] + histogram.Last())) / (histogram.Count - 2);
//      squareAverage = Math.Sqrt((squareAverage - histogram[0] + histogram.Last()) / (histogram.Count - 2));
//
//      double deviation = 0;
//      double standardDeviation = 0;
//      int moreThanAverageCount = 0;
//      int moreThan50Count = 0;
//      int moreThan90Count = 0;
//      int moreThan120Count = 0;
//      int moreThan150Count = 0;
//      for (var i = 0; i < histogram.Count; i++) {
//        if (i == 0 || i == histogram.Count - 1) {
//          continue;
//        }
//        var value = histogram[i];
//        if (value > 50) {
//          ++moreThan50Count;
//        }
//        if (value > 90) {
//          ++moreThan90Count;
//        }
//        if (value > 120) {
//          ++moreThan120Count;
//        }
//        if (value > 150) {
//          ++moreThan150Count;
//        }
//        if (value > average) {
//          ++moreThanAverageCount;
//        }
//        deviation += Math.Abs(value - average);
//        standardDeviation += Math.Pow(value - average, 2);
//      }
//      standardDeviation = Math.Sqrt(standardDeviation);
//
//      features.Add(average);
//      features.Add(squareAverage);
//      features.Add(deviation);
//      features.Add(standardDeviation);
//      features.Add(moreThan50Count);
//      features.Add(moreThan90Count);
//      features.Add(moreThan120Count);
//      features.Add(moreThan150Count);
//      features.Add(moreThanAverageCount);
//      features.AddRange(histogram);
//      return features.ToArray();
    }

    public static double[] StructureTensorFeatures(double[][] image) {
      const int verticalPartCount = 2;
      const int horizontalPartCount = 2;

      var height = image.Length;
      var width = image[0].Length;

      var features = new List<double>();
      var subtileYSize = height / verticalPartCount;
      var subtileXSize = width / horizontalPartCount;
      double e1 = 0.0, e2 = 0.0;

      for (int i = 0; i < verticalPartCount; ++i) {
        for (int j = 0; j < horizontalPartCount; ++j) {
          double a = 0, b = 0, c = 0;
          for (int y = 1; y < subtileYSize - 1; ++y) {
            for (int x = 1; x < subtileXSize - 1; ++x) {
              var indexY = y + i * subtileYSize;
              var indexX = x + j * subtileXSize;
              var ix = image[indexY][indexX + 1] - image[indexY][indexX - 1];
              var iy = image[indexY + 1][indexX] - image[indexY - 1][indexX];
              a += ix * ix;
              b += ix * iy;
              c += iy * iy;
            }
          }
          var t1 = Math.Abs(a + c - Complex.Abs(Complex.Sqrt((a - c) * (a - c) - 4 * b * b))) / 2;
          var t2 = Math.Abs(a + c + Complex.Abs(Complex.Sqrt((a - c) * (a - c) - 4 * b * b))) / 2;
          e1 += Math.Min(t1, t2);
          e2 += Math.Max(t1, t2);
        }
      }

      features.Add(e1);
      features.Add(e2);
      return features.ToArray();
    }

    public static double[] GetFeatures(this Bitmap bitmap) {
      var height = bitmap.Height;
      var width = bitmap.Width;        
      var tileData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);
      var stride = tileData.Stride;
      var tileBytes = new byte[stride * height];
      Marshal.Copy(tileData.Scan0, tileBytes, 0, tileBytes.Length);
      bitmap.UnlockBits(tileData);

      var image = new double[height][];
        for (int y = 0; y < height; ++y) {
          image[y] = new double[width];
          for (int x = 0; x < width; ++x) {
            var index = y * stride + x * 3;
            image[y][x] = 0.3 * tileBytes[index + 2] + 0.59 * tileBytes[index + 1] + 0.11 * tileBytes[index];
          }
        }
      var result = new List<double>();
      result.AddRange(StandardDeviationFeatures(image));
      result.AddRange(LocalBinaryPatternFeatures(image));
      result.AddRange(StructureTensorFeatures(image));
      return result.ToArray();
    } 
  }
}
