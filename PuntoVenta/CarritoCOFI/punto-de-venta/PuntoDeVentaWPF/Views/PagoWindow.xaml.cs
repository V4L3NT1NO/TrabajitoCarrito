using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using QRCoder;
using Newtonsoft.Json.Linq;

namespace PuntoDeVentaWPF.Views
{
    public partial class PagoWindow : Window
    {
        public enum Metodo { Efectivo, Tarjeta, QR }

        public double Total { get; }
        public Metodo MetodoSeleccionado { get; private set; } = Metodo.Efectivo;
        public double Recibido { get; private set; }
        public double Cambio { get; private set; }
        public bool SolicitarTicket => (ChkTicket?.IsChecked == true);

        HttpClient http = new HttpClient { BaseAddress = new Uri("http://localhost:3000") };
        string sesionQr = null;
        string linkQr = null;
        bool qrPagado = false;
        CancellationTokenSource cts;

        public PagoWindow(double total)
        {
            InitializeComponent();
            Total = Math.Round(total, 2);
            LblTotal.Text = Total.ToString("F2", CultureInfo.InvariantCulture);
            TxtRecibido.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ActualizarUI();
        }

        static readonly Regex soloNum = new(@"^[0-9]*[.,]?[0-9]*$");

        private void SoloNumero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            e.Handled = !soloNum.IsMatch(tb.Text + e.Text);
        }

        private void TxtRecibido_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TxtRecibido.Text.Replace(',', '.'), NumberStyles.Any,
                                CultureInfo.InvariantCulture, out var rec))
            {
                Recibido = Math.Round(rec, 2);
                Cambio = Recibido >= Total ? Math.Round(Recibido - Total, 2) : 0.0;
                TxtCambio.Text = Cambio.ToString("F2", CultureInfo.InvariantCulture);
            }
            else
            {
                Recibido = 0; Cambio = 0;
                TxtCambio.Text = "0.00";
            }
            ActualizarUI();
        }

        private void Metodo_Checked(object sender, RoutedEventArgs e)
        {
            if (RbEfectivo?.IsChecked == true) MetodoSeleccionado = Metodo.Efectivo;
            else if (RbTarjeta?.IsChecked == true) MetodoSeleccionado = Metodo.Tarjeta;
            else if (RbQR?.IsChecked == true) MetodoSeleccionado = Metodo.QR;

            if (MetodoSeleccionado != Metodo.QR)
            {
                CancelarPolling();
                sesionQr = null;
                linkQr = null;
                qrPagado = false;

                if (ImgQr != null) ImgQr.Source = null;
                if (TxtLinkQr != null) TxtLinkQr.Text = "";
                if (LblEstadoQr != null) LblEstadoQr.Text = "";
            }

            ActualizarUI();
        }

        private void ActualizarUI()
        {
            if (GbEfectivo != null)
                GbEfectivo.IsEnabled = (MetodoSeleccionado == Metodo.Efectivo);

            if (BtnGenerarQr != null)
                BtnGenerarQr.IsEnabled = (MetodoSeleccionado == Metodo.QR) && string.IsNullOrEmpty(sesionQr);

            if (BtnCopiarLink != null)
                BtnCopiarLink.IsEnabled = (MetodoSeleccionado == Metodo.QR) && !string.IsNullOrEmpty(linkQr);

            if (BtnConfirmar != null)
                BtnConfirmar.IsEnabled =
                    (MetodoSeleccionado == Metodo.Efectivo && Recibido >= Total) ||
                    (MetodoSeleccionado == Metodo.QR && qrPagado);
        }


        async void BtnGenerarQr_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CancelarPolling();
                qrPagado = false;
                sesionQr = null;
                linkQr = null;
                ImgQr.Source = null;
                TxtLinkQr.Text = "";
                LblEstadoQr.Text = "Creando QR...";

                var body = new StringContent($@"{{""total"":{Total.ToString("F2", CultureInfo.InvariantCulture)}}}",
                                             System.Text.Encoding.UTF8, "application/json");
                var resp = await http.PostAsync("/qr/create", body);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                var o = JObject.Parse(json);
                sesionQr = (string)o["sessionId"];
                linkQr = (string)o["payment_url"];
                TxtLinkQr.Text = linkQr;

                var qrGen = new QRCodeGenerator();
                var data = qrGen.CreateQrCode(linkQr, QRCodeGenerator.ECCLevel.Q);
                var pngQr = new PngByteQRCode(data).GetGraphic(20);
                ImgQr.Source = ByteArrayToBitmapImage(pngQr);

                LblEstadoQr.Text = "Escanea el QR";
                BtnCopiarLink.IsEnabled = true;

                cts = new CancellationTokenSource();
                _ = ConsultarEstadoQrAsync(cts.Token);
            }
            catch (Exception ex)
            {
                LblEstadoQr.Text = "Error al crear QR";
                MessageBox.Show(ex.Message, "QR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ActualizarUI();
            }
        }

        async Task ConsultarEstadoQrAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !string.IsNullOrEmpty(sesionQr))
            {
                try
                {
                    await Task.Delay(1000, token);
                    var resp = await http.GetAsync($"/qr/status/{sesionQr}", token);
                    if (!resp.IsSuccessStatusCode) continue;
                    var txt = await resp.Content.ReadAsStringAsync(token);
                    var o = JObject.Parse(txt);
                    var estado = (string)o["status"];

                    if (estado == "paid")
                    {
                        qrPagado = true;
                        Dispatcher.Invoke(() =>
                        {
                            LblEstadoQr.Text = "Pago confirmado";
                            ActualizarUI();
                        });
                        break;
                    }
                    else if (estado == "expired")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LblEstadoQr.Text = "QR expirado";
                            BtnGenerarQr.IsEnabled = true;
                        });
                        break;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => LblEstadoQr.Text = "Escanea el QR");
                    }
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }

        static BitmapImage ByteArrayToBitmapImage(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        void BtnCopiarLink_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(linkQr))
                Clipboard.SetText(linkQr);
        }

        void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            CancelarPolling();
            DialogResult = false;
            Close();
        }

        void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (MetodoSeleccionado == Metodo.Efectivo && Recibido < Total)
            {
                MessageBox.Show("El monto recibido es insuficiente.", "Pago",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MetodoSeleccionado == Metodo.QR && !qrPagado)
            {
                MessageBox.Show("El pago por QR aún no fue confirmado.", "Pago",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }

        void CancelarPolling()
        {
            try { cts?.Cancel(); } catch { }
            cts = null;
        }
    }
}
