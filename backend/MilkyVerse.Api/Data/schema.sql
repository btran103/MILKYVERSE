-- =====================================================================
-- MILKYVERSE DATABASE SCHEMA & SEED DATA (MYSQL 8.0+)
-- Hệ thống Thương mại Điện tử Trà Sữa Tích hợp AI Barista
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `milkyverse_db` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `milkyverse_db`;

-- 1. USERS
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `FullName` VARCHAR(100) NOT NULL,
    `Email` VARCHAR(150) NOT NULL UNIQUE,
    `PasswordHash` VARCHAR(255) NOT NULL,
    `PhoneNumber` VARCHAR(20) DEFAULT '',
    `Address` VARCHAR(255) DEFAULT '',
    `Role` VARCHAR(50) NOT NULL DEFAULT 'Customer',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. CATEGORIES
CREATE TABLE IF NOT EXISTS `Categories` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Slug` VARCHAR(120) NOT NULL UNIQUE,
    `Description` VARCHAR(500) DEFAULT '',
    `ImageUrl` VARCHAR(255) DEFAULT '',
    `DisplayOrder` INT NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. PRODUCTS
CREATE TABLE IF NOT EXISTS `Products` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `CategoryId` INT NOT NULL,
    `Name` VARCHAR(150) NOT NULL,
    `Slug` VARCHAR(160) NOT NULL UNIQUE,
    `Description` VARCHAR(1000) DEFAULT '',
    `BasePrice` DECIMAL(12,2) NOT NULL,
    `ImageUrl` VARCHAR(255) DEFAULT '',
    `IsBestSeller` TINYINT(1) NOT NULL DEFAULT 0,
    `IsNew` TINYINT(1) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `StockStatus` INT NOT NULL DEFAULT 1,
    CONSTRAINT `FK_Products_Categories` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. INGREDIENTS
CREATE TABLE IF NOT EXISTS `Ingredients` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Unit` VARCHAR(20) NOT NULL DEFAULT 'g',
    `CurrentStock` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `MinAlertThreshold` DECIMAL(12,2) NOT NULL DEFAULT 100.00,
    `UnitCost` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. TOPPINGS
CREATE TABLE IF NOT EXISTS `Toppings` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `IngredientId` INT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `Price` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `PortionAmount` DECIMAL(12,2) NOT NULL DEFAULT 40.00,
    `IsAvailable` TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT `FK_Toppings_Ingredients` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. RECIPES & RECIPE INGREDIENTS
CREATE TABLE IF NOT EXISTS `Recipes` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `ProductId` INT NOT NULL,
    `Size` VARCHAR(10) NOT NULL DEFAULT 'M',
    `Notes` VARCHAR(255) DEFAULT '',
    CONSTRAINT `FK_Recipes_Products` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RecipeIngredients` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `RecipeId` INT NOT NULL,
    `IngredientId` INT NOT NULL,
    `Amount` DECIMAL(12,2) NOT NULL,
    CONSTRAINT `FK_RecipeIngredients_Recipes` FOREIGN KEY (`RecipeId`) REFERENCES `Recipes` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RecipeIngredients_Ingredients` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 7. ORDERS
CREATE TABLE IF NOT EXISTS `Orders` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `OrderCode` VARCHAR(50) NOT NULL UNIQUE,
    `UserId` INT NULL,
    `Subtotal` DECIMAL(12,2) NOT NULL,
    `DiscountAmount` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `ShippingFee` DECIMAL(12,2) NOT NULL DEFAULT 15000.00,
    `TotalAmount` DECIMAL(12,2) NOT NULL,
    `Status` VARCHAR(30) NOT NULL DEFAULT 'Pending',
    `PaymentStatus` VARCHAR(30) NOT NULL DEFAULT 'Pending',
    `PaymentMethod` VARCHAR(30) NOT NULL DEFAULT 'COD',
    `CustomerName` VARCHAR(100) NOT NULL,
    `CustomerPhone` VARCHAR(20) NOT NULL,
    `ShippingAddress` VARCHAR(255) NOT NULL,
    `Note` VARCHAR(500) DEFAULT '',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_Orders_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 8. ORDER DETAILS & OPTIONS
