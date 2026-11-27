CREATE DATABASE ProductCatalogDb;
GO

USE ProductCatalogDb;
GO

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
    Stock INT NOT NULL CHECK (Stock >= 0)
);
GO

INSERT INTO dbo.Products (Name, Category, Price, Stock)
VALUES
    (N'Wireless Mouse', N'Electronics', 29.99, 50),
    (N'Mechanical Keyboard', N'Electronics', 89.50, 25),
    (N'Desk Lamp', N'Home Office', 45.00, 15);
GO
