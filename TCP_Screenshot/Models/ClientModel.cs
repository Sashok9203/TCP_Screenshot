﻿using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using Shared_Data;
using System.Text.Json;
using System;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text;
using static System.Resources.ResXFileRef;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TCP_Screenshot.Models
{
    class ClientModel : INotifyPropertyChanged
    {
        private readonly IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 8080);

        private bool manual = true;

        private bool manualScreenshot
        {
            get => manual;
            set 
            {
                manual = value;
                OnPropertyChanged(nameof(AutoManualButtonName));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private TcpClient client ;

        private async void autoManual()
        {
            manualScreenshot = !manualScreenshot;
            if (!manualScreenshot)
                await getScreenShotAsync(Command.AutoScreenshotStart);
        }

        private async Task saveSelectedImages(object o)
        {
            if (o is IEnumerable<object>  data)
                await saveImagesAsync(data);
        }

        private async Task saveImagesAsync(IEnumerable<object> images)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageConverter converter = new();
                string path = fbd.SelectedPath;
                await Parallel.ForEachAsync(images, async (item, ct) =>
                {
                    if (item is ScreenshotData scr)
                    {
                        Bitmap? bmp = default;
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>{bmp = BitmapFromSource(scr.Screenshot);}));
                        string savePath = Path.Combine(path, $"screenshot_{scr.Time:d.M.yyyy_(HH-mm-ss-ff)}.png");
                        await File.WriteAllBytesAsync(savePath, (byte[])converter.ConvertTo(bmp, typeof(byte[])), ct);
                    }
                   
                });
            }
        }

        private void showImage(object o)
        {
            if (o is ScreenshotData data)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "view_temp.png");
                Bitmap bmp = BitmapFromSource(data.Screenshot);
                bmp.Save(tempPath, ImageFormat.Png);
                new Process
                {
                    StartInfo = new ProcessStartInfo(tempPath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            
        }

        private void deleteImage(object o)
        {
            if (o is IEnumerable<object> data)
            {
               foreach (ScreenshotData item in data.ToArray())
                    Screenshots.Remove(item);
            }
        }

        private  BitmapSource ConvertBitmap(Bitmap source)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        private  Bitmap BitmapFromSource(BitmapSource source)
        {
            Bitmap bmp = new (source.PixelWidth,source.PixelHeight,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                                                ImageLockMode.WriteOnly,
                                                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty,data.Scan0,data.Height * data.Stride,data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        private async Task getScreenShotFromServerAsync(StreamReader reader)
        {
            string json = await reader.ReadLineAsync();

            byte[]? bytes = JsonSerializer.Deserialize<byte[]>(json);

            using MemoryStream ms = new(bytes);

            Bitmap bmp = new(ms);

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Screenshots.Add(new()
                {
                    Screenshot = ConvertBitmap(bmp)
                });
            }));
        }

        private async Task getScreenShotAsync(Command command)
        {
            ClientCommand clientCommand = new()
            {
                Command = command,
                Period = Period
            };

            try
            {
                client = new();
                await client.ConnectAsync(endPoint);
                using StreamWriter writer = new(client.GetStream());
                writer.WriteLine(JsonSerializer.Serialize(clientCommand));
                writer.Flush();
                using StreamReader reader = new(client.GetStream());
                await Task.Run(async () =>
                {
                    do { await getScreenShotFromServerAsync(reader); }
                    while (manualScreenshot == false);
                });
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Exception"); }
        }
        
        public int Period { get; set; }
        public string AutoManualButtonName => manualScreenshot ? "Auto" : "Manual";
        public RelayCommand Get => new(async (o) => await getScreenShotAsync(Command.Screenshot),(o)=>manualScreenshot);
        public RelayCommand Show => new((o) => showImage(o));
        public RelayCommand Delete => new((o) => deleteImage(o));
        public RelayCommand Save => new(async (o) => await saveSelectedImages(o));
        public RelayCommand Exit => new((o) => Environment.Exit(0));
        public RelayCommand Auto => new((o) =>  autoManual());
        public RelayCommand SaveAll => new(async (o) =>await saveImagesAsync(Screenshots),(o)=>manualScreenshot);
        public RelayCommand DeleteAll => new((o) => Screenshots.Clear());

        public ObservableCollection<ScreenshotData> Screenshots { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    }
}
