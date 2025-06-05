using EBISX_POS.API.Services.DTO.Report;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Globalization;
using System.Text;

namespace EBISX_POS.API.Services.PDF
{
    public class SalesReportPDFService
    {
        static SalesReportPDFService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private string _businessName;
        private string _address;
        private string _tin;

        public SalesReportPDFService()
        {
            _businessName = "N/A";
            _address = "N/A";
            _tin = "N/A";
        }

        public void UpdateBusinessInfo(string businessName, string address, string tin)
        {
            _businessName = businessName;
            _address = address;
            _tin = tin;
        }

        public byte[] GenerateSalesReportPDF(List<SalesReportDTO> sales, DateTime fromDate, DateTime toDate)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            page.Width = XUnit.FromInch(13.0);
            page.Height = XUnit.FromInch(8.5);
            var gfx = XGraphics.FromPdfPage(page);
            var phCulture = new CultureInfo("en-PH");

            // Fonts
            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
            var normalFont = new XFont("Arial", 9, XFontStyle.Regular);
            var smallFont = new XFont("Arial", 8, XFontStyle.Regular);

            double y = 40;
            double margin = 30;
            double tableTop = 0;
            double pageWidth = page.Width - margin * 2;

            // Header
            gfx.DrawString(_businessName, titleFont, XBrushes.DarkBlue, new XPoint(margin, y));
            y += 18;
            gfx.DrawString(_address, normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 12;
            gfx.DrawString($"TIN {_tin}", normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 18;
            gfx.DrawString("SALES REPORT", headerFont, XBrushes.DarkBlue, new XPoint(margin, y));
            y += 14;
            gfx.DrawString($"From {fromDate:MM-dd-yyyy} To {toDate:MM-dd-yyyy}", normalFont, XBrushes.Black, new XPoint(margin, y));
            y += 18;
            tableTop = y;

            // Table columns - Adjusted widths to sum up to 1.00
            var columns = new[]
            {
                ("DATE", 0.055),     // Reduced from 0.06
                ("INVOICE", 0.073),  // Reduced from 0.08
                ("MENU NAME", 0.184), // Reduced from 0.20
                ("UNIT", 0.046),     // Reduced from 0.05
                ("QTY", 0.037),      // Reduced from 0.04
                ("COST", 0.046),     // Reduced from 0.05
                ("PRICE", 0.064),    // Reduced from 0.07
                ("GROUP", 0.138),    // Reduced from 0.15
                ("BARCODE", 0.092),  // Reduced from 0.10
                ("STATUS", 0.073),   // Reduced from 0.08
                ("TOTAL\nCOST", 0.064), // Reduced from 0.07
                ("REVENUE", 0.064),  // Reduced from 0.07
                ("PROFIT", 0.064)    // Reduced from 0.07
            };
            // Total: 1.000 (100%)

            double[] colWidths = columns.Select(c => c.Item2 * pageWidth).ToArray();
            double headerRowHeight = 30;
            double rowHeight = 18;

            var formats = new[]
            {
                XStringFormats.Center,
                XStringFormats.CenterLeft,
                XStringFormats.CenterLeft,
                XStringFormats.Center,
                XStringFormats.Center,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.Center,
                XStringFormats.CenterLeft,
                XStringFormats.Center,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight,
                XStringFormats.CenterRight
            };

            // Draw table header
            double headerY = y;
            double x = margin;
            for (int i = 0; i < columns.Length; i++)
            {
                var rect = new XRect(x, headerY, colWidths[i], headerRowHeight);
                gfx.DrawRectangle(XBrushes.LightGray, rect);
                var headerLines = columns[i].Item1.Split('\n');
                double lineHeight = headerRowHeight / headerLines.Length;
                for (int j = 0; j < headerLines.Length; j++)
                {
                    var lineRect = new XRect(x, headerY + j * lineHeight, colWidths[i], lineHeight);
                    gfx.DrawString(headerLines[j], smallFont, XBrushes.Black, lineRect, formats[i]);
                }
                x += colWidths[i];
            }
            y += headerRowHeight;

            // Table rows
            foreach (var sale in sales)
            {
                // Check if adding the next row will exceed the page height (with a bottom margin)
                if (y + rowHeight > page.Height - margin)
                {
                    // Add a new page
                    page = document.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Landscape;
                    page.Width = XUnit.FromInch(13.0);
                    page.Height = XUnit.FromInch(8.5);
                    gfx = XGraphics.FromPdfPage(page);
                    y = margin; // Reset y position for the new page

                    // Redraw table header on the new page
                    double currentHeaderY = y;
                    double currentX = margin;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        var rect = new XRect(currentX, currentHeaderY, colWidths[i], headerRowHeight);
                        gfx.DrawRectangle(XBrushes.LightGray, rect);
                        var headerLines = columns[i].Item1.Split('\n');
                        double lineHeight = headerRowHeight / headerLines.Length;
                        for (int j = 0; j < headerLines.Length; j++)
                        {
                            var lineRect = new XRect(currentX, currentHeaderY + j * lineHeight, colWidths[i], lineHeight);
                            gfx.DrawString(headerLines[j], smallFont, XBrushes.Black, lineRect, formats[i]);
                        }
                        currentX += colWidths[i];
                    }
                    y += headerRowHeight;
                }

                x = margin;
                
                // Truncate long text with ellipsis
                var menuName = TruncateWithEllipsis(sale.MenuName, 30);
                var itemGroup = TruncateWithEllipsis(sale.ItemGroup, 25);

                var values = new[]
                {
                    sale.InvoiceDate.ToString("MM/dd/yyyy", phCulture),
                    sale.InvoiceNumber.ToString("D12"),
                    menuName,
                    sale.BaseUnit,
                    sale.Quantity.ToString(),
                    sale.Cost.ToString("N2", phCulture),
                    sale.Price.ToString("N2", phCulture),
                    itemGroup,
                    sale.Barcode.ToString(),
                    sale.Status,
                    sale.TotalCost.ToString("N2", phCulture),
                    sale.Revenue.ToString("N2", phCulture),
                    sale.Profit.ToString("N2", phCulture)
                };

                // Verify arrays have the same length
                if (values.Length != columns.Length || values.Length != formats.Length)
                {
                    throw new InvalidOperationException($"Array length mismatch: values={values.Length}, columns={columns.Length}, formats={formats.Length}");
                }

                // Use red color for returned items
                var textBrush = sale.IsReturned ? XBrushes.Red : XBrushes.Black;

                for (int i = 0; i < values.Length; i++)
                {
                    var rect = new XRect(x, y, colWidths[i], rowHeight);
                    gfx.DrawString(values[i], smallFont, textBrush, rect, formats[i]);
                    x += colWidths[i];
                }
                y += rowHeight;
                gfx.DrawLine(XPens.Gray, margin, y, margin + pageWidth, y);
            }

            // Totals row
            // Check if adding the totals row will exceed the page height (with a bottom margin)
            if (y + rowHeight > page.Height - margin)
            {
                 // Add a new page for totals
                page = document.AddPage();
                page.Orientation = PdfSharp.PageOrientation.Landscape;
                page.Width = XUnit.FromInch(13.0);
                page.Height = XUnit.FromInch(8.5);
                gfx = XGraphics.FromPdfPage(page);
                y = margin; // Reset y position for the new page
            }

            x = margin;
            var totals = new[]
            {
                "", // DATE
                "", // INVOICE
                "", // MENU NAME
                "", // UNIT
                "TOTALS:", // QTY
                sales.Sum(s => s.Cost).ToString("N2", phCulture), // COST
                sales.Sum(s => s.Price).ToString("N2", phCulture), // PRICE
                "", // GROUP
                "", // BARCODE
                "", // STATUS
                sales.Sum(s => s.TotalCost).ToString("N2", phCulture), // TOTAL COST
                sales.Sum(s => s.Revenue).ToString("N2", phCulture), // REVENUE
                sales.Sum(s => s.Profit).ToString("N2", phCulture) // PROFIT
            };

            // Verify totals array length
            if (totals.Length != columns.Length)
            {
                throw new InvalidOperationException($"Totals array length mismatch: totals={totals.Length}, columns={columns.Length}");
            }

            for (int i = 0; i < totals.Length; i++)
            {
                var rect = new XRect(x, y, colWidths[i], rowHeight);
                gfx.DrawRectangle(XBrushes.White, rect);
                gfx.DrawString(totals[i], totals[i] == "TOTALS:" ? headerFont : smallFont, XBrushes.Black, rect, formats[i]);
                x += colWidths[i];
            }
            y += rowHeight;

            // Save to memory stream
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }

        private string TruncateWithEllipsis(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }
    }
} 