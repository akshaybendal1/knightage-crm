using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

/// <summary>
/// Shrinks organization logos. The app shows the logo at 36 px (sidebar, browser tab) and PDFs at about 40 pt high, but
/// people upload photos and print-size artwork -- one production logo was 4000 x 4000 px (441 KB) and was downloaded on
/// every page load, holding up the page's other requests by about 2 s.
/// </summary>
public interface ILogoImageProcessor
{
    /// <summary>
    /// The image scaled down to fit within <paramref name="maxWidth"/> x <paramref name="maxHeight"/> (a box, not a square:
    /// wide logos need width, not height), in its own format (PNG keeps its transparency), turned upright if the camera
    /// recorded a rotation. An image that already fits comes back unchanged, as does any image when it can't be processed
    /// on this server. Null when the bytes aren't a readable PNG or JPEG.
    /// </summary>
    LogoImage? Fit(byte[] data, string contentType, int maxWidth, int maxHeight);
}
