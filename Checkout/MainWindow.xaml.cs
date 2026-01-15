using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using static Checkout.Product;
using static Checkout.Receipt;

namespace Checkout
{

    public partial class MainWindow : Window
    {
        private decimal totalSum = 0;

        private List<Product> allProducts;

        public MainWindow()
        {
            InitializeComponent();

            Database.EnsureCreated();

            allProducts = new List<Product>();
            foreach (var cat in ProductRepository.GetAllCategories())
            {
                allProducts.AddRange(ProductRepository.GetProductsByCategory(cat));
            }

            LoadInventory();
        }

        private void ToggleCategoryPanel(string category, StackPanel targetPanel, string colorHex)
        {
            bool isCurrentlyVisible = targetPanel.Visibility == Visibility.Visible;

            // Rensa och dölj alla paneler
            panelFood.Children.Clear();
            panelCandy.Children.Clear();
            panelTobacco.Children.Clear();
            panelPaper.Children.Clear();

            panelFood.Visibility = Visibility.Collapsed;
            panelCandy.Visibility = Visibility.Collapsed;
            panelTobacco.Visibility = Visibility.Collapsed;
            panelPaper.Visibility = Visibility.Collapsed;

            if (!isCurrentlyVisible)
            {
                // Hämta produkter
                var items = allProducts.Where(p => p.Category == category);

                foreach (var product in items)
                {
                    Button b = new Button()
                    {
                        Content = product.Name,
                        Tag = product,
                        Margin = new Thickness(0, 5, 0, 5),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Width = 170,
                        Height = 60,
                        Background = (Brush)new BrushConverter().ConvertFromString(colorHex),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };
                    b.Click += ProductSelected;
                    targetPanel.Children.Add(b);
                }

                // Visa panelen
                targetPanel.Visibility = Visibility.Visible;
            }
        }


        private void ButtonFood(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Enkel mat", panelFood, "#FF6495ED"); // CornflowerBlue
        }

        private void ButtonCandy(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Godis", panelCandy, "#FF87CEEB"); // SkyBlue
        }

        private void ButtonTobacco(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Tobak", panelTobacco, "#FFADD8E6"); // LightBlue
        }

        private void ButtonPaper(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Tidningar", panelPaper, "#FFB0C4DE"); // LightSteelBlue
        }

        private List<CartItem> cart = new List<CartItem>();

        private void ProductSelected(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Product product = button.Tag as Product;

            // Hämta aktuellt lager
            var supply = ProductRepository.GetInventory()
                .FirstOrDefault(p => p.Id == product.Id);

            if (supply == null || supply.Inventory <= 0)
            {
                MessageBox.Show("Produkten är slut i lager!", "Slut i lager",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingItem = cart.FirstOrDefault(c => c.Product.Id == product.Id);

            if (existingItem != null)
            {
                if (existingItem.Quantity >= supply.Inventory)
                {
                    MessageBox.Show("Det finns inte fler i lager.", "Lagerbegränsning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem { Product = product, Quantity = 1 });
            }

            UpdateCartDisplay();
        }

        private void UpdateCartDisplay()
        {
            lstProducts.Items.Clear();
            totalSum = 0;

            foreach (var item in cart)
            {
                lstProducts.Items.Add($"{item.Product.Name} x{item.Quantity} - {item.Product.Price * item.Quantity} kr");
                totalSum += item.Product.Price * item.Quantity;
            }

            UpdateTotal();
        }

        private void ButtonClearCart(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Kundvagnen är redan tom.", "Rensa kundvagn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            cart.Clear();
            lstProducts.Items.Clear();
            totalSum = 0;
            UpdateTotal();
        }

        private void ButtonPay(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Kundvagnen är tom!");
                return;
            }

            // Försök sälja alla produkter
            foreach (var item in cart)
            {
                bool ok = ProductRepository.TrySellProduct(
                    item.Product.Id,
                    item.Quantity);

                if (!ok)
                {
                    MessageBox.Show(
                        $"Inte tillräckligt lager för {item.Product.Name}",
                        "Lagerfel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }
            }

            // Skapa kvitto
            var receipt = ReceiptManager.CreateReceipt(cart.ToList());

            lstReceipts.Items.Add(
                $"Kvittosnr: {receipt.ReceiptNumber}, Total: {receipt.TotalPrice} kr");

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(folder, $"Receipt_{receipt.ReceiptNumber}.pdf");
            receipt.SaveAsPdf(filePath);

            cart.Clear();
            lstProducts.Items.Clear();
            totalSum = 0;
            UpdateTotal();

            // Uppdatera lager-tabben
            LoadInventory();
        }

        private void UpdateTotal()
        {
            txtTotal.Text = $"{totalSum} kr";
        }

        private void ReceiptSelected(object sender, SelectionChangedEventArgs e)
        {
            if (lstReceipts.SelectedIndex < 0) return;

            var receipt = Receipt.ReceiptManager.AllReceipts[lstReceipts.SelectedIndex];

            decimal taxRate = 0.25m;
            decimal exklMoms = Math.Round(receipt.TotalPrice / (1 + taxRate), 2);
            decimal moms = Math.Round(receipt.TotalPrice - exklMoms, 2);

            txtReceiptNumber.Text = $"Kvittonr: {receipt.ReceiptNumber}";
            txtReceiptTime.Text = $"Tid: {receipt.Timestamp}";

            txtReceiptTotal.Text =
                $"Exkl moms: {exklMoms} kr\n" +
                $"Moms (25%): {moms} kr\n" +
                $"Total: {receipt.TotalPrice} kr";

            itemsReceiptPanel.ItemsSource = receipt.Items;
        }
        private void LoadInventory()
        {
            dgInventory.ItemsSource = ProductRepository.GetInventory();
        }
    }
}