# Punto de Venta

## Overview
Punto de Venta is a WPF application designed for managing sales transactions in a retail environment. It provides functionalities for handling products, categories, and sales, allowing users to efficiently manage their inventory and sales processes.

## Project Structure
The project consists of two main components: an API for backend operations and a WPF application for the user interface.

### API
- **api/package.json**: Configuration file for npm, listing dependencies and scripts for the API project.
- **api/api.js**: Sets up an Express server with routes for handling requests related to users, sales, details, products, and categories.
- **api/dbFunctions.js**: Contains functions for interacting with the database, including methods for CRUD operations.
- **api/db.json**: Serves as the database, storing data for users, sales, details, products, and categories in JSON format.

### WPF Application
- **PuntoDeVentaWPF/PuntoDeVentaWPF.csproj**: Project file for the WPF application, containing metadata about the project and its dependencies.
- **PuntoDeVentaWPF/App.xaml**: Defines application-level resources and settings for the WPF application.
- **PuntoDeVentaWPF/App.xaml.cs**: Code-behind for App.xaml, handling application startup and shutdown events.
- **PuntoDeVentaWPF/MainWindow.xaml**: Defines the main user interface layout, including a title bar and tab control for categories and the shopping cart.
- **PuntoDeVentaWPF/MainWindow.xaml.cs**: Code-behind for MainWindow.xaml, implementing logic for user interactions.
- **PuntoDeVentaWPF/Views/TecladoWindow.xaml**: Defines the user interface for a custom keyboard window for user input.
- **PuntoDeVentaWPF/Views/TecladoWindow.xaml.cs**: Code-behind for TecladoWindow.xaml, managing button clicks and user input.
- **PuntoDeVentaWPF/Models/Models.cs**: Defines data models for the application, including classes for Producto, Categoria, Venta, and Detalle.
- **PuntoDeVentaWPF/Services/Carrito.cs**: Contains the Carrito class, managing shopping cart functionality.

## Setup and Usage
1. **API Setup**:
   - Navigate to the `api` directory.
   - Run `npm install` to install the required dependencies.
   - Start the API server using `node api.js`.

2. **WPF Application Setup**:
   - Open the `PuntoDeVentaWPF` directory in a compatible IDE.
   - Build the project to restore dependencies.
   - Run the application to access the Punto de Venta interface.

## Features
- Manage products and categories.
- Add products to a shopping cart.
- Calculate totals and initiate payments.
- Custom keyboard for user input.

## License
This project is licensed under the MIT License.