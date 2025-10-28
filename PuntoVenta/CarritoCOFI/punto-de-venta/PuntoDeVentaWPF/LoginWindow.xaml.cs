using System.Windows;

namespace PuntoDeVentaWPF
{
    // Define los posibles roles
    public enum RolUsuario { NoAutenticado, Trabajador, Dueno }

    public partial class LoginWindow : Window
    {
        public RolUsuario RolAutenticado { get; private set; } = RolUsuario.NoAutenticado;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string pin = PinBox.Password;

            if (pin == "1234") // PIN del DUEÑO
            {
                RolAutenticado = RolUsuario.Dueno;
                DialogResult = true; // Cierra la ventana exitosamente
            }
            else if (pin == "5678") // PIN del TRABAJADOR
            {
                RolAutenticado = RolUsuario.Trabajador;
                DialogResult = true; // Cierra la ventana exitosamente
            }
            else
            {
                MessageBox.Show("PIN incorrecto. Intente de nuevo.", "Error de Acceso", MessageBoxButton.OK, MessageBoxImage.Error);
                PinBox.Clear();
                PinBox.Focus();
            }
        }
    }
}