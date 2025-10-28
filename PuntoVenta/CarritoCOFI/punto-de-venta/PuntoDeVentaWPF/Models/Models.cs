using System.Collections.Generic;

namespace PuntoDeVentaWPF.Models
{
    public class Producto
    {
        public int producto_id { get; set; }
        public string nombre { get; set; }
        public int categoria_id { get; set; }
        public double precio { get; set; }
        public int stock { get; set; }
        // extra fields
        public string tipo { get; set; } = "general";
        public int cantidad { get; set; } = 1;
    }

    public class Categoria
    {
        public int categoria_id { get; set; }
        public string nombre { get; set; }
    }

    public class Venta
    {
        public object Venta_id { get; set; } // backend may assign
        public double monto_total { get; set; }
        public string nit { get; set; }
        public int usuario_id { get; set; }
    }

    public class Detalle
    {
        public object detalles_id { get; set; }
        public object venta_id { get; set; }
        public int producto_id { get; set; }
        public int cantidad { get; set; }
        public double precio_unitario { get; set; }
    }
}