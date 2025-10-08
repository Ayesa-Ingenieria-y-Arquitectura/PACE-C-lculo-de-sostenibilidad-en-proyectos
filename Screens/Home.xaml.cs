using System.Windows;
using System.Windows.Controls;
using Bc3_WPF.Screens.Tabla_Presupuestos;

namespace Bc3_WPF.Screens
{
    /// <summary>
    /// Lógica de interacción para Home.xaml
    /// </summary>
    public partial class Home : System.Windows.Controls.UserControl
    {
        public Home()
        {
            InitializeComponent();
        }

        private void ExploreFiles_Click(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal que contiene este UserControl
            Window mainWindow = Window.GetWindow(this);

            // Crear una instancia del UserControl Databases
            TablaDePresupuestos databasesScreen = new TablaDePresupuestos();

            // Asumiendo que la ventana principal tiene un ContentControl o Frame llamado MainContent
            // donde se cargan los diferentes UserControls
            if (mainWindow != null)
            {
                // Si estás usando un ContentControl
                ContentControl contentControl = mainWindow.FindName("MainContent") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = databasesScreen;
                }
            }
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal que contiene este UserControl
            Window mainWindow = Window.GetWindow(this);

            // Crear una instancia del UserControl Databases
            Bc3_WPF.Screens.Databases databasesScreen = new Bc3_WPF.Screens.Databases();

            // Asumiendo que la ventana principal tiene un ContentControl o Frame llamado MainContent
            // donde se cargan los diferentes UserControls
            if (mainWindow != null)
            {
                // Si estás usando un ContentControl
                ContentControl contentControl = mainWindow.FindName("MainContent") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = databasesScreen;
                }

                // Si estás usando un Frame
                // Frame mainFrame = mainWindow.FindName("MainFrame") as Frame;
                // if (mainFrame != null)
                // {
                //     mainFrame.Content = databasesScreen;
                // }
            }
        }

        private void CompareClick(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal que contiene este UserControl
            Window mainWindow = Window.GetWindow(this);

            // Crear una instancia del UserControl Databases
            Bc3_WPF.Screens.Comparador databasesScreen = new Bc3_WPF.Screens.Comparador();

            // Asumiendo que la ventana principal tiene un ContentControl o Frame llamado MainContent
            // donde se cargan los diferentes UserControls
            if (mainWindow != null)
            {
                // Si estás usando un ContentControl
                ContentControl contentControl = mainWindow.FindName("MainContent") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = databasesScreen;
                }

                // Si estás usando un Frame
                // Frame mainFrame = mainWindow.FindName("MainFrame") as Frame;
                // if (mainFrame != null)
                // {
                //     mainFrame.Content = databasesScreen;
                // }
            }
        }
    }
}
