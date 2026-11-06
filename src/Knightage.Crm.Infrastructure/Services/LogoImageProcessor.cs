using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.Services;

/// <summary>
/// ILogoImageProcessor on System.Drawing (GDI+), which only runs on Windows -- production is IIS on Windows, and so is
/// local development. Anywhere else the logo is passed through unchanged rather than failing.
/// </summary>
public class LogoImageProcessor : ILogoImageProcessor
{
    // A 2 MB upload can still be a 20000 x 20000 PNG that needs 1.6 GB to decode; refuse anything above ~5000 x 5000.
    private const long MaxPixels = 25_000_000;
    private const int ExifOrientation = 0x0112;

    private readonly ILogger<LogoImageProcessor> _logger;

    public LogoImageProcessor(ILogger<LogoImageProcessor> logger)
    {
        _logger = logger;
    }

    public LogoImage? Fit(byte[] data, string contentType, int maxWidth, int maxHeight)
    {
        return OperatingSystem.IsWindows() ? FitOnWindows(data, contentType, maxWidth, maxHeight) : new LogoImage(data, contentType);
    }

    [SupportedOSPlatform("windows")]
    private LogoImage? FitOnWindows(byte[] data, string contentType, int maxWidth, int maxHeight)
    {
        try
        {
            using var input = new MemoryStream(data);
            // validateImageData: false reads only the header here, so the size check runs before the pixels are decoded.
            using var source = Image.FromStream(input, useEmbeddedColorManagement: false, validateImageData: false);
            var isPng = source.RawFormat.Guid == ImageFormat.Png.Guid;
            if (!isPng && source.RawFormat.Guid != ImageFormat.Jpeg.Guid)
            {
                return null;
            }

            if ((long)source.Width * source.Height > MaxPixels)
            {
                _logger.LogWarning("Logo of {Width} x {Height} px refused (too many pixels).", source.Width, source.Height);
                return null;
            }

            var actualType = isPng ? "image/png" : "image/jpeg";
            var turned = TurnUpright(source);
            if (!turned && source.Width <= maxWidth && source.Height <= maxHeight)
            {
                return new LogoImage(data, actualType);
            }

            var scale = Math.Min(1.0, Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height));
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));
            using var target = new Bitmap(width, height, isPng ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(target))
            using (var wrap = new ImageAttributes())
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                wrap.SetWrapMode(WrapMode.TileFlipXY); // no faint border from sampling past the edge
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrap);
            }

            using var output = new MemoryStream();
            if (isPng)
            {
                target.Save(output, ImageFormat.Png);
            }
            else
            {
                var jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using var quality = new EncoderParameters(1);
                quality.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                target.Save(output, jpeg, quality);
            }

            return new LogoImage(output.ToArray(), actualType);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or OutOfMemoryException)
        {
            // GDI+ reports bytes it can't read as an image as ArgumentException / OutOfMemoryException.
            _logger.LogWarning(ex, "Could not read a logo image ({ContentType}, {Bytes} bytes).", contentType, data.Length);
            return null;
        }
        catch (Exception ex) when (ex is TypeInitializationException or DllNotFoundException or PlatformNotSupportedException)
        {
            // GDI+ itself isn't usable on this server: keep the logo as it is rather than refusing it.
            _logger.LogError(ex, "Logo resizing is unavailable on this server; the logo is kept unchanged.");
            return new LogoImage(data, contentType);
        }
    }

    /// <summary>Applies the EXIF orientation a phone camera records instead of turning the pixels; true when it turned.</summary>
    [SupportedOSPlatform("windows")]
    private static bool TurnUpright(Image image)
    {
        if (!image.PropertyIdList.Contains(ExifOrientation))
        {
            return false;
        }

        var turn = image.GetPropertyItem(ExifOrientation)?.Value is { Length: > 0 } value ? value[0] switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => (RotateFlipType?)null,
        } : null;
        if (turn is null)
        {
            return false;
        }

        image.RotateFlip(turn.Value);
        return true;
    }
}
