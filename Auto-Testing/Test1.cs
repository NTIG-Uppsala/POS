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
        [TestMethod]
        public void AddCoffeeAndClearCartTest()
        {
            var app = FlaUI.Core.Application.Launch("C:\\Users\\pontus.noaksson\\source\\repos\\NTIG-Uppsala\\Checkout\\bin\\Release\\net9.0-windows\\Checkout.exe");
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                var cf = new ConditionFactory(new UIA3PropertyLibrary());

                // Hitta kontroller
                var lb = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"))?.AsListBox();
                var txtTotal = window.FindFirstDescendant(cf.ByAutomationId("txtTotal"))?.AsLabel();
                var btnAddCoffee = window.FindFirstDescendant(cf.ByAutomationId("btnAddCoffee"))?.AsButton();
                var btnClear = window.FindFirstDescendant(cf.ByAutomationId("btnClear"))?.AsButton();

                Assert.IsNotNull(lb, "Hittade inte ListBox");
                Assert.IsNotNull(txtTotal, "Hittade inte total-TextBlock");
                Assert.IsNotNull(btnAddCoffee, "Hittade inte AddCoffee-knappen");
                Assert.IsNotNull(btnClear, "Hittade inte Clear-knappen");

                Debug.Print(lb.FindAllChildren().Length.ToString());
                Debug.Print(lb.Items.Count().ToString());

                // ---------- Test 1: Lägg till kaffe ----------
                btnAddCoffee.Click();
                btnAddCoffee.Click();
                btnAddCoffee.Click();
                btnAddCoffee.Click();
                btnAddCoffee.Click();

                Debug.Print(lb.FindAllChildren().Length.ToString());
                Debug.Print(lb.Properties.SizeOfSet.ToString());
                Debug.Print(lb.FindAllChildren().Length.ToString());
                Debug.Print("yo");

                var added1 = Retry.WhileFalse(() =>
                {
                    lb = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"))?.AsListBox();
                    return lb != null && lb.FindAllChildren().Length == 1;
                }, TimeSpan.FromSeconds(5)).Success;

                Assert.IsTrue(added1, "ListBox should have 1 item after adding coffee");
                var items1 = lb.FindAllChildren();
                Assert.AreEqual("Kaffe - 49 kr", items1[0].Name, "Fel text på första item");
                Assert.AreEqual("49 kr", txtTotal.Text, "Fel total efter första kaffe");

                // ---------- Test 2: Lägg till kaffe igen ----------
                Debug.Print(items1.Length.ToString());
                btnAddCoffee.Click();
                Debug.Print(items1.Length.ToString());

                //var added2 = Retry.WhileFalse(() =>
                //{
                //    lb = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"))?.AsListBox();
                //    return lb != null && lb.FindAllChildren().Length == 2;
                //}, TimeSpan.FromSeconds(5)).Success;
                //Assert.AreEqual(lb.Items[0].Name, "Kaffe - 49 kr");
                //Assert.AreEqual(lb.Items[1].Text, "Kaffe - 49 kr");
                //Assert.IsTrue(added2, "ListBox should have 2 items after adding coffee again");
                var items2 = lb.FindAllChildren();
                Debug.Print(items2.Length.ToString());
                Debug.Print(items1.Length.ToString());
                //Assert.AreEqual("Kaffe - 49 kr", items2[1].Name, "Fel text på andra item");
                //Assert.AreEqual("98 kr", txtTotal.Text, "Fel total efter två kaffe");

                // ---------- Test 3: Rensa kundvagn ----------
                btnClear.Click();

                var cleared = Retry.WhileFalse(() =>
                {
                    lb = window.FindFirstDescendant(cf.ByAutomationId("lstProducts"))?.AsListBox();
                    txtTotal = window.FindFirstDescendant(cf.ByAutomationId("txtTotal"))?.AsLabel();
                    return lb != null && lb.FindAllChildren().Length == 0 &&
                           txtTotal != null && txtTotal.Text == "0 kr";
                }, TimeSpan.FromSeconds(5)).Success;

                Assert.IsTrue(cleared, "Cart should be cleared and total reset to 0 kr");
            }
        }
    }
}