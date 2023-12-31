﻿using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace TCP_Screenshot.Models
{
    [Serializable]
    public class ScreenshotData
    {
        public DateTime Time { get; set; } = DateTime.Now;
        
        public Bitmap? Screenshot { get; set; }

    }
}