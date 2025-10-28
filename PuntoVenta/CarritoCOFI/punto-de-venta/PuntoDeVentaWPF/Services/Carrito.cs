using Newtonsoft.Json;
using PuntoDeVentaWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Globalization;


namespace PuntoDeVentaWPF.Services
{
    public class Carrito
    {
        private readonly HttpClient _http;
        public string ApiUrl { get; }
        public List<Producto> Items { get; } = new();
        public double Subtotal { get; private set; }
        public double Total { get; private set; }
        public bool PagoIniciado { get; private set; } = false;

        public Carrito(string apiUrl)
        {
            ApiUrl = apiUrl.TrimEnd('/');
            _http = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        private double CalcularDescuento(Producto p)
        {
            var tipo = (p.tipo ?? "").ToLower();
            if (tipo == "alimento") return p.precio * 0.10;
            if (tipo == "tecnología" || tipo == "tecnologia") return p.precio * 0.05;
            return 0.0;
        }

        private double CalcularImpuesto(Producto p)
        {
            var tipo = (p.tipo ?? "").ToLower();
            if (tipo == "alimento") return p.precio * 0.03;
            if (tipo == "tecnología" || tipo == "tecnologia") return p.precio * 0.13;
            return p.precio * 0.08;
        }

        private void ActualizarTotales()
        {
            Subtotal = 0.0;
            Total = 0.0;
            foreach (var p in Items)
            {
                var cantidad = p.cantidad > 0 ? p.cantidad : 1;
                var descuento = CalcularDescuento(p) * cantidad;
                var impuesto = CalcularImpuesto(p) * cantidad;
                var precioFinal = (p.precio * cantidad) - descuento + impuesto;
                Subtotal += p.precio * cantidad;
                Total += precioFinal;
            }
        }

        public async Task<(bool, object)> AgregarProductoAsync(int idProducto, int cantidad = 1)
        {
            if (PagoIniciado)
                return (false, "No se pueden agregar productos: el pago ya ha comenzado.");

            try
            {
                var resp = await _http.GetAsync($"{ApiUrl}/productos/{idProducto}");
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (false, $"Producto con ID {idProducto} no encontrado en la base de datos.");
                if (!resp.IsSuccessStatusCode)
                    return (false, $"Error al conectar con la API (código {(int)resp.StatusCode}).");

                var json = await resp.Content.ReadAsStringAsync();
                var producto = JsonConvert.DeserializeObject<Producto>(json);
                if (producto == null)
                    return (false, "Respuesta inválida del servidor.");

                if (producto.precio == 0)
                    return (false, $"El producto '{producto.nombre ?? "Desconocido"}' no tiene precio registrado.");

                if (string.IsNullOrWhiteSpace(producto.tipo))
                    producto.tipo = "general";



                if (producto.stock < cantidad)
                    {
                        return (false, $"Stock insuficiente. Solo quedan {producto.stock} unidades disponibles.");
                    }

                producto.cantidad = cantidad;
                Items.Add(producto);
                ActualizarTotales();
                return (true, producto);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexión con la API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool, string) EliminarProducto(int indice1Based)
        {
            if (PagoIniciado) return (false, "No puedes eliminar productos: el pago ya ha comenzado.");
            if (!Items.Any()) return (false, "El carrito está vacío.");
            if (indice1Based < 1 || indice1Based > Items.Count) return (false, "Índice inválido.");

            var producto = Items[indice1Based - 1];
            Items.RemoveAt(indice1Based - 1);
            ActualizarTotales();
            return (true, $"'{producto.nombre}' eliminado correctamente.");
        }

        public async Task<(bool, string)> IniciarPagoAsync()
        {
            if (!Items.Any()) return (false, "No puedes iniciar el pago: el carrito está vacío.");

            try
            {
                var dlg = new PuntoDeVentaWPF.Views.PagoWindow((double)Total) { Owner = System.Windows.Application.Current.MainWindow };
                if (dlg.ShowDialog() != true) return (false, "Pago cancelado por el usuario.");

                using var httpLocal = new HttpClient { BaseAddress = new Uri(ApiUrl) };

                var ventaBody = new
                {
                    Venta_id = (int?)null,
                    monto_total = Math.Round(Total, 2),
                    nit = "",
                    usuario_id = 1
                };

                var rVenta = await httpLocal.PostAsJsonAsync("/ventas", ventaBody);
                var bodyVenta = await rVenta.Content.ReadAsStringAsync();
                if (!rVenta.IsSuccessStatusCode)
                    return (false, $"POST /ventas => {(int)rVenta.StatusCode} {rVenta.ReasonPhrase}. Respuesta: {bodyVenta}");

                object ventaId = 1;

                foreach (var item in Items)
                {
                    int cant = item.cantidad > 0 ? item.cantidad : 1;
                    var detBody = new
                    {
                        detalles_id = (int?)null,
                        venta_id = ventaId,
                        producto_id = item.producto_id,
                        cantidad = cant,
                        precio_unitario = Math.Round(item.precio, 2)
                    };

                    var rDet = await httpLocal.PostAsJsonAsync("/detalles", detBody);
                    var bodyDet = await rDet.Content.ReadAsStringAsync();
                    if (!rDet.IsSuccessStatusCode)
                        return (false, $"POST /detalles => {(int)rDet.StatusCode} {rDet.ReasonPhrase}. Respuesta: {bodyDet}");
                }

                PagoIniciado = true;
                return (true, $"Pago registrado. Total: Bs {Total:F2}");
            }
            catch (Exception ex)
            {
                return (false, $"Error al registrar la venta o detalles: {ex.Message}");
            }
        }



        public async Task<List<Producto>> ObtenerTodosProductosAsync()
        {
            var resp = await _http.GetAsync($"{ApiUrl}/productos");
            resp.EnsureSuccessStatusCode();
            var cont = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Producto>>(cont);
        }

        public void Vaciar()
        {
            Items.Clear();
            ActualizarTotales();
            PagoIniciado = false;
        }
    }
}