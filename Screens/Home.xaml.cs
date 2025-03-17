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
    }
}
