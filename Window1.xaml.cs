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
        }

        private void newFile(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TablaDePresupuestos();
        }

        private void HomeWindows(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Home();
        }

        private void CompareFiles(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Comparator();
        }
        #endregion
    }
}
