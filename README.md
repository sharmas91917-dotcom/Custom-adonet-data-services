# Custom ADO.NET Data Service with Caching and Web API Endpoints

## Overview
This project implements a backend data access layer using **ADO.NET** (`SqlConnection`, `SqlCommand`, parameterized queries), **in-memory caching** (`MemoryCache` with thread-safe double-check locking and automatic cache invalidation on writes), a **Custom ADO.NET Data Service** (`DataService<T>` with query/change interceptors and a service operation), and **ASP.NET Web API** endpoints.

## Project Structure
- `Database/CreateProductsTable.sql` — SQL Server database and `dbo.Products` table script with sample data.
- `Src/CachedAdoProductRepository.cs` — ADO.NET CRUD operations and `MemoryCache` caching layer.
- `Src/CustomProductDataService.cs` — Custom ADO.NET Data Service exposing `Products` and `GetExpensiveProducts`.
- `Src/ProductsController.cs` — RESTful Web API endpoints for products and manual cache refresh.

## Setup Instructions
1. Run `Database/CreateProductsTable.sql` in SQL Server / SSMS.
2. Add a `DefaultConnection` connection string in `Web.config` pointing to `ProductCatalogDb`.
3. Open the project in Visual Studio (`.NET Framework` ASP.NET Web Application) and run it.

## Sample Endpoints
- `GET /api/products` — Retrieve all products (cached).
- `GET /api/products/{id}` — Retrieve a product by ID.
- `POST /api/products` — Create a product and invalidate cache.
- `PUT /api/products/{id}` — Update a product and invalidate cache.
- `DELETE /api/products/{id}` — Delete a product and invalidate cache.
- `POST /api/products/cache/refresh` — Manually clear and reload cache.
- `GET /CustomProductDataService.svc/Products` — Query products via Custom ADO.NET Data Service.
- 
