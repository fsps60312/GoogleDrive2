using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    partial class NetworkStatusWithSpeedBarViewModel
    {
        static class ImageProcessor
        {
            private static void WriteAsync(Stream stream, byte[] data)
            {
                stream.Write(data, 0, data.Length);
            }
            private static void WriteAsync(Stream stream, string data)
            {
                WriteAsync(stream, Encoding.UTF8.GetBytes(data));
            }
            private static void WriteAsync(Stream stream, UInt32 v)
            {
                WriteAsync(stream, new byte[4] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF) });
            }
            private static void WriteAsync(Stream stream, UInt16 v)
            {
                WriteAsync(stream, new byte[2] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) });
            }
            private static double GetMax(List<double>s)
            {
                if (s.Count == 0) return double.Epsilon;
                s.Sort();
                var c = Math.Max(1, s.Count / 5);
                return Math.Max(double.Epsilon, s/*.GetRange(s.Count - c, c)*/.Average() * 2.5);
            }
            public static Stream GetImageStream(int width, int height, List<Tuple<double, double>> points)
            {
                //width = 100;
                //height = 40;
                // The File Header
                UInt32 bfSize = 122 + 4 * (UInt32)width * (UInt32)height;
                UInt32 bfOffBits = 122;
                UInt32 biSizeImage = 4 * (UInt32)width * (UInt32)height;
                Stream stream = new MemoryStream();
                WriteAsync(stream, "BM");
                WriteAsync(stream, bfSize);
                WriteAsync(stream, new byte[4]);
                WriteAsync(stream, bfOffBits);
                // The Image Header
                WriteAsync(stream, (UInt32)108);
                WriteAsync(stream, (UInt32)width);
                WriteAsync(stream, (UInt32)height);
                WriteAsync(stream, (UInt16)1);
                WriteAsync(stream, (UInt16)32);
                WriteAsync(stream, (UInt32)3);
                WriteAsync(stream, biSizeImage);
                WriteAsync(stream, (UInt32)0);
                WriteAsync(stream, (UInt32)0);
                WriteAsync(stream, (UInt32)0);
                WriteAsync(stream, (UInt32)0);
                // The Color Table
                WriteAsync(stream, (UInt32)0xFF << 16);//R
                WriteAsync(stream, (UInt32)0xFF << 8);//G
                WriteAsync(stream, (UInt32)0xFF << 0);//B
                WriteAsync(stream, (UInt32)0xFF << 24);//A
                WriteAsync(stream, " niW");
                WriteAsync(stream, new byte[0x24]);
                for (int i = 0; i < 3; i++) WriteAsync(stream, (UInt32)0);

                // Start of the Pixel Array (the bitmap Data)
                List<Tuple<int, double>> hList = new List<Tuple<int, double>>();
                {
                    double max = GetMax(points.Select(p => p.Item2).ToList());
                    int count = 0, now = 0;
                    double sum = 0;
                    for (int i = 0; ; i++)
                    {
                        while (i == points.Count || now < Math.Min(width - 1, (int)(points[i].Item1 * width)))
                        {
                            if (now >= width) goto index_endProcessing;
                            if (count > 0)
                            {
                                hList.Add(new Tuple<int, double>(now, sum / count));
                                sum = 0;
                                count = 0;
                            }
                            now++;
                        }
                        if (i == points.Count) break;
                        count++;
                        sum += points[i].Item2 / max;
                    }
                    index_endProcessing:;
                }
                double[] hs = new double[width];
                if (hList.Count > 0)
                {
                    if (hList.First().Item1 != 0) hList.Insert(0, new Tuple<int, double>(0, 0));
                    if (hList.Last().Item1 != width - 1) hList.Insert(0, new Tuple<int, double>(width - 1, 0));
                    foreach (var v in hList) hs[v.Item1] = v.Item2;
                    for (int i = 1; i < hList.Count; i++)
                    {
                        int l = hList[i - 1].Item1, r = hList[i].Item1;
                        if (l + 1 < r)
                        {
                            for (int j = l + 1; j < r; j++)
                            {
                                hs[j] = (hs[l] * (r - j) + hs[r] * (j - l)) / (r - l);
                            }
                        }
                    }
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        WriteAsync(stream, (UInt32)(hs[j] == -1 ? 0x00000000 : (hs[j] * (height - 1) <= i ? 0x00000000 : 0xFF00AFAF)));
                    }
                }
                //MyLogger.Debug($"stream position: {stream.Position} {bfSize} {bfOffBits} {biSizeImage}");
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }
}