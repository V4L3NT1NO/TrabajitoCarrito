const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const db = require('./dbFunctions');

const app = express();
const PORT = 3000;

app.use(cors());
app.use(bodyParser.json());

// --- GET ALL ---
app.get('/usuarios', (req, res) => res.json(db.getAllUsuarios()));
app.get('/ventas', (req, res) => res.json(db.getAllVentas()));
app.get('/detalles', (req, res) => res.json(db.getAllDetalles()));
app.get('/productos', (req, res) => res.json(db.getAllProductos()));
app.get('/categorias', (req, res) => res.json(db.getAllCategorias()));

// --- GET BY ID ---
app.get('/usuarios/:id', (req, res) => {
  const usuario = db.getUsuarioById(Number(req.params.id));
  usuario ? res.json(usuario) : res.status(404).json({ error: 'Usuario no encontrado' });
});
app.get('/ventas/:id', (req, res) => {
  const venta = db.getVentaById(Number(req.params.id));
  venta ? res.json(venta) : res.status(404).json({ error: 'Venta no encontrada' });
});
app.get('/detalles/:id', (req, res) => {
  const detalle = db.getDetalleById(Number(req.params.id));
  detalle ? res.json(detalle) : res.status(404).json({ error: 'Detalle no encontrado' });
});
app.get('/productos/:id', (req, res) => {
  const producto = db.getProductoById(Number(req.params.id));
  producto ? res.json(producto) : res.status(404).json({ error: 'Producto no encontrado' });
});
app.get('/categorias/:id', (req, res) => {
  const categoria = db.getCategoriaById(Number(req.params.id));
  categoria ? res.json(categoria) : res.status(404).json({ error: 'Categoría no encontrada' });
});

// --- ADD ---
app.post('/usuarios', (req, res) => {
  db.addUsuario(req.body);
  res.json({ status: 'Usuario agregado' });
});
app.post('/ventas', (req, res) => {
  db.addVenta(req.body);
  res.json({ status: 'Venta agregada' });
});

app.post('/detalles', (req, res) => {
    const { producto_id, cantidad } = req.body || {};
    const ok = db.actualizarStock(Number(producto_id), Number(cantidad));
    if (!ok) return res.status(400).json({ error: 'Stock insuficiente o producto no existe' });
    db.addDetalle(req.body);
    res.json({ status: 'Detalle agregado' });
});

app.post('/productos', (req, res) => {
  db.addProducto(req.body);
  res.json({ status: 'Producto agregado' });
});
app.post('/categorias', (req, res) => {
  db.addCategoria(req.body);
  res.json({ status: 'Categoría agregada' });
});

// --- VERIFICAR USUARIO ---
app.post('/verificarUsuario', (req, res) => {
  const { Nombre, Contra } = req.body;
  const existe = db.verificarUsuario(Nombre, Contra);
  res.json({ existe });
});

// --- REGISTRO DE VENTA CON TARJETA (SIMULADO) ---
app.post('/registrarVentaTarjeta', (req, res) => {
  try {
    const resultado = db.registrarVentaTarjeta(req.body);
    res.json(resultado);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Error al registrar la venta con tarjeta' });
  }
});


const qrSesiones = new Map();

function nuevoId() {
    return (Math.random().toString(36).slice(2) + Date.now().toString(36)).toUpperCase();
}

function crearSesionQR({ total, currency = 'BOB', ttlMs = 2 * 60 * 1000 }) {
    const sessionId = nuevoId();
    const orderRef = 'ORD-' + Math.random().toString(36).slice(2, 8).toUpperCase();
    const ahora = Date.now();
    const sesion = {
        sessionId,
        orderRef,
        total: Number(total) || 0,
        currency,
        status: 'pending',
        created_at: new Date(ahora).toISOString(),
        expires_at: new Date(ahora + ttlMs).toISOString()
    };
    qrSesiones.set(sessionId, sesion);

    
    setTimeout(() => {
        const s = qrSesiones.get(sessionId);
        if (s && s.status === 'pending') s.status = 'expired';
    }, ttlMs + 2000);

    return sesion;
}


app.post('/qr/create', (req, res) => {
    const { total, currency } = req.body || {};
    if (typeof total !== 'number') {
        return res.status(400).json({ error: 'total debe ser number' });
    }
    const sesion = crearSesionQR({ total, currency: currency || 'BOB' });
    sesion.payment_url = `http://localhost:${PORT}/qr/pay/${sesion.sessionId}`;
    res.json(sesion);
});


app.get('/qr/status/:sessionId', (req, res) => {
    const s = qrSesiones.get(req.params.sessionId);
    if (!s) return res.status(404).json({ error: 'QR no encontrado' });
    if (s.status === 'pending' && Date.now() > Date.parse(s.expires_at)) s.status = 'expired';
    res.json(s);
});


app.get('/qr/pay/:sessionId', (req, res) => {
    const s = qrSesiones.get(req.params.sessionId);
    if (!s) return res.status(404).send('QR no encontrado');
    if (Date.now() > Date.parse(s.expires_at)) {
        s.status = 'expired';
        return res.send('<h2>QR expirado</h2>');
    }
    s.status = 'paid';
    res.send('<h2>Pago simulado OK</h2><p>Ya puedes volver a la caja.</p>');
});


app.listen(PORT, () => {
  console.log(`API escuchando en http://localhost:${PORT}`);
});