using EBISX_POS.API.Data;
using EBISX_POS.API.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;

namespace EBISX_POS.API.Services.PDF
{
    public class MenuBarcodePDFService
    {
        static MenuBarcodePDFService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // Constants for layout
        private const double MARGIN = 20; // Margin in points
        private const double LABEL_WIDTH = 180; // Width of each label in points
        private const double LABEL_HEIGHT = 90; // Height of each label in points
        private const double BARCODE_HEIGHT = 40; // Height of barcode in points
        private const double TEXT_SPACING = 5; // Spacing between elements in points

        // Font settings
        private const string FONT_FAMILY = "Arial";
        private const double ID_FONT_SIZE = 12;
        private const double NAME_FONT_SIZE = 10;

        private readonly BarcodeWriter<Bitmap> _barcodeWriter;

        public MenuBarcodePDFService()
        {
            _barcodeWriter = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = (int)LABEL_WIDTH,
                    Height = (int)BARCODE_HEIGHT,
                    Margin = 0,
                    PureBarcode = true
                }
            };
        }

        public byte[] GenerateMenuBarcodeLabels(List<Menu> menus)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Calculate grid layout
            var pageWidth = page.Width - (MARGIN * 2);
            var pageHeight = page.Height - (MARGIN * 2);
            var columnsPerPage = (int)(pageWidth / LABEL_WIDTH);
            var rowsPerPage = (int)(pageHeight / LABEL_HEIGHT);

            int currentMenuIndex = 0;
            while (currentMenuIndex < menus.Count)
            {
                // Add new page if needed
                if (currentMenuIndex > 0 && currentMenuIndex % (columnsPerPage * rowsPerPage) == 0)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                }

                // Calculate position for current label
                var position = currentMenuIndex % (columnsPerPage * rowsPerPage);
                var row = position / columnsPerPage;
                var column = position % columnsPerPage;

                var x = MARGIN + (column * LABEL_WIDTH);
                var y = MARGIN + (row * LABEL_HEIGHT);

                // Draw current label
                DrawLabel(gfx, menus[currentMenuIndex], x, y);

                currentMenuIndex++;
            }

            // Save to memory stream
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }

        private void DrawLabel(XGraphics gfx, Menu menu, double x, double y)
        {
            try
            {
                // Generate barcode as bit matrix first
                var bitMatrix = _barcodeWriter.Encode(menu.Id.ToString());
                using var bitmap = new Bitmap(bitMatrix.Width, bitMatrix.Height);

                // Convert bit matrix to bitmap
                for (int i = 0; i < bitMatrix.Width; i++)
                {
                    for (int j = 0; j < bitMatrix.Height; j++)
                    {
                        bitmap.SetPixel(i, j, bitMatrix[i, j] ? Color.Black : Color.White);
                    }
                }

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                // Create and dispose XImage properly
                using var barcodeImage = XImage.FromStream(ms);
                gfx.DrawImage(barcodeImage, x, y, LABEL_WIDTH, BARCODE_HEIGHT);

                // Draw menu ID
                var idFont = new XFont(FONT_FAMILY, ID_FONT_SIZE, XFontStyle.Bold);
                var idY = y + BARCODE_HEIGHT + TEXT_SPACING;
                var idRect = new XRect(x, idY, LABEL_WIDTH, ID_FONT_SIZE);
                gfx.DrawString(menu.Id.ToString(), idFont, XBrushes.Black, idRect, XStringFormats.Center);

                // Draw menu name
                var nameFont = new XFont(FONT_FAMILY, NAME_FONT_SIZE, XFontStyle.Regular);
                var nameY = idY + ID_FONT_SIZE + TEXT_SPACING;
                var nameRect = new XRect(x, nameY, LABEL_WIDTH, NAME_FONT_SIZE);
                
                // Truncate name if too long
                var displayName = menu.MenuName;
                if (displayName.Length > 20)
                {
                    displayName = displayName.Substring(0, 17) + "...";
                }
                
                gfx.DrawString(displayName, nameFont, XBrushes.Black, nameRect, XStringFormats.Center);

                // Draw border around label (optional)
                gfx.DrawRectangle(XPens.LightGray, x, y, LABEL_WIDTH, LABEL_HEIGHT);
            }
            catch (Exception ex)
            {
                // Log error or handle it appropriately
                Console.WriteLine($"Error generating barcode for menu {menu.Id}: {ex.Message}");
            }
        }
    }
}