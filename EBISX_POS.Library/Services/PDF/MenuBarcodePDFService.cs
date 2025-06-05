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
        private const double CATEGORY_HEADER_HEIGHT = 30; // Height for category header
        private const double CATEGORY_SPACING = 10; // Spacing between categories

        // Font settings
        private const string FONT_FAMILY = "Arial";
        private const double ID_FONT_SIZE = 12;
        private const double NAME_FONT_SIZE = 10;
        private const double CATEGORY_FONT_SIZE = 14;

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

            // Group menus by category
            var menusByCategory = menus
                .Where(m => m.Category != null)
                .GroupBy(m => m.Category.CtgryName)
                .OrderBy(g => g.Key)
                .ToList();

            // Calculate grid layout
            var pageWidth = page.Width - (MARGIN * 2);
            var pageHeight = page.Height - (MARGIN * 2);
            var columnsPerPage = (int)(pageWidth / LABEL_WIDTH);
            var rowsPerPage = (int)((pageHeight - CATEGORY_HEADER_HEIGHT) / LABEL_HEIGHT);

            double currentY = MARGIN;
            int currentPage = 0;

            foreach (var categoryGroup in menusByCategory)
            {
                var categoryName = categoryGroup.Key;
                var categoryMenus = categoryGroup.ToList();

                // Check if we need a new page for this category
                if (currentY + CATEGORY_HEADER_HEIGHT + LABEL_HEIGHT > pageHeight - MARGIN)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = MARGIN;
                    currentPage++;
                }

                // Draw category header
                DrawCategoryHeader(gfx, categoryName, MARGIN, currentY);
                currentY += CATEGORY_HEADER_HEIGHT;

                int currentMenuIndex = 0;
                while (currentMenuIndex < categoryMenus.Count)
                {
                    // Check if we need a new page
                    if (currentY + LABEL_HEIGHT > pageHeight - MARGIN)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        currentY = MARGIN;
                        currentPage++;
                        
                        // Redraw category header on new page
                        DrawCategoryHeader(gfx, categoryName, MARGIN, currentY);
                        currentY += CATEGORY_HEADER_HEIGHT;
                    }

                    // Calculate position for current label
                    var position = currentMenuIndex % columnsPerPage;
                    var x = MARGIN + (position * LABEL_WIDTH);

                    // Draw current label
                    DrawLabel(gfx, categoryMenus[currentMenuIndex], x, currentY);

                    // Move to next row if we've filled a row
                    if (position == columnsPerPage - 1)
                    {
                        currentY += LABEL_HEIGHT;
                    }

                    currentMenuIndex++;
                }

                // Add spacing between categories
                currentY += CATEGORY_SPACING;
            }

            // Save to memory stream
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }

        private void DrawCategoryHeader(XGraphics gfx, string categoryName, double x, double y)
        {
            var categoryFont = new XFont(FONT_FAMILY, CATEGORY_FONT_SIZE, XFontStyle.Bold);
            var categoryBrush = new XSolidBrush(XColor.FromArgb(0, 0, 102)); // Dark blue color
            
            // Draw category background
            var headerRect = new XRect(x, y, gfx.PdfPage.Width - (MARGIN * 2), CATEGORY_HEADER_HEIGHT);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(230, 230, 255)), headerRect); // Light blue background
            
            // Draw category name
            gfx.DrawString(categoryName, categoryFont, categoryBrush,
                new XRect(x + 10, y + 5, headerRect.Width - 20, CATEGORY_HEADER_HEIGHT - 10),
                XStringFormats.CenterLeft);
            
            // Draw bottom border
            gfx.DrawLine(new XPen(XColor.FromArgb(0, 0, 102), 1), 
                x, y + CATEGORY_HEADER_HEIGHT, 
                x + headerRect.Width, y + CATEGORY_HEADER_HEIGHT);
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