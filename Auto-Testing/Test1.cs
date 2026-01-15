using Checkout;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FlaUI.Core;
using Microsoft.Data.Sqlite;
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

        private string TestDbPath = null!;

        private FlaUI.Core.Application? _app;

        [TestInitialize]
        public void Setup()
        {
            TestDbPath = Path.Combine(
                Path.GetDirectoryName(AppPath)!,
                $"TestDatabase_{Guid.NewGuid()}.db"
            );

            ProductRepository.SetDatabasePath(TestDbPath);
            Database.UseDatabase(TestDbPath);
            Database.EnsureCreated();

            StringAssert.Contains(
                ProductRepository.CurrentDatabasePath,
                "TestDatabase_",
                "TESTS ARE RUNNING AGAINST PRODUCTION DATABASE!"
            );
        }


        [TestMethod]
        public void Test_PayButton_EmptiesCart()
        {
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);

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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);
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

        [TestMethod]
        public void Test_Inventory_Decreases()
        {
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);

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
            var tab = window.FindFirstDescendant(cf.ByName("Lager")).AsTabItem();
            tab.Select();

            // Hitta kvittolista
            // Hitta lagerrutnätet
            var dgInventory = window.FindFirstDescendant(cf.ByAutomationId("dgInventory")).AsDataGridView();

            // Hitta raden för produkten
            var row = dgInventory.Rows
                .FirstOrDefault(r => r.Cells[2].Value?.ToString() == "Korv med bröd");

            Assert.IsNotNull(row, "Product not found in inventory grid.");

            // Lager ska ha minskat från 100 → 99
            int inventory = int.Parse(row.Cells[3].Value.ToString());
            Assert.AreEqual(99, inventory, "Stock should decrease by 1 after purchase.");

        }

        [TestMethod]
        public void Test_Sold_Increase()
        {
            _app = Application.Launch(AppPath, TestDbPath);
            using var automation = new UIA3Automation();
            var window = _app.GetMainWindow(automation);

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Öppna Godis
            var candyButton = window.FindFirstDescendant(cf.ByAutomationId("btnCandy")).AsButton();
            candyButton.Click();

            // Vänta tills knappen syns
            var productButton = Retry.WhileNull(
                () => window.FindFirstDescendant(cf.ByName("Kexchoklad")),
                timeout: TimeSpan.FromSeconds(2)
            ).Result?.AsButton();

            Assert.IsNotNull(productButton, "Kexchoklad button not found");
            productButton.Click();

            // Betala
            var payButton = window.FindFirstDescendant(cf.ByAutomationId("btnPay")).AsButton();
            payButton.Click();

            // Gå till Lager
            var tab = window.FindFirstDescendant(cf.ByName("Lager")).AsTabItem();
            tab.Select();

            // Hitta lagerrutnätet
            var dgInventory = window.FindFirstDescendant(cf.ByAutomationId("dgSold")).AsDataGridView();

            // Hitta raden för Kexchoklad
            var row = dgInventory.Rows.FirstOrDefault(r => r.Cells[2].Value?.ToString() == "Kexchoklad");
            Assert.IsNotNull(row, "Kexchoklad not found in inventory grid.");

            // Kontrollera sold
            int sold = int.Parse(row.Cells[4].Value.ToString());
            Assert.AreEqual(1, sold, "Sold should increase by 1 after purchase.");
        }


        [TestCleanup]
        public void Cleanup()
        {
            if (_app != null && !_app.HasExited)
            {
                _app.Close();
                _app.Dispose();
                _app = null;
            }

            foreach (var p in Process.GetProcessesByName("Checkout"))
            {
                p.Kill();
            }

            if (File.Exists(TestDbPath))
            {
                try { File.Delete(TestDbPath); }
                catch { /* ignorera – Windows kan vara seg */ }
            }
        }

    }
}