CREATE TABLE IF NOT EXISTS `OrderDetails` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `OrderId` INT NOT NULL,
    `ProductId` INT NULL,
    `ProductName` VARCHAR(150) NOT NULL,
    `Size` VARCHAR(10) NOT NULL DEFAULT 'M',
    `SugarLevel` VARCHAR(20) NOT NULL DEFAULT '100%',
    `IceLevel` VARCHAR(20) NOT NULL DEFAULT '100%',
    `UnitPrice` DECIMAL(12,2) NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    `TotalPrice` DECIMAL(12,2) NOT NULL,
    `IsCustomDIY` TINYINT(1) NOT NULL DEFAULT 0,
    `CustomNotes` VARCHAR(500) DEFAULT '',
    CONSTRAINT `FK_OrderDetails_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_OrderDetails_Products` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `OrderItemOptions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `OrderDetailId` INT NOT NULL,
    `ToppingId` INT NULL,
    `ToppingName` VARCHAR(100) NOT NULL,
    `Price` DECIMAL(12,2) NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    CONSTRAINT `FK_OrderItemOptions_OrderDetails` FOREIGN KEY (`OrderDetailId`) REFERENCES `OrderDetails` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 9. ORDER STATUS HISTORIES
CREATE TABLE IF NOT EXISTS `OrderStatusHistories` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `OrderId` INT NOT NULL,
    `Status` VARCHAR(50) NOT NULL,
    `Note` VARCHAR(255) DEFAULT '',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_OrderStatusHistories_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 10. PROMOTIONS
CREATE TABLE IF NOT EXISTS `Promotions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Code` VARCHAR(50) NOT NULL UNIQUE,
    `Description` VARCHAR(255) DEFAULT '',
    `DiscountPercent` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    `DiscountAmount` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `MinOrderValue` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `MaxUsage` INT NOT NULL DEFAULT 1000,
    `UsedCount` INT NOT NULL DEFAULT 0,
    `StartDate` DATETIME NOT NULL,
    `EndDate` DATETIME NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 11. INVENTORY TRANSACTIONS
CREATE TABLE IF NOT EXISTS `InventoryTransactions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `IngredientId` INT NOT NULL,
    `TransactionType` VARCHAR(30) NOT NULL DEFAULT 'IMPORT',
    `Quantity` DECIMAL(12,2) NOT NULL,
    `BalanceAfter` DECIMAL(12,2) NOT NULL,
    `ReferenceType` VARCHAR(50) DEFAULT '',
    `ReferenceId` VARCHAR(50) DEFAULT '',
    `Note` VARCHAR(255) DEFAULT '',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_InventoryTransactions_Ingredients` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 12. CHAT SESSIONS & MESSAGES (AI BARISTA)
CREATE TABLE IF NOT EXISTS `ChatSessions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NULL,
    `SessionTitle` VARCHAR(150) NOT NULL DEFAULT 'Trò chuyện mới',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_ChatSessions_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ChatMessages` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `SessionId` INT NOT NULL,
    `SenderRole` VARCHAR(20) NOT NULL DEFAULT 'User',
    `Content` TEXT NOT NULL,
    `SuggestedDrinkJson` TEXT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_ChatMessages_ChatSessions` FOREIGN KEY (`SessionId`) REFERENCES `ChatSessions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- SAMPLE SEED DATA
INSERT INTO `Users` (`FullName`, `Email`, `PasswordHash`, `PhoneNumber`, `Address`, `Role`, `CreatedAt`)
VALUES 
('Quản trị viên MilkyVerse', 'admin@milkyverse.com', '$2a$11$qBgn.w8842i686m/U4/x6.Yg46v54q0F7aR66r7K4b.d9j5gVzKae', '0901234567', 'Số 1 Vũ Trụ Trà Sữa, Quận 1, TP.HCM', 'Admin', NOW()),
('Nguyễn Barista (Nhân viên)', 'staff@milkyverse.com', '$2a$11$qBgn.w8842i686m/U4/x6.Yg46v54q0F7aR66r7K4b.d9j5gVzKae', '0909876543', 'Chi nhánh MilkyVerse Q3', 'Staff', NOW()),
('Trần Bảo Ngọc', 'khachhang@milkyverse.com', '$2a$11$qBgn.w8842i686m/U4/x6.Yg46v54q0F7aR66r7K4b.d9j5gVzKae', '0988776655', '123 Đường Sữa Tươi, Quận 1', 'Customer', NOW());

INSERT INTO `Categories` (`Name`, `Slug`, `Description`, `ImageUrl`, `DisplayOrder`, `IsActive`)
VALUES 
('Trà Sữa Truyền Thống', 'tra-sua-truyen-thong', 'Hương vị trà sữa cổ điển đậm đà từ lá hồng trà tuyển chọn.', 'https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80', 1, 1),
('Trà Sữa Oolong & Rang', 'tra-sua-oolong-rang', 'Hậu vị sâu lắng, thơm nồng khói gỗ và trà ô long thượng hạng.', 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=800&auto=format&fit=crop&q=80', 2, 1),
('Trà Trái Cây Tươi', 'tra-trai-cay-tuoi', 'Thanh mát giải nhiệt với 100% trái cây nhiệt đới tự nhiên.', 'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80', 3, 1),
('Macchiato & Cheese Foam', 'macchiato-cheese-foam', 'Lớp kem sữa phô mai Anchor bồng bềnh béo mặn.', 'https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80', 4, 1);

INSERT INTO `Promotions` (`Code`, `Description`, `DiscountPercent`, `DiscountAmount`, `MinOrderValue`, `MaxUsage`, `UsedCount`, `StartDate`, `EndDate`, `IsActive`)
VALUES 
('MILKYNEW', 'Giảm 20% cho khách hàng mới', 20.00, 0.00, 50000.00, 500, 0, NOW(), DATE_ADD(NOW(), INTERVAL 90 DAY), 1),
('FREESHIP', 'Miễn phí vận chuyển 15.000đ', 0.00, 15000.00, 99000.00, 1000, 0, NOW(), DATE_ADD(NOW(), INTERVAL 180 DAY), 1),
('AIBARISTA', 'Giảm 30.000đ theo tư vấn AI', 0.00, 30000.00, 120000.00, 200, 0, NOW(), DATE_ADD(NOW(), INTERVAL 60 DAY), 1);
