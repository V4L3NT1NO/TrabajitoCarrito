using System.Windows;

namespace PuntoDeVentaWPF.Views
{
    public partial class TecladoWindow : Window
    {
        public string Resultado { get; private set; } = "";

        public TecladoWindow(string titulo = "Ingrese valor")
        {
            InitializeComponent();
            Titulo.Text = titulo;
            Entry.Focus();
        }

        private void BtnChar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b)
                Entry.Text += b.Content.ToString();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Entry.Text))
                Entry.Text = Entry.Text.Substring(0, Entry.Text.Length - 1);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            Entry.Clear();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Resultado = Entry.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}