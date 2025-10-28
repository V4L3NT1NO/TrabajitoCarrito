const fs = require('fs');
const path = require('path');
const dbPath = path.join(__dirname, 'db.json');

function readDB() {
  try {
    return JSON.parse(fs.readFileSync(dbPath, 'utf8'));
  } catch (err) {
    // si no existe, inicializa con estructura vacía
    const init = {
      usuarios: [],
      ventas: [],
      detalles: [],
      productos: [],
      categorias: []
    };
    writeDB(init);
    return init;
  }
}

function writeDB(data) {
  fs.writeFileSync(dbPath, JSON.stringify(data, null, 2), 'utf8');
}

function registrarVentaTarjeta(data) {
  const { cliente_id, productos, total, tarjeta } = data;

  if (!cliente_id || !productos || productos.length === 0 || !tarjeta) {
    return { error: 'Datos incompletos en la solicitud' };
  }

  // Simulación de pago con tarjeta (sin conexión real)
  const pagoAprobado = Math.random() > 0.1; // 90% éxito
  const estadoPago = pagoAprobado ? 'APROBADO' : 'RECHAZADO';

  if (!pagoAprobado) {
    return { mensaje: 'Pago rechazado por el sistema simulado', estadoPago };
  }

  // Crear venta
  const nuevaVenta = {
    Venta_id: Date.now(),
    Cliente_id: cliente_id,
    Total: total,
    MetodoPago: 'Tarjeta',
    EstadoPago: estadoPago,
    Fecha: new Date().toISOString(),
  };

  const dbData = readDB();
  dbData.ventas.push(nuevaVenta);

  // Registrar los detalles de la venta
  productos.forEach(prod => {
    dbData.detalles.push({
      detalle_id: Date.now() + Math.floor(Math.random() * 1000),
      Venta_id: nuevaVenta.Venta_id,
      Producto_id: prod.id,
      Cantidad: prod.cantidad,
      Subtotal: prod.subtotal,
    });
  });

  writeDB(dbData);

  return {
    mensaje: 'Venta registrada correctamente',
    estadoPago,
    venta: nuevaVenta
  };
}


// --- GET ALL ---
function getAllUsuarios() { return readDB().usuarios; }
function getAllVentas() { return readDB().ventas; }
function getAllDetalles() { return readDB().detalles; }
function getAllProductos() { return readDB().productos; }
function getAllCategorias() { return readDB().categorias; }

// --- GET BY ID ---
function getUsuarioById(id) { return readDB().usuarios.find(u => u.Usuario_id === id); }
function getVentaById(id) { return readDB().ventas.find(v => v.Venta_id === id); }
function getDetalleById(id) { return readDB().detalles.find(d => d.detalles_id === id); }
function getProductoById(id) { return readDB().productos.find(p => p.producto_id === id); }
function getCategoriaById(id) { return readDB().categorias.find(c => c.categoria_id === id); }

// --- ADD ---
function addUsuario(usuario) { const db = readDB(); db.usuarios.push(usuario); writeDB(db); }
function addVenta(venta) { const db = readDB(); db.ventas.push(venta); writeDB(db); }
function addDetalle(detalle) { const db = readDB(); db.detalles.push(detalle); writeDB(db); }
function addProducto(producto) { const db = readDB(); db.productos.push(producto); writeDB(db); }
function addCategoria(categoria) { const db = readDB(); db.categorias.push(categoria); writeDB(db); }

function actualizarStock(productoId, cantidad) {
    const db = readDB();
    const prod = db.productos.find(p => p.producto_id === Number(productoId));
    if (!prod) return false;
    const cant = Number(cantidad) || 0;
    if (cant < 0) return false;
    if (prod.stock < cant) return false;
    prod.stock -= cant;
    writeDB(db);
    return true;
}


// --- VERIFICAR USUARIO ---
function verificarUsuario(nombre, contra) {
  const db = readDB();
  return db.usuarios.some(u => u.Nombre === nombre && u.Contra === contra);
}

module.exports = {
  getAllUsuarios,
  getAllVentas,
  getAllDetalles,
  getAllProductos,
  getAllCategorias,
  getUsuarioById,
  getVentaById,
  getDetalleById,
  getProductoById,
  getCategoriaById,
  addUsuario,
  addVenta,
  addDetalle,
  addProducto,
  addCategoria,
  verificarUsuario,
  actualizarStock
};