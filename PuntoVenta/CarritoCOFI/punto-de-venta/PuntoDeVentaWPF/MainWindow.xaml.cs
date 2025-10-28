using Newtonsoft.Json;
using PuntoDeVentaWPF.Models;
using PuntoDeVentaWPF.Services;
using PuntoDeVentaWPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PuntoDeVentaWPF
{
    public partial class MainWindow : Window
    {
        private readonly string API_URL = "http://localhost:3000";
        private readonly Carrito _carrito;

        //Added role
        private readonly RolUsuario _rolActual;

        public MainWindow(RolUsuario rol)
        {
            InitializeComponent();
            _rolActual = rol;
            _carrito = new Carrito(API_URL);
            ConfigurarPermisos();
            RefreshCarritoGrid();
        }

        private void ConfigurarPermisos()
        {
            // La función de "Dueño" para añadir producto está en el TabItem "Carrito"
            // Queremos que el botón 'BtnAgregarProducto_Click' solo sea visible para el Dueño.
            
            // Debes cambiar el tipo de botón en XAML de un StackPanel a la propiedad 
            // de un botón específico. Para el ejemplo, usaremos el botón 'BtnAgregarProducto_Click'

            if (_rolActual == RolUsuario.Dueno)
            {
                // Habilita todas las funciones
                BtnAgregarProducto.Visibility = Visibility.Visible;
                // Puedes añadir otras habilitaciones aquí si tienes botones solo para el dueño
            }
            else
            {
                // Oculta o deshabilita las funciones de Dueño para el Trabajador
                BtnAgregarProducto.Visibility = Visibility.Collapsed; // Lo oculta
                // o si prefieres deshabilitarlo, usarías: BtnAgregarProducto.IsEnabled = false;
            }
        }
        private async void BtnMostrarCategorias_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var http = new System.Net.Http.HttpClient();
                var resp = await http.GetAsync($"{API_URL}/categorias");
                resp.EnsureSuccessStatusCode();
                var cjson = await resp.Content.ReadAsStringAsync();
                var categorias = JsonConvert.DeserializeObject<List<Categoria>>(cjson);
                GridCategorias.ItemsSource = categorias;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudieron obtener las categorías:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            // pedir id
            var tecladoId = new TecladoWindow("ID del producto") { Owner = this };
            if (tecladoId.ShowDialog() != true) return;
            if (!int.TryParse(tecladoId.Resultado, out int idProducto) || idProducto <= 0)
            {
                MessageBox.Show("ID inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // pedir cantidad
            var tecladoCantidad = new TecladoWindow("Cantidad") { Owner = this };
            if (tecladoCantidad.ShowDialog() != true) return;
            if (!int.TryParse(tecladoCantidad.Resultado, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Cantidad inválida.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (ok, result) = await _carrito.AgregarProductoAsync(idProducto, cantidad);
            if (!ok)
            {
                MessageBox.Show(result.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshCarritoGrid();
        }

        private async void BtnEscanear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productos = await _carrito.ObtenerTodosProductosAsync();
                if (productos == null || !productos.Any())
                {
                    MessageBox.Show("No hay productos en la base de datos.", "Escanear", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var rnd = new Random();
                var producto = productos[rnd.Next(productos.Count)];
                var (ok, res) = await _carrito.AgregarProductoAsync(producto.producto_id, 1);
                if (ok)
                {
                    MessageBox.Show($"Producto agregado: {producto.nombre}", "Escanear", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshCarritoGrid();
                }
                else
                {
                    MessageBox.Show(res.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo escanear producto:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (GridCarrito.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un producto del carrito para eliminar.", "Eliminar", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic row = GridCarrito.SelectedItem;
            int index = row.Index;
            var (ok, msg) = _carrito.EliminarProducto(index);
            if (ok)
            {
                RefreshCarritoGrid();
            }
            else
            {
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnIniciarPago_Click(object sender, RoutedEventArgs e)
        {
            var (ok, msg) = await _carrito.IniciarPagoAsync();
            if (ok)
            {
                MessageBox.Show(msg, "Pago", MessageBoxButton.OK, MessageBoxImage.Information);
                _carrito.Vaciar();
                RefreshCarritoGrid();
            }
            else
            {
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCarritoGrid()
        {
            var view = _carrito.Items.Select((p, idx) => new
            {
                Index = idx + 1,
                p.nombre,
                precio = p.precio.ToString("F2"),
                tipo = string.IsNullOrEmpty(p.tipo) ? "general" : p.tipo,
                cantidad = p.cantidad,
                TotalItem = (p.precio * (p.cantidad > 0 ? p.cantidad : 1)).ToString("F2")
            }).ToList();

            GridCarrito.ItemsSource = view;
            LblSubtotal.Text = $"Subtotal: Bs {_carrito.Subtotal:F2}";
            LblTotal.Text = $"Total: Bs {_carrito.Total:F2}";
        }

        //ADD
        // Genera y abre un HTML con el ticket de la venta actual (venta en memoria)
        private void MostrarReporteVentaActual(bool autoPrint = true)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'><title>Ticket - Venta actual</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;padding:12px}h1{font-size:18px}table{width:100%;border-collapse:collapse;margin-top:8px}th,td{border:1px solid #ddd;padding:6px;text-align:left}tfoot td{font-weight:bold}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>Factura</h1>");
            sb.AppendLine($"<p>Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");

            sb.AppendLine("<table><thead><tr><th>#</th><th>Producto</th><th>Cantidad</th><th>Precio u.</th><th>Subtotal</th></tr></thead><tbody>");
            int i = 1;
            foreach (var p in _carrito.Items)
            {
                var cantidad = p.cantidad > 0 ? p.cantidad : 1;
                var subtotal = (p.precio * cantidad).ToString("F2");
                sb.AppendLine($"<tr><td>{i++}</td><td>{System.Net.WebUtility.HtmlEncode(p.nombre)}</td><td>{cantidad}</td><td>Bs {p.precio:F2}</td><td>Bs {subtotal}</td></tr>");
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine($"<tfoot><tr><td colspan='4'>Subtotal</td><td>Bs {_carrito.Subtotal:F2}</td></tr>");
            sb.AppendLine($"<tr><td colspan='4'>Total</td><td>Bs {_carrito.Total:F2}</td></tr></tfoot>");
            sb.AppendLine("</table>");

            if (autoPrint)
                sb.AppendLine("<script>window.onload = function(){ window.print(); }</script>");

            sb.AppendLine("</body></html>");

            var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ticket_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            System.IO.File.WriteAllText(temp, sb.ToString(), System.Text.Encoding.UTF8);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = temp,
                UseShellExecute = true
            });
        }

        private void BtnImprimirVentaActual_Click(object sender, RoutedEventArgs e)
        {
            if (!_carrito.Items.Any())
            {
                MessageBox.Show("El carrito está vacío.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MostrarReporteVentaActual(true);
        }
    }
}

