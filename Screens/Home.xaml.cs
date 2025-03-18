using System.Windows;
using System.Windows.Controls;

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
            // Buscar la ventana principal
            Window1 parentWindow = Window.GetWindow(this) as Window1;

            // Llamar al método newFile de la ventana principal
            if (parentWindow != null)
            {
                // Simular un clic en el botón de archivo nuevo en la ventana principal
                parentWindow.GetType().GetMethod("newFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(parentWindow, new object[] { sender, e });
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
    }
}
