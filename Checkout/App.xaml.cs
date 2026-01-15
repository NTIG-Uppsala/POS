using System.Configuration;
using System.Data;
using System.Windows;

namespace Checkout
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 🔑 Om tester skickar in db-path
            if (e.Args.Length > 0)
            {
                ProductRepository.SetDatabasePath(e.Args[0]);
                Database.UseDatabase(e.Args[0]);
            }

            Database.EnsureCreated();
        }
    }

}
