using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Checkout
{
    public partial class MainWindow : Window
    {
        private decimal totalSum = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonAddCoffee(object sender, RoutedEventArgs e)
        {
            string product = "Kaffe - 49 kr";
            lstProducts.Items.Add(product);
            totalSum += 49;
            UpdateTotal();
        }

        private void ButtonClearCart(object sender, RoutedEventArgs e)
        {
            lstProducts.Items.Clear();
            totalSum = 0;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            txtTotal.Text = $"{totalSum} kr";
        }
    }
}