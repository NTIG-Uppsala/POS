using System;
using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CheckoutTests
{
    [TestClass]
    public class CheckoutUITests
    {
        private const string AppPath =
            "C:\\Users\\pontus.noaksson\\source\\repos\\POS\\Checkout\\bin\\Debug\\net9.0-windows\\Checkout.exe";

        [TestMethod]
        public void Test_AddCoffeeButton_AddsProductAndUpdatesTotal()
        {
            var app = FlaUI.Core.Application.Launch(AppPath);
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                Assert.IsNotNull(window, "Main window was not found.");

                ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

                var listElement = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"));
                var addButton = window.FindFirstDescendant(cf.ByAutomationId("btnAddCoffee"));
                var totalText = window.FindFirstDescendant(cf.ByAutomationId("txtTotal"));

                Assert.IsNotNull(listElement, "ListBox not found.");
                Assert.IsNotNull(addButton, "Add Coffee button not found.");
                Assert.IsNotNull(totalText, "Total textblock not found.");

                var listBox = listElement.AsListBox();
                var button = addButton.AsButton();
                var total = totalText.AsLabel();

                // Klicka för att lägga till kaffe
                button.Click();

                // Kontrollera att produkten finns i listan
                var addedItem = listBox.FindFirstChild(cf.ByName("Kaffe - 49 kr"));
                Trace.Assert(addedItem != null, "Expected product was not found in the list.");

                // Kontrollera att totalsumman uppdaterats
                Trace.Assert(total.Text == "49 kr", "Total price was not updated correctly.");
            }
        }


        [TestMethod]
        public void Test_ClearButton_EmptiesListAndResetsTotal()
        {
            var app = FlaUI.Core.Application.Launch(AppPath);
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                Assert.IsNotNull(window, "Main window was not found.");

                ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

                var listElement = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"));
                var addButton = window.FindFirstDescendant(cf.ByAutomationId("btnAddCoffee"));
                var clearButton = window.FindFirstDescendant(cf.ByAutomationId("btnClear"));
                var totalText = window.FindFirstDescendant(cf.ByAutomationId("txtTotal"));

                var listBox = listElement.AsListBox();
                var add = addButton.AsButton();
                var clear = clearButton.AsButton();
                var total = totalText.AsLabel();

                // Lägg till produkter
                add.Click();
                add.Click();

                // Kontrollera att minst en produkt finns innan vi rensar
                var addedItem = listBox.FindFirstChild(cf.ByName("Kaffe - 49 kr"));
                Trace.Assert(addedItem != null, "Expected product was not found in the list before clearing.");

                // Kontrollera totalsumman innan rensning
                Trace.Assert(total.Text == "98 kr", "Total before clearing is not correct.");

                // Tryck på rensa kundvagn
                clear.Click();

                // Kontrollera att totalsumman nollställts
                Trace.Assert(total.Text == "0 kr", "Total was not reset.");
            }
        }

        [TestMethod]
        public void Test_AddingMultipleProducts_UpdatesTotalIncrementally()
        {
            var app = FlaUI.Core.Application.Launch(AppPath);
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                Assert.IsNotNull(window, "Main window was not found.");

                ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

                var listElement = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"));
                var addButton = window.FindFirstDescendant(cf.ByAutomationId("btnAddCoffee"));
                var totalText = window.FindFirstDescendant(cf.ByAutomationId("txtTotal"));

                var listBox = listElement.AsListBox();
                var button = addButton.AsButton();
                var total = totalText.AsLabel();

                // Lägg till 3 produkter
                button.Click();
                button.Click();
                button.Click();

                // Kontrollera att minst ett objekt finns
                var addedItem = listBox.FindFirstChild(cf.ByName("Kaffe - 49 kr"));
                Trace.Assert(addedItem != null, "Expected product was not found in the list.");

                // Kontrollera att totalsumman stämmer
                Trace.Assert(total.Text == "147 kr", "Total was not correctly calculated after multiple additions.");
            }
        }
    }
}
