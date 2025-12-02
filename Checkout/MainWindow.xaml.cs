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
using static Checkout.Product;
using static Checkout.Receipt; 


namespace Checkout
{

    public partial class MainWindow : Window
    {
        private decimal totalSum = 0;

        private List<Product> allProducts = new List<Product>()
        {
            new Product("Korv med bröd", 25, "Food"),
            new Product("Varm toast (ost & skinka)", 30, "Food"),
            new Product("Pirog (köttfärs)", 22, "Food"),
            new Product("Färdig sallad (kyckling)", 49, "Food"),
            new Product("Panini (mozzarella & pesto)", 45, "Food"),

            new Product("Marabou Mjölkchoklad 100 g", 25, "Candy"),
            new Product("Daim dubbel", 15, "Candy"),
            new Product("Kexchoklad", 12, "Candy"),
            new Product("Malaco Gott & Blandat 160 g", 28, "Candy"),

            new Product("Marlboro Red (20-pack)", 89, "Tobacco"),
            new Product("Camel Blue (20-pack)", 85, "Tobacco"),
            new Product("L&M Filter (20-pack)", 79, "Tobacco"),
            new Product("Skruf Original Portion", 62, "Tobacco"),
            new Product("Göteborgs Rapé White Portion", 67, "Tobacco"),

            new Product("Aftonbladet (dagens)", 28, "Paper"),
            new Product("Expressen (dagens)", 28, "Paper"),
            new Product("Illustrerad Vetenskap", 79, "Paper"),
            new Product("Kalle Anka & Co", 45, "Paper"),
            new Product("Allt om Mat", 69, "Paper"),
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleCategoryPanel(string category, StackPanel targetPanel, string colorHex)
        {
            // Stäng alla andra paneler
            var panels = new List<StackPanel> { panelFood, panelCandy, panelTobacco, panelPaper };
            foreach (var panel in panels)
            {
                if (panel != targetPanel)
                {
                    panel.Visibility = Visibility.Collapsed;
                    panel.Children.Clear();
                }
            }

            // Om panel redan syns → stäng den
            if (targetPanel.Visibility == Visibility.Visible)
            {
                targetPanel.Visibility = Visibility.Collapsed;
                targetPanel.Children.Clear();
                return;
            }

            // Visa panel
            targetPanel.Children.Clear();
            targetPanel.Visibility = Visibility.Visible;

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
        }

        private List<CartItem> cart = new List<CartItem>();

        private void ProductSelected(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Product product = button.Tag as Product;

            // Kolla om produkten redan finns i kundvagnen
            var existingItem = cart.FirstOrDefault(c => c.Product.Name == product.Name);

            if (existingItem != null)
            {
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

        private void ButtonFood(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Food", panelFood, "#FF6495ED"); // CornflowerBlue
        }

        private void ButtonCandy(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Candy", panelCandy, "#FF87CEEB"); // SkyBlue
        }

        private void ButtonTobacco(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Tobacco", panelTobacco, "#FFADD8E6"); // LightBlue
        }

        private void ButtonPaper(object sender, RoutedEventArgs e)
        {
            ToggleCategoryPanel("Paper", panelPaper, "#FFB0C4DE"); // LightSteelBlue
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

            // Skapa kvitto
            var receipt = ReceiptManager.CreateReceipt(cart.ToList());

            // Visa kvittot i listboxen
            lstReceipts.Items.Add($"Kvittosnr: {receipt.ReceiptNumber}, Total: {receipt.TotalPrice} kr, Tid: {receipt.Timestamp}");

            // Spara som PDF (exempelväg)
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(folder, $"Receipt_{receipt.ReceiptNumber}.pdf");
            receipt.SaveAsPdf(filePath);

            // Töm kundvagn
            cart.Clear();
            lstProducts.Items.Clear();
            totalSum = 0;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            txtTotal.Text = $"{totalSum} kr";
        }

        private void ReceiptSelected(object sender, SelectionChangedEventArgs e)
        {
            if (lstReceipts.SelectedIndex < 0) return;

            var receipt = Receipt.ReceiptManager.AllReceipts[lstReceipts.SelectedIndex];

            txtReceiptNumber.Text = $"Kvittonr: {receipt.ReceiptNumber}";
            txtReceiptTime.Text = $"Tid: {receipt.Timestamp}";
            txtReceiptTotal.Text = $"Total: {receipt.TotalPrice} kr";

            itemsReceiptPanel.ItemsSource = receipt.Items;
        }
    }
}