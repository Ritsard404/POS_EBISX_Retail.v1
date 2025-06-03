using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EBISX_POS.Helper
{
    public static class RawPrinterHelper
    {
        static RawPrinterHelper()
        {
            // Register the code page encoding provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private class CustomEncoderFallback : EncoderFallback
        {
            public override int MaxCharCount => 1;

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new CustomEncoderFallbackBuffer();
            }
        }

        private class CustomEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            private char _fallbackChar = 'P'; // Replace ₱ with 'P'
            private bool _hasFallback;

            public override int Remaining => _hasFallback ? 1 : 0;

            public override bool Fallback(char charUnknown, int index)
            {
                _hasFallback = true;
                return true;
            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                _hasFallback = true;
                return true;
            }

            public override char GetNextChar()
            {
                if (!_hasFallback)
                    return '\0';

                _hasFallback = false;
                return _fallbackChar;
            }

            public override bool MovePrevious()
            {
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        // Import necessary Win32 functions from winspool.drv
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefaults);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Send raw text to a Windows‐installed printer.
        /// </summary>
        public static bool PrintText(string printerName, string text)
        {
            try
            {
                // Create encoding with custom fallback
                var encoding = Encoding.GetEncoding(437, 
                    new CustomEncoderFallback(),
                    DecoderFallback.ExceptionFallback);

                byte[] bytes = encoding.GetBytes(text);
                return PrintRawBytes(printerName, bytes);
            }
            catch (Exception)
            {
                // If all else fails, try ASCII
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                return PrintRawBytes(printerName, bytes);
            }
        }
        public static bool PrintRawBytes(string printerName, byte[] bytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            var docInfo = new DOCINFO()
            {
                pDocName = "Raw Thermal Job",
                pDataType = "RAW",
                pOutputFile = null
            };

            try
            {
                // 1) Open the printer by name
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                    return false;

                // 2) Start a new document
                if (!StartDocPrinter(hPrinter, 1, docInfo))
                {
                    ClosePrinter(hPrinter);
                    return false;
                }

                // 3) Start a new page
                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                // 4) Allocate unmanaged memory, copy bytes, and write
                IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                bool success = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int written);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);

                // 5) End page, end doc, close printer
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);

                return success && (written == bytes.Length);
            }
            catch
            {
                if (hPrinter != IntPtr.Zero)
                {
                    EndPagePrinter(hPrinter);
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                }
                return false;
            }
        }
    }

    public static class CashTrackPrinter
    {
        /// <summary>
        /// Builds and sends a "Cash Track Report" directly to the specified 58 mm thermal printer.
        /// </summary>
        /// <param name="cashierEmail">Email of the cashier (used in filename if you want to archive).</param>
        /// <param name="cashInDrawer">Current "Cash In Drawer" amount (decimal).</param>
        /// <param name="totalCashDrawer">Current "Total Cash Drawer" amount (decimal).</param>
        /// <param name="printerName">
        ///   Exact Windows printer name (e.g. "POS-58 Thermal").  
        ///   You can find it under Windows Settings → Devices → Printers & scanners.
        /// </param>
        /// <param name="archiveFolderPath">
        ///   (Optional) If non‐empty, saves a .txt snapshot of the report here.
        ///   If you do not want to archive, pass null or empty string.
        /// </param>
        public static void PrintCashTrackReport(
            string cashierEmail,
            string cashInDrawer,
            string totalCashDrawer,
            string printerName,
            string archiveFolderPath = null)
        {
            // 1) Build the report text (trim each line)
            string rawReport = $@"
                ================================
                        Cash Track Report
                ================================
                Cash In Drawer: {cashInDrawer:C}
                Total Cash Drawer: {totalCashDrawer:C}
            ";
            string reportContent = string.Join("\n",
                rawReport
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
            );

            // 2) Optionally archive as a .txt file
            if (!string.IsNullOrWhiteSpace(archiveFolderPath))
            {
                if (!Directory.Exists(archiveFolderPath))
                    Directory.CreateDirectory(archiveFolderPath);

                string fileName = $"Cash-Track-{cashierEmail}-{DateTimeOffset.UtcNow:MMMM-dd-yyyy-HH-mm-ss}.txt";
                string filePath = Path.Combine(archiveFolderPath, fileName);
                File.WriteAllText(filePath, reportContent);
                // (You can choose to expose this path or ignore it.)
            }

            // 3) Append a few line‐feeds so the printer has "cut room"
            reportContent += "\n\n\n";

            // 4) Send to thermal printer via RawPrinterHelper
            bool printed = RawPrinterHelper.PrintText(printerName, reportContent);
            if (!printed)
            {
                // Handle the error however you like:
                // e.g. throw, log, or show a message on your UI.
                throw new Exception($"Failed to print Cash Track Report to printer \"{printerName}\".");
            }
        }

    }
}
