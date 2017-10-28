﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class File
    {
        public Uploader GetUploader() { return new Uploader(this); }
        public bool IsImageFile
        {
            get { return MimeType.StartsWith("image"); }
        }
        public bool IsVideoFile
        {
            get { return MimeType.StartsWith("video"); }
        }
        public bool IsMusicFile
        {
            get { return MimeType.StartsWith("audio"); }
        }
        public static async Task<File> OpenSingleFileAsync()
        {
            return await OpenSingleFilePrivateAsync();
        }
        public static async Task<List<File>>OpenMultipleFilesAsync()
        {
            return await OpenMultipleFilesPrivateAsync();
        }
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        static void AddInstanceCount(int value)
        {
            System.Threading.Interlocked.Add(ref InstanceCount, value);
            InstanceCountChanged?.Invoke(InstanceCount);
        }
        public File()
        {
            AddInstanceCount(1);
        }
        ~File()
        {
            CloseReadIfNot();
            AddInstanceCount(-1);
        }
    }
}
