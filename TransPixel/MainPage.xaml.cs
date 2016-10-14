using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TransPixel
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private WriteableBitmap wb;

        public MainPage()
        {
            this.InitializeComponent();
            //wb = new WriteableBitmap(100, 100);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                BitmapDecoder decoder = null;

                using (IRandomAccessStream stream = await file.OpenReadAsync())
                {
                    decoder = await BitmapDecoder.CreateAsync(stream);

                    // Get the first frame
                    BitmapFrame bitmapFrame = await decoder.GetFrameAsync(0);

                    // Save the resolution (will be used for saving the file later)

                    // Get the pixels
                    PixelDataProvider dataProvider =
                        await bitmapFrame.GetPixelDataAsync(BitmapPixelFormat.Bgra8,
                                                            BitmapAlphaMode.Premultiplied,
                                                            new BitmapTransform(),
                                                            ExifOrientationMode.RespectExifOrientation,
                                                            ColorManagementMode.ColorManageToSRgb);

                    byte[] pixels = dataProvider.DetachPixelData();

                    // Create WriteableBitmap and set the pixels
                    wb = new WriteableBitmap((int)bitmapFrame.PixelWidth,
                                                                 (int)bitmapFrame.PixelHeight);

                    using (Stream pixelStream = wb.PixelBuffer.AsStream())
                    {
                        await pixelStream.WriteAsync(pixels, 0, pixels.Length);
                    }

                    // Invalidate the WriteableBitmap and set as Image source
                    wb.Invalidate();
                    imgRes.Source = wb;
                    //wb = bitmap;
                }

                // Get the source bitmap pixels
                WriteableBitmap srcBitmap = wb;
                byte[] srcPixels = new byte[4 * srcBitmap.PixelWidth * srcBitmap.PixelHeight];

                using (Stream pixelStream = srcBitmap.PixelBuffer.AsStream())
                {
                    await pixelStream.ReadAsync(srcPixels, 0, srcPixels.Length);
                }

                // Create a destination bitmap and pixels array
                WriteableBitmap dstBitmap =
                        new WriteableBitmap(srcBitmap.PixelWidth, srcBitmap.PixelHeight);
                byte[] dstPixels = new byte[4 * dstBitmap.PixelWidth * dstBitmap.PixelHeight];


                for (int i = 0; i < srcPixels.Length; i += 4)
                {
                    double b = (double)srcPixels[i] / 255.0;
                    double g = (double)srcPixels[i + 1] / 255.0;
                    double r = (double)srcPixels[i + 2] / 255.0;

                    byte a = srcPixels[i + 3];

                    double k = (0.21 * r + 0.71 * g + 0.07 * b) * 255;
                    byte f = Convert.ToByte(k);

                    dstPixels[i] = f;
                    dstPixels[i + 1] = f;
                    dstPixels[i + 2] = f;
                    dstPixels[i + 3] = a;

                }

                // Move the pixels into the destination bitmap
                using (Stream pixelStream = dstBitmap.PixelBuffer.AsStream())
                {
                    await pixelStream.WriteAsync(dstPixels, 0, dstPixels.Length);
                }
                dstBitmap.Invalidate();

                // Display the new bitmap
                img.Source = dstBitmap;
            }
        }


    }
}

