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
        private const double CATEGORY_HEADER_HEIGHT = 40; // Increased height for category header
        private const double CATEGORY_SPACING = 25; // Increased spacing between categories
        private const double CATEGORY_PADDING = 10; // Padding inside category header

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

            // Page?usable dimensions (inside margins)
            var pageWidth = page.Width - (MARGIN * 2);
            var pageHeight = page.Height - (MARGIN * 2);

            // How many labels fit horizontally
            var columnsPerPage = (int)(pageWidth / LABEL_WIDTH);

            double currentY = MARGIN;

            foreach (var categoryGroup in menusByCategory)
            {
                var categoryName = categoryGroup.Key;
                var categoryMenus = categoryGroup.ToList();

                // If not enough vertical space for header + at least one label, go to new page
                if (currentY + CATEGORY_HEADER_HEIGHT + LABEL_HEIGHT > pageHeight + MARGIN)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = MARGIN;
                }

                // Draw the category header
                DrawCategoryHeader(gfx, categoryName, MARGIN, currentY);
                currentY += CATEGORY_HEADER_HEIGHT;

                int currentMenuIndex = 0;
                while (currentMenuIndex < categoryMenus.Count)
                {
                    // If no more vertical space for one label, start a fresh page (and redraw header)
                    if (currentY + LABEL_HEIGHT > pageHeight + MARGIN)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        currentY = MARGIN;

                        DrawCategoryHeader(gfx, categoryName, MARGIN, currentY);
                        currentY += CATEGORY_HEADER_HEIGHT;
                    }

                    // X?position is column * LABEL_WIDTH
                    var position = currentMenuIndex % columnsPerPage;
                    var x = MARGIN + (position * LABEL_WIDTH);

                    // Draw the label
                    DrawLabel(gfx, categoryMenus[currentMenuIndex], x, currentY);

                    // If this was the last column in the row, move down one LABEL_HEIGHT
                    if (position == (columnsPerPage - 1))
                    {
                        currentY += LABEL_HEIGHT;
                    }

                    currentMenuIndex++;
                }

                // ??????????????????????????????????????????????????????????????
                // **HERE**: if the last drawn row was not “full,” we never incremented currentY.
                // So force one more LABEL_HEIGHT to move to the next “band” before spacing.
                // (E.g. if count=5 and columnsPerPage=3, the 2nd row only had 2 items—no Y?bump yet.)
                var itemsInLastRow = categoryMenus.Count % columnsPerPage;
                if (itemsInLastRow != 0)
                {
                    currentY += LABEL_HEIGHT;
                }
                // ??????????????????????????????????????????????????????????????

                // Finally add spacing before next category
                currentY += CATEGORY_SPACING;
            }

            // Save PDF to stream
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }

        private void DrawCategoryHeader(XGraphics gfx, string categoryName, double x, double y)
        {
            var categoryFont = new XFont(FONT_FAMILY, CATEGORY_FONT_SIZE, XFontStyle.Bold);
            var categoryBrush = new XSolidBrush(XColor.FromArgb(0, 0, 102)); // Dark blue color
            
            // Calculate header dimensions
            var headerWidth = gfx.PdfPage.Width - (MARGIN * 2);
            var headerRect = new XRect(x, y, headerWidth, CATEGORY_HEADER_HEIGHT);
            
            // Draw category background
            var backgroundBrush = new XSolidBrush(XColor.FromArgb(230, 230, 255)); // Light blue background
            gfx.DrawRectangle(backgroundBrush, headerRect);
            
            // Calculate text area with padding
            var textRect = new XRect(
                x + CATEGORY_PADDING,
                y + CATEGORY_PADDING,
                headerWidth - (CATEGORY_PADDING * 2),
                CATEGORY_HEADER_HEIGHT - (CATEGORY_PADDING * 2)
            );

            // Draw category name
            var format = new XStringFormat
            {
                Alignment = XStringAlignment.Near,
                LineAlignment = XLineAlignment.Center
            };

            // Draw category name with ellipsis if too long
            var displayName = categoryName.Length > 40 ? categoryName.Substring(0, 37) + "..." : categoryName;
            gfx.DrawString(displayName, categoryFont, categoryBrush, textRect, format);
            
            // Draw bottom border
            var pen = new XPen(XColor.FromArgb(0, 0, 102), 1.5);
            gfx.DrawLine(pen,
                x + 5,
                y + CATEGORY_HEADER_HEIGHT,
                x + headerWidth - 5,
                y + CATEGORY_HEADER_HEIGHT);

            // Add a subtle shadow effect
            var shadowPen = new XPen(XColor.FromArgb(200, 200, 200), 0.5);
            gfx.DrawLine(shadowPen,
                x + 2,
                y + CATEGORY_HEADER_HEIGHT + 1,
                x + headerWidth - 2,
                y + CATEGORY_HEADER_HEIGHT + 1);
        }

        private void DrawLabel(XGraphics gfx, Menu menu, double x, double y)
        {
            try
            {
                var contentX = x + INNER_PADDING;
                var contentY = y + INNER_PADDING;
                var contentWidth = LABEL_WIDTH - (2 * INNER_PADDING);

                // Draw label background
                var labelRect = new XRect(x, y, LABEL_WIDTH, LABEL_HEIGHT);
                gfx.DrawRectangle(XBrushes.White, labelRect);

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

                // Draw menu ID with better spacing
                var idFont = new XFont(FONT_FAMILY, ID_FONT_SIZE, XFontStyle.Bold);
                var idY = contentY + BARCODE_HEIGHT + TEXT_SPACING;
                gfx.DrawString(menu.SearchId.ToString(), idFont, XBrushes.Black,
                    new XRect(contentX, idY, contentWidth, ID_FONT_SIZE), XStringFormats.Center);

                // Draw menu name with word wrapping
                var nameFont = new XFont(FONT_FAMILY, NAME_FONT_SIZE, XFontStyle.Regular);
                var nameY = idY + ID_FONT_SIZE + TEXT_SPACING;
                var nameRect = new XRect(contentX, nameY, contentWidth, NAME_FONT_SIZE * 1.5);

                var nameFormat = new XStringFormat
                {
                    Alignment = XStringAlignment.Center,
                    LineAlignment = XLineAlignment.Center
                };

                var displayName = menu.MenuName.Length > 20 ? menu.MenuName.Substring(0, 17) + "..." : menu.MenuName;
                gfx.DrawString(displayName, nameFont, XBrushes.Black, nameRect, nameFormat);

                // Draw subtle border
                var borderPen = new XPen(XColor.FromArgb(200, 200, 200), 0.5);
                gfx.DrawRectangle(borderPen, labelRect);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating barcode for menu {menu.Id}: {ex.Message}");
            }
        }
    }
}