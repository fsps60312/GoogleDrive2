using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    partial class FileUploadBarViewModel
    {
        static class ImageProcessor
        {
            private static async Task WriteAsync(Stream stream,byte[]data)
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
            private static async Task WriteAsync(Stream stream,string data)
            {
                await WriteAsync(stream, Encoding.UTF8.GetBytes(data));
            }
            private static async Task WriteAsync(Stream stream,UInt32 v)
            {
                await WriteAsync(stream, new byte[4] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF) });
            }
            private static async Task WriteAsync(Stream stream,UInt16 v)
            {
                await WriteAsync(stream, new byte[2] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) });
            }
            public static async Task<Stream>GetImageStream(int width,int height,List<Tuple<double,double>>rawPoints)
            {
                //width = 100;
                //height = 40;
                // The File Header
                UInt32 bfSize = 122 + 4 * (UInt32)width * (UInt32)height;
                UInt32 bfOffBits = 122;
                UInt32 biSizeImage= 4 * (UInt32)width * (UInt32)height;
                Stream stream = new MemoryStream();
                await WriteAsync(stream, "BM");
                await WriteAsync(stream, bfSize);
                await WriteAsync(stream, new byte[4]);
                await WriteAsync(stream, bfOffBits);
                // The Image Header
                await WriteAsync(stream, (UInt32)108);
                await WriteAsync(stream, (UInt32)width);
                await WriteAsync(stream, (UInt32)height);
                await WriteAsync(stream, (UInt16)1);
                await WriteAsync(stream, (UInt16)32);
                await WriteAsync(stream, (UInt32)3);
                await WriteAsync(stream, biSizeImage);
                await WriteAsync(stream, (UInt32)0);
                await WriteAsync(stream, (UInt32)0);
                await WriteAsync(stream, (UInt32)0);
                await WriteAsync(stream, (UInt32)0);
                // The Color Table
                await WriteAsync(stream, (UInt32)0xFF << 16);//R
                await WriteAsync(stream, (UInt32)0xFF << 8);//G
                await WriteAsync(stream, (UInt32)0xFF << 0);//B
                await WriteAsync(stream, (UInt32)0xFF << 24);//A
                await WriteAsync(stream, " niW");
                await WriteAsync(stream, new byte[0x24]);
                for (int i = 0; i < 3; i++) await WriteAsync(stream, (UInt32)0);

                // Start of the Pixel Array (the bitmap Data)
                List<Tuple<double, double>> points = new List<Tuple<double, double>>();
                if(rawPoints.Count<=width*5)
                {
                    foreach (var p in rawPoints) points.Add(p);
                }
                else
                {
                    for (int i = 0; i < width * 5; i++) points.Add(rawPoints[i * rawPoints.Count / (width * 5)]);
                }
                double[] hs = new double[width];
                for (int i = 0; i < width; i++) hs[i] = -1;
                {
                    double max = double.Epsilon;
                    foreach (var p in points) max = Math.Max(max, p.Item2);
                    int count = 0, now = 0;
                    double sum = 0;
                    for (int i = 0; i < points.Count; i++)
                    {
                        while (now <= points[i].Item1 * width)
                        {
                            if (now >= width) goto index_endProcessing;
                            hs[now] = (count == 0 ? -1 : sum / count);
                            sum = 0;
                            count = 0;
                            now++;
                        }
                        count++;
                        sum += points[i].Item2 / max;
                    }
                index_endProcessing:;
                    while(now<width)
                    {
                        hs[now] = (count == 0 ? -1 : sum / count);
                        sum = 0;
                        count = 0;
                        now++;
                    }
                    for(int i=width-1;i>0;i--)
                    {
                        if (hs[i - 1] == -1) hs[i - 1] = hs[i];
                    }
                }
                for (int i=0;i<height;i++)
                {
                    for(int j=0;j<width;j++)
                    {
                        await WriteAsync(stream, (UInt32)(hs[j] == -1 ? 0x3F000000 : (hs[j] * (height - 1) <= i ? 0x7F00FFFF : 0x7FFFFF00)));
                    }
                }
                MyLogger.Debug($"stream position: {stream.Position} {bfSize} {bfOffBits} {biSizeImage}");
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }
}
