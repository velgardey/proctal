using System.Windows.Media;
using System.Windows.Media.Imaging;
using Proctal.Core;
namespace Proctal.Desktop.Platform;
public record ScreenshotItem(ImageAttachment Payload,BitmapSource Preview)
{public string Name=>Payload.Name;}
public static class ScreenshotService
{
    public static ScreenshotItem Capture()
    {
        var screen=System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);var rect=screen.Bounds;
        using var bitmap=new System.Drawing.Bitmap(rect.Width,rect.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using(var graphics=System.Drawing.Graphics.FromImage(bitmap))graphics.CopyFromScreen(rect.Location,System.Drawing.Point.Empty,rect.Size,System.Drawing.CopyPixelOperation.SourceCopy);
        using var stream=new MemoryStream();bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);stream.Position=0;
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
        return FromImage(image,$"Screen {DateTime.Now:HH-mm-ss}.png");
    }
    public static ScreenshotItem Load(string path)
    {
        if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("Choose an image smaller than 20 MB.");
        using var stream=File.OpenRead(path);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
        return FromImage(image,Path.GetFileName(path));
    }
    public static ScreenshotItem FromImage(BitmapSource source,string name)
    {
        BitmapSource image=source;var scale=Math.Min(1,2560d/Math.Max(source.PixelWidth,source.PixelHeight));
        if(scale<1){image=new TransformedBitmap(source,new ScaleTransform(scale,scale));image.Freeze();}
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var encoded=new MemoryStream();encoder.Save(encoded);
        if(encoded.Length>5*1024*1024)throw new InvalidDataException("The image is too large after encoding. Use a smaller crop.");
        return new ScreenshotItem(new ImageAttachment(name,"data:image/png;base64,"+Convert.ToBase64String(encoded.ToArray())),image);
    }
}
