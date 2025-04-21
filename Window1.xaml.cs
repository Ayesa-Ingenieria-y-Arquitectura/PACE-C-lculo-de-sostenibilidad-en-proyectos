using Bc3_WPF.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bc3_WPF
{
    /// <summary>
    /// Lógica de interacción para Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            MainContent.Content = new Home();
        }

        #region MENU
        private void DisplayMenu(object sender, RoutedEventArgs e)
        {
            // En el nuevo diseño, el menú es siempre visible, pero podríamos
            // implementar un comportamiento de expansión si es necesario
            // (por ejemplo, mostrar texto junto a los iconos)

            // Si se quiere mantener el comportamiento original, se puede adaptar así:
            /*
            if (Menu.Visibility == Visibility.Visible)
            {
                Menu.Visibility = Visibility.Hidden;
                MenuButton.Visibility = Visibility.Visible;
            }
            else
            {
                Menu.Visibility = Visibility.Visible;
                MenuButton.Visibility = Visibility.Hidden;
            }
            */
        }

        private void ExploreFiles_Click(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal que contiene este UserControl
            Window mainWindow = Window.GetWindow(this);

            // Crear una instancia del UserControl Databases
            Bc3_WPF.Screens.TablaDePresupuestos databasesScreen = new Bc3_WPF.Screens.TablaDePresupuestos();

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

        private void HomeWindows(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Home();
        }
        #endregion
    }
}
