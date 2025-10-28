using System.Windows;

namespace PuntoDeVentaWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            /////////////////////////////////////////////// Su///////////////////////////
            // Keep the application running while we show the login dialog.
            // Use OnExplicitShutdown so closing the login dialog (which would otherwise
            // become the implicit MainWindow) doesn't trigger application shutdown.
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Global handler to surface unhandled exceptions during startup/runtime
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Unhandled exception: {args.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            /////////////////////////////////////////////// End Su///////////////////////////

            var loginWindow = new LoginWindow();
            
            // Muestra la ventana de login y espera
            if (loginWindow.ShowDialog() == true) 
            {
                // Si el login fue exitoso (DialogResult = true)

                // Crea la ventana principal y le pasa el rol
                var mainWindow = new MainWindow(loginWindow.RolAutenticado);
                
                /////////////////////////////////////////////// Sus///////////////////////////
                // Now set the MainWindow and change shutdown mode so the application
                // lifetime is tied to the main window.
                this.MainWindow = mainWindow;
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                /////////////////////////////////////////////// End Sus///////////////////////////
                   
                mainWindow.Show();
            }
            // Si el login falla (cierra la ventana o DialogResult != true), 
            // la aplicación simplemente terminará.
        }
    }
}