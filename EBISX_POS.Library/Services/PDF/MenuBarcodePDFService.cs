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
        private const double INNER_PADDING = 5;    // Padding inside each label
        private const double LABEL_WIDTH = 175; // Width of each label in points
        private const double LABEL_HEIGHT = 70; // Height of each label in points
        private const double BARCODE_HEIGHT = 25; // Height of barcode in points
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
                var contentX = x + INNER_PADDING;
                var contentY = y + INNER_PADDING;
                var contentWidth = LABEL_WIDTH - (2 * INNER_PADDING);

                // Generate barcode
                var bitMatrix = _barcodeWriter.Encode(menu.SearchId.ToString());
                using var bitmap = new Bitmap(bitMatrix.Width, bitMatrix.Height);
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

                using var barcodeImage = XImage.FromStream(ms);
                gfx.DrawImage(barcodeImage, contentX, contentY, contentWidth, BARCODE_HEIGHT);

                // Draw menu ID
                var idFont = new XFont(FONT_FAMILY, ID_FONT_SIZE, XFontStyle.Bold);
                var idY = contentY + BARCODE_HEIGHT + TEXT_SPACING;
                gfx.DrawString(menu.SearchId.ToString(), idFont, XBrushes.Black,
                    new XRect(contentX, idY, contentWidth, ID_FONT_SIZE), XStringFormats.Center);

                // Draw menu name
                var nameFont = new XFont(FONT_FAMILY, NAME_FONT_SIZE, XFontStyle.Regular);
                var nameY = idY + ID_FONT_SIZE + TEXT_SPACING;

                var displayName = menu.MenuName.Length > 20 ? menu.MenuName[..17] + "..." : menu.MenuName;

                gfx.DrawString(displayName, nameFont, XBrushes.Black,
                    new XRect(contentX, nameY, contentWidth, NAME_FONT_SIZE), XStringFormats.Center);

                // Optional border
                gfx.DrawRectangle(XPens.LightGray, x, y, LABEL_WIDTH, LABEL_HEIGHT);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating barcode for menu {menu.Id}: {ex.Message}");
            }
        }
    }
}