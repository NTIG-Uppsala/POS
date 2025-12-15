using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace CheckoutTests
{
    [TestClass]
    public class CheckoutUITests
    {
        private static readonly string AppPath =
            System.IO.Path.Combine(
                System.IO.Path.GetFullPath("../../../../Checkout/bin/Debug/net9.0-windows/"),
                "Checkout.exe"
            );

        private static readonly string TestDbPath =
    Path.Combine(
        System.IO.Path.GetFullPath("../../../../Checkout/bin/Debug/net9.0-windows/"),
        "TestDatabase.db"
    );

        [TestInitialize]
        public void Setup()
        {
            // Ta bort testdatabasen om den finns
            if (File.Exists(TestDbPath))
                File.Delete(TestDbPath);

            // Kopiera originaldatabasen
            string originalDb = Path.Combine(
                Path.GetFullPath("../../../../Checkout/bin/Debug/net9.0-windows/"),
                "Database.db"
            );
            File.Copy(originalDb, TestDbPath);

            // Låt ProductRepository använda testdatabasen
            Checkout.ProductRepository.SetDatabasePath(TestDbPath);
        }

        [TestMethod]
        public void Test_PayButton_EmptiesCart()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            Assert.IsNotNull(window, "Main window was not found.");

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            var payButton = window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton();
            var listBox = window.FindFirstDescendant(cf.ByAutomationId("lstProducts")).AsListBox();
            var totalText = window.FindFirstDescendant(cf.ByAutomationId("txtTotal")).AsLabel();

            // Öppna mat-kategorin
            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();

            // --- Viktigt: här använder vi Retry.WhileNull utan timeout ---
            string productName = "Korv med bröd";  // ändra till korrekt namn

            var result = Retry.WhileNull(
                () => window.FindFirstDescendant(cf.ByName(productName))
            );

            Assert.IsNotNull(result.Result, $"Could not find product button '{productName}'");

            var productButton = result.Result.AsButton();
            productButton.Click();

            // Tryck på Betala
            payButton.Click();

            Assert.AreEqual(0, listBox.Items.Length, "Cart should be empty after payment.");
            Assert.AreEqual("0 kr", totalText.Text, "Total should be reset after payment.");
        }

        [TestMethod]
        public void Test_ClearButton_EmptiesCart()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            Assert.IsNotNull(window);

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            var clearButton = window.FindFirstDescendant(cf.ByAutomationId("btnClear")).AsButton();
            var listBox = window.FindFirstDescendant(cf.ByAutomationId("lstProducts")).AsListBox();
            var totalText = window.FindFirstDescendant(cf.ByAutomationId("txtTotal")).AsLabel();

            // Klicka på kategori för att visa produkter
            var candyButton = window.FindFirstDescendant(cf.ByAutomationId("btnCandy")).AsButton();
            candyButton.Click();

            // Klicka på en produkt direkt via namn
            var productButton = window.FindFirstDescendant(cf.ByName("Marabou Mjölkchoklad 100 g")).AsButton();
            productButton.Click();

            // Kontrollera att varukorgen har 1 objekt
            Trace.Assert(listBox.Items.Length == 1, "Cart should contain 1 item before clear.");

            // Klicka på rensa
            clearButton.Click();

            Trace.Assert(listBox.Items.Length == 0, "Cart should be empty after clear.");
            Trace.Assert(totalText.Text == "0 kr", "Total should be reset after clear.");
        }

        [TestMethod]
        public void Test_AllDropDownsOpenAndClose()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            Assert.IsNotNull(window, "Main window was not found.");

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Kategorier och exempelprodukt (vi tar första produkten från din allProducts-lista)
            var categories = new (string btnId, string exampleProduct)[]
            {
        ("btnFood","Korv med bröd"),
        ("btnCandy","Marabou Mjölkchoklad 100 g"),
        ("btnTobacco","Marlboro Red (20-pack)"),
        ("btnPaper","Aftonbladet (dagens)")
            };

            foreach (var (btnId, exampleProduct) in categories)
            {
                var categoryButton = window.FindFirstDescendant(cf.ByAutomationId(btnId)).AsButton();

                // Klicka första gången → produkter ska synas
                categoryButton.Click();
                var productButton = window.FindFirstDescendant(cf.ByName(exampleProduct))?.AsButton();
                Trace.Assert(productButton != null, $"Product '{exampleProduct}' should be visible after clicking {btnId}.");

                // Klicka igen → produkter ska gömmas
                categoryButton.Click();
                productButton = window.FindFirstDescendant(cf.ByName(exampleProduct))?.AsButton();
                Trace.Assert(productButton == null, $"Product '{exampleProduct}' should be hidden after clicking {btnId} again.");
            }
        }

        [TestMethod]
        public void Test_ProductButtons_AddToCart()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            Assert.IsNotNull(window);

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());
            var listBox = window.FindFirstDescendant(cf.ByAutomationId("lstProducts")).AsListBox();

            var categories = new (string btnId, string productName)[]
            {
        ("btnFood","Korv med bröd"),
        ("btnCandy","Marabou Mjölkchoklad 100 g"),
        ("btnTobacco","Marlboro Red (20-pack)"),
        ("btnPaper","Aftonbladet (dagens)")
            };

            foreach (var (btnId, productName) in categories)
            {
                var catButton = window.FindFirstDescendant(cf.ByAutomationId(btnId)).AsButton();
                Assert.IsNotNull(catButton, $"Could not find category button {btnId}");
                catButton.Click();

                // Hämta produktknappen direkt via namn
                var productButton = Retry.WhileNull(
                    () => window.FindFirstDescendant(cf.ByName(productName)),
                    timeout: TimeSpan.FromSeconds(2)
                ).Result?.AsButton();

                Assert.IsNotNull(productButton, $"Product button '{productName}' not found in category {btnId}");

                int before = listBox.Items.Length;
                productButton.Click();
                int after = listBox.Items.Length;

                Assert.IsTrue(after == before + 1,
                    $"Cart did not increase after clicking '{productName}' in {btnId}. Before={before}, After={after}");

                catButton.Click(); // stäng dropdown
            }
        }


        [TestMethod]
        public void Test_ProductQuantity_IncrementsCorrectly()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            Assert.IsNotNull(window);

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());
            var listBox = window.FindFirstDescendant(cf.ByAutomationId("lstProducts")).AsListBox();

            // Öppna mat-kategorin
            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();

            // Ange produktnamn
            string productName = "Korv med bröd";

            // Hitta produktknappen direkt via namn
            var result = Retry.WhileNull(
                () => window.FindFirstDescendant(cf.ByName(productName)),
                TimeSpan.FromSeconds(2)
            );

            Assert.IsNotNull(result.Result, $"Could not find product button '{productName}'");
            var productButton = result.Result.AsButton();

            // Klicka produkten flera gånger
            for (int i = 1; i <= 3; i++)
            {
                productButton.Click();
                // Kontrollera att varukorgen innehåller exakt i antal objekt (vi kollar längden)
                Trace.Assert(listBox.Items.Length >= 1, "Cart should contain at least 1 item.");
                // Alternativt: kolla att texten innehåller korrekt kvantitet
                var cartText = listBox.Items[0].Text;
                Trace.Assert(cartText.Contains($"x{i}"), $"Product quantity should be x{i} in cart, got '{cartText}'");
            }
        }

        [TestMethod]
        public void Test_ReceiptAdded_AfterPayment()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Öppna mat
            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();

            // Klicka produkt
            var productButton = window.FindFirstDescendant(cf.ByName("Korv med bröd")).AsButton();
            productButton.Click();

            // Betala
            var payButton = window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton();
            payButton.Click();

            // Gå till kvittotabben
            var tab = window.FindFirstDescendant(cf.ByName("Kvitton")).AsTabItem();
            tab.Select();

            // Hitta kvittolista
            var receiptList = window.FindFirstDescendant(cf.ByAutomationId("lstReceipts")).AsListBox();

            Assert.AreEqual(1, receiptList.Items.Length, "There should be exactly 1 receipt added.");
        }

        [TestMethod]
        public void Test_ReceiptDetails_ShownOnSelect()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Lägg till produkt
            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();
            var productButton = window.FindFirstDescendant(cf.ByName("Korv med bröd")).AsButton();
            productButton.Click();

            // Betala
            var payButton = window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton();
            payButton.Click();

            // Gå till kvittotabben
            var tab = window.FindFirstDescendant(cf.ByName("Kvitton")).AsTabItem();
            tab.Select();

            var receiptList = window.FindFirstDescendant(cf.ByAutomationId("lstReceipts")).AsListBox();
            receiptList.Select(0);

            var receiptNumber = window.FindFirstDescendant(cf.ByAutomationId("txtReceiptNumber")).AsLabel();
            var total = window.FindFirstDescendant(cf.ByAutomationId("txtReceiptTotal")).AsLabel();

            Assert.IsTrue(receiptNumber.Text.Contains("Kvittonr:"), "Receipt number should be displayed.");

            // Extrahera raden som börjar med "Total:" och jämför
            var totalLine = total.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("Total:"));

            Assert.IsNotNull(totalLine, "Total line not found in receipt.");
            Assert.AreEqual("Total: 25 kr", totalLine, "Total price should match purchased product.");
        }


        [TestMethod]
        public void Test_ReceiptItems_ListedCorrectly()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Öppna och köp två produkter
            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();
            window.FindFirstDescendant(cf.ByName("Korv med bröd")).AsButton().Click();
            window.FindFirstDescendant(cf.ByName("Varm toast (ost & skinka)")).AsButton().Click();

            // Betala
            window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton().Click();

            // Gå till kvitton
            window.FindFirstDescendant(cf.ByName("Kvitton")).AsTabItem().Select();

            var receiptList = window.FindFirstDescendant(cf.ByAutomationId("lstReceipts")).AsListBox();
            receiptList.Select(0);

            var itemsPanel = window.FindFirstDescendant(cf.ByAutomationId("itemsReceiptPanel")).AsListBox();

            Assert.AreEqual(2, itemsPanel.Items.Length, "Receipt should list 2 purchased items.");
        }

        [TestMethod]
        public void Test_ReceiptNumber_Increments()
        {
            var app = FlaUI.Core.Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);
            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            var foodButton = window.FindFirstDescendant(cf.ByAutomationId("btnFood")).AsButton();
            foodButton.Click();

            // Gör två köp
            for (int i = 0; i < 2; i++)
            {
                window.FindFirstDescendant(cf.ByName("Korv med bröd")).AsButton().Click();
                window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton().Click();
            }

            // Gå till kvitton
            window.FindFirstDescendant(cf.ByName("Kvitton")).AsTabItem().Select();

            var receiptList = window.FindFirstDescendant(cf.ByAutomationId("lstReceipts")).AsListBox();

            Assert.IsTrue(receiptList.Items[0].Text.Contains("1"));
            Assert.IsTrue(receiptList.Items[1].Text.Contains("2"));
        }

        
    }
}
