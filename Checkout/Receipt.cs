using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using System.IO;
using static Checkout.Product;

namespace Checkout
{
    public class Receipt
    {
        public int ReceiptNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Product.CartItem> Items { get; set; }
        public decimal TotalPrice => Items.Sum(i => i.Product.Price * i.Quantity);

        public Receipt(int receiptNumber, List<Product.CartItem> items)
        {
            ReceiptNumber = receiptNumber;
            Timestamp = DateTime.Now;
            Items = items.Select(i => new Product.CartItem
            {
                Product = i.Product,
                Quantity = i.Quantity
            }).ToList();
        }

        public string GetReceiptText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--------------------------------");
            sb.AppendLine(StoreInfo.StoreName);
            sb.AppendLine(StoreInfo.Address);
            sb.AppendLine(StoreInfo.OrgNumber);
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Kvitto nr: {ReceiptNumber}");
            sb.AppendLine($"Tid: {Timestamp}");
            sb.AppendLine("--------------------------------");

            foreach (var item in Items)
            {
                sb.AppendLine($"{item.Product.Name} x{item.Quantity}  {item.Product.Price * item.Quantity} kr");
            }

            sb.AppendLine("--------------------------------");
            sb.AppendLine($"TOTALT: {TotalPrice} kr");
            sb.AppendLine("Tack för ditt köp!");
            sb.AppendLine("--------------------------------");

            return sb.ToString();
        }

        public void SaveAsPdf(string filePath)
        {
            PdfDocument document = new PdfDocument();
            document.Info.Title = $"Kvittonr: {ReceiptNumber}";
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont headerFont = new XFont("Arial", 14, XFontStyle.Bold);
            XFont regularFont = new XFont("Arial", 12, XFontStyle.Regular);

            double yPoint = 20;
            double regularLineHeight = 25;
            double headerLineHeight = 22;
            double lineGap = 30;

            decimal taxRate = 0.25m;
            decimal exklMoms = Math.Round(TotalPrice / (1 + taxRate), 2);
            decimal taxAmount = Math.Round(TotalPrice - exklMoms, 2);

            // --- Affärsinformation ---
            gfx.DrawString(StoreInfo.StoreName, headerFont, XBrushes.Black,
                new XRect(0, yPoint, page.Width, headerLineHeight), XStringFormats.TopCenter);
            yPoint += headerLineHeight;

            gfx.DrawString(StoreInfo.Address, regularFont, XBrushes.Black,
                new XRect(0, yPoint, page.Width, regularLineHeight), XStringFormats.TopCenter);
            yPoint += regularLineHeight;

            gfx.DrawString(StoreInfo.OrgNumber, regularFont, XBrushes.Black,
                new XRect(0, yPoint, page.Width, regularLineHeight), XStringFormats.TopCenter);
            yPoint += regularLineHeight + 10;

            // --- Kvitto info ---
            gfx.DrawString($"Kvitto nr: {ReceiptNumber}", regularFont, XBrushes.Black, 20, yPoint);
            gfx.DrawString($"Tid: {Timestamp}", regularFont, XBrushes.Black, page.Width - 200, yPoint);
            yPoint += regularLineHeight;

            gfx.DrawLine(XPens.Black, 20, yPoint, page.Width - 20, yPoint);
            yPoint += lineGap;

            // --- Rubriker ---
            gfx.DrawString("Produkt", regularFont, XBrushes.Black, 20, yPoint);
            gfx.DrawString("Antal", regularFont, XBrushes.Black, 290, yPoint);
            gfx.DrawString("Pris", regularFont, XBrushes.Black, 400, yPoint);
            yPoint += regularLineHeight;

            gfx.DrawLine(XPens.Black, 20, yPoint, page.Width - 20, yPoint);
            yPoint += lineGap;

            // --- Produkter ---
            foreach (var item in Items)
            {
                gfx.DrawString(item.Product.Name, regularFont, XBrushes.Black, 20, yPoint);
                gfx.DrawString(item.Quantity.ToString(), regularFont, XBrushes.Black, 300, yPoint);
                gfx.DrawString($"{item.Product.Price * item.Quantity} kr", regularFont, XBrushes.Black, 400, yPoint);
                yPoint += regularLineHeight;
            }

            yPoint += 5;
            gfx.DrawLine(XPens.Black, 20, yPoint, page.Width - 20, yPoint);
            yPoint += regularLineHeight;

            // --- TOTAL & Moms ---
            gfx.DrawString($"TOTALT exkl. moms: {exklMoms:F2} kr", headerFont, XBrushes.Black, 20, yPoint);
            yPoint += headerLineHeight;

            gfx.DrawString($"MOMS {taxRate * 100}%: {taxAmount:F2} kr", headerFont, XBrushes.Black, 20, yPoint);
            yPoint += headerLineHeight;

            gfx.DrawString($"TOTALT inkl. moms: {TotalPrice:F2} kr", headerFont, XBrushes.Black, 20, yPoint);
            yPoint += headerLineHeight;

            gfx.DrawString("Tack för ditt köp!", regularFont, XBrushes.Black, 20, yPoint);

            document.Save(filePath);
        }


        public static class StoreInfo
        {
            public static string StoreName { get; set; } = "Connys Gottebod";
            public static string Address { get; set; } = "Kioskvägen 2, 753 28 Uppsala";
            public static string OrgNumber { get; set; } = "Org.nr: 654321-0987";
        }

        public static class ReceiptManager
        {
            private static int _nextReceiptNumber = 1;
            public static List<Receipt> AllReceipts { get; private set; } = new List<Receipt>();

            public static Receipt CreateReceipt(List<Product.CartItem> cartItems)
            {
                var receipt = new Receipt(_nextReceiptNumber++, cartItems);
                AllReceipts.Add(receipt);
                return receipt;
            }
        }
    }
}