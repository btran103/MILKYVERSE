using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(MilkyVerseDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.Users.Any())
        {
            return; // DB has been seeded
        }

        // ==========================================
        // 1. USERS & ROLES
        // ==========================================
        var admin = new User
        {
            FullName = "Quản trị viên MilkyVerse",
            Email = "admin@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            PhoneNumber = "0901234567",
            Address = "Số 1 Đường Thiên Hà, Quận 1, TP.HCM",
            Role = "Admin",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var manager = new User
        {
            FullName = "Lê Quản Lý (Cửa hàng trưởng)",
            Email = "manager@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            PhoneNumber = "0903334455",
            Address = "Số 88 Đường Vũ Trụ, Quận 1, TP.HCM",
            Role = "Manager",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var staff = new User
        {
            FullName = "Nguyễn Barista & Bếp (Nhân viên quầy)",
            Email = "staff@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
            PhoneNumber = "0909876543",
            Address = "Chi nhánh MilkyVerse Quận 3, TP.HCM",
            Role = "Staff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var warehouse = new User
        {
            FullName = "Phạm Thủ Kho (Quản lý kho & FEFO)",
            Email = "warehouse@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Warehouse@123"),
            PhoneNumber = "0911223344",
            Address = "Kho Tổng MilkyVerse, Bình Thạnh, TP.HCM",
            Role = "WarehouseStaff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var shipper = new User
        {
            FullName = "Hoàng Giao Hàng (Shipper MilkyVerse)",
            Email = "shipper@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Shipper@123"),
            PhoneNumber = "0977889900",
            Address = "Đội Giao Vận MilkyVerse Galaxy Express",
            Role = "DeliveryStaff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer = new User
        {
            FullName = "Trần Bảo Ngọc (Khách VIP)",
            Email = "khachhang@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            PhoneNumber = "0988776655",
            Address = "123 Đường Sữa Tươi, Phường Bến Nghé, Quận 1, TP.HCM",
            Role = "Customer",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin, manager, staff, warehouse, shipper, customer);
        await context.SaveChangesAsync();

        // ==========================================
        // 2. CATEGORIES (DRINKS + 10 FOOD CATEGORIES + COMBOS)
        // ==========================================
        // Drinks (11 Drink Categories)
        var catClassic = new Category
        {
            Name = "Trà Sữa Truyền Thống",
            Slug = "tra-sua-truyen-thong",
            CategoryType = "DRINK",
            Description = "Hương vị trà sữa cổ điển đậm đà từ lá hồng trà tuyển chọn và sữa béo ngậy chuẩn vị Đài Loan.",
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 1,
            IsActive = true
        };

        var catFruit = new Category
        {
            Name = "Trà Trái Cây",
            Slug = "tra-trai-cay",
            CategoryType = "DRINK",
            Description = "Thanh mát giải nhiệt với 100% trái cây nhiệt đới tươi ngon tự nhiên giàu vitamin.",
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 2,
            IsActive = true
        };

        var catPureTea = new Category
        {
            Name = "Trà Nguyên Chất & Trà Pha Vị",
            Slug = "tra-nguyen-chat-pha-vi",
            CategoryType = "DRINK",
            Description = "Thưởng thức cốt trà mộc thanh khiết tuyển chọn từ những đồi trà danh tiếng và các biến tấu pha vị mộc mạc.",
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 3,
            IsActive = true
        };

        var catCheese = new Category
        {
            Name = "Trà Macchiato & Kem Cheese",
            Slug = "tra-macchiato-kem-cheese",
            CategoryType = "DRINK",
            Description = "Lớp kem sữa phô mai Anchor bồng bềnh béo mặn sánh mịn kết hợp nền trà hảo hạng thơm mát.",
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 4,
            IsActive = true
        };

        var catFreshMilkDrink = new Category
        {
            Name = "Sữa Tươi & Đồ Uống Sữa",
            Slug = "sua-tuoi-do-uong-sua",
            CategoryType = "DRINK",
            Description = "Sữa tươi thanh trùng Đà Lạt nguyên chất 100% béo ngậy tự nhiên kết hợp các nguyên liệu thượng hạng.",
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 5,
            IsActive = true
        };

        var catMatchaCacao = new Category
        {
            Name = "Matcha & Đồ Uống Cacao",
            Slug = "matcha-cacao",
            CategoryType = "DRINK",
            Description = "Nghệ thuật pha chế bột trà xanh Uji Nhật Bản và cacao nguyên chất thơm lừng nồng nàn.",
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 6,
            IsActive = true
        };

        var catFrappeIceBlended = new Category
        {
            Name = "Frappe & Đồ Uống Đá Xay",
            Slug = "frappe-da-xay",
            CategoryType = "DRINK",
            Description = "Thức uống đá xay tuyết sánh mịn mát lạnh, phủ kem whipping béo ngậy bồng bềnh phong cách hiện đại.",
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 7,
            IsActive = true
        };

        var catFruitJuice = new Category
        {
            Name = "Nước Ép Trái Cây",
            Slug = "nuoc-ep-trai-cay",
            CategoryType = "DRINK",
            Description = "100% nước ép nguyên chất từ hoa quả tươi hữu cơ, không thêm đường hóa học, thanh lọc cơ thể.",
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 8,
            IsActive = true
        };

        var catSodaGas = new Category
        {
            Name = "Soda & Đồ Uống Có Gas",
            Slug = "soda-do-uong-co-gas",
            CategoryType = "DRINK",
            Description = "Soda bọt ga sảng khoái kết hợp siro trái cây nhập khẩu và lá bạc hà mát lạnh xua tan nắng hè.",
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 9,
            IsActive = true
        };

        var catLemonRefreshment = new Category
        {
            Name = "Nước Chanh & Đồ Uống Giải Khát",
            Slug = "nuoc-chanh-giai-khat",
            CategoryType = "DRINK",
            Description = "Các món nước giải khát dân dã mộc mạc từ chanh tươi, tắc, mơ, sấu, dừa xiêm tự nhiên mát ruột.",
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 10,
            IsActive = true
        };

        var catCoffee = new Category
        {
            Name = "Cà Phê & Đồ Uống Bổ Sung",
            Slug = "ca-phe-do-uong-bo-sung",
            CategoryType = "DRINK",
            Description = "Hạt cà phê Robusta Buôn Ma Thuột & Arabica Cầu Đất phối trộn tinh tế, đánh thức mọi nguồn cảm hứng.",
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 11,
            IsActive = true
        };

        var catOolong = new Category
        {
            Name = "Trà Sữa Oolong & Rang",
            Slug = "tra-sua-oolong-rang",
            CategoryType = "DRINK",
            Description = "Hậu vị sâu lắng, thơm nồng khói gỗ và trà ô long thượng hạng từ cao nguyên.",
            ImageUrl = "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 12,
            IsActive = true
        };

        // Food Categories (10 categories required)
        var catVienChien = new Category
        {
            Name = "Cá Viên & Các Loại Viên Chiên",
            Slug = "vien-chien",
            CategoryType = "FOOD",
            Description = "Các loại viên chiên vàng ruộm giòn rụm: cá viên, bò viên, mực viên, thanh cua chấm tương đen & sốt bơ trứng.",
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 5,
            IsActive = true
        };

        var catGaChanGa = new Category
        {
            Name = "Gà & Các Món Chân Gà",
            Slug = "ga-chan-ga",
            CategoryType = "FOOD",
            Description = "Chân gà sốt Thái rút xương giòn sần sật, sả tắc thơm lừng, cánh gà chiên mắm và gà lắc phô mai đậm đà.",
            ImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 6,
            IsActive = true
        };

        var catKhoai = new Category
        {
            Name = "Khoai Tây & Các Món Từ Khoai",
            Slug = "khoai-tay-khoai-lang",
            CategoryType = "FOOD",
            Description = "Khoai tây chiên giòn rụm lắc phô mai, rong biển, BBQ, khoai lang kén béo bùi chuẩn vị ăn vặt tuổi thơ.",
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 7,
            IsActive = true
        };

        var catPhoMaiXucXich = new Category
        {
            Name = "Phô Mai, Xúc Xích & Đồ Chiên",
            Slug = "pho-mai-xuc-xich-do-chien",
            CategoryType = "FOOD",
            Description = "Phô mai que kéo sợi ngập tràn, xúc xích lốc xoáy, nem chua rán phố cổ và há cảo chiên giòn tan.",
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 8,
            IsActive = true
        };

        var catBanhTrang = new Category
        {
            Name = "Bánh Tráng & Các Món Trộn",
            Slug = "banh-trang-mon-tron",
            CategoryType = "FOOD",
            Description = "Bánh tráng trộn bò khô sả tế cay nồng, bánh tráng nướng Đà Lạt giòn rụm, bánh tráng cuốn sốt me bơ trứng béo ngậy.",
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 9,
            IsActive = true
        };

        var catTrungChen = new Category
        {
            Name = "Trứng Chén & Món Trứng Nướng",
            Slug = "trung-chen-mon-trung",
            CategoryType = "FOOD",
            Description = "Trứng chén nướng mỡ hành phô mai kéo sợi thơm phức, cút lộn xào me chua ngọt đậm đà cuốn hút.",
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 10,
            IsActive = true
        };

        var catMiCay = new Category
        {
            Name = "Mì & Các Món Cay 7 Cấp Độ",
            Slug = "mi-mon-cay",
            CategoryType = "FOOD",
            Description = "Mì cay Hàn Quốc 7 cấp độ bò Mỹ hải sản cay xuýt xoa, mì trộn xúc xích phô mai trứng lòng đào nóng hổi.",
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 11,
            IsActive = true
        };

        var catComNo = new Category
        {
            Name = "Cơm Chiên & Món Ăn No",
            Slug = "com-mon-no",
            CategoryType = "FOOD",
            Description = "Cơm chiên kim chi bò Mỹ hạt cơm tơi giòn, cơm chiên hải sản hạt vàng ươm, kimbap chiên giòn ấm bụng.",
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 12,
            IsActive = true
        };

        var catNoiTang = new Category
        {
            Name = "Nội Tạng Nướng & Xiên Nướng",
            Slug = "noi-tang-mon-nuong",
            CategoryType = "FOOD",
            Description = "Lòng nướng sa tế đậm đà than hồng, dồi sụn nướng giòn sần sật, sụn gà rang muối và xiên que nướng cay.",
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 13,
            IsActive = true
        };

        var catAnVatBoSung = new Category
        {
            Name = "Đồ Ăn Vặt Bổ Sung",
            Slug = "an-vat-bo-sung",
            CategoryType = "FOOD",
            Description = "Bắp xào bơ tép thơm nức mũi, bánh gạo tokbokki cay ngọt phô mai, khô gà lá chanh cay xé nhâm nhi cực đã.",
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 14,
            IsActive = true
        };

        var catCombo = new Category
        {
            Name = "Combo Thức Ăn & Nước Uống",
            Slug = "combo-an-uong",
            CategoryType = "COMBO",
            Description = "Combo kết hợp tuyệt hảo giữa trà sữa mát lạnh và các món ăn vặt nóng giòn với mức giá siêu ưu đãi.",
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 15,
            IsActive = true
        };

        context.Categories.AddRange(
            catClassic, catFruit, catPureTea, catCheese, catFreshMilkDrink,
            catMatchaCacao, catFrappeIceBlended, catFruitJuice, catSodaGas,
            catLemonRefreshment, catCoffee, catOolong,
            catVienChien, catGaChanGa, catKhoai, catPhoMaiXucXich, catBanhTrang,
            catTrungChen, catMiCay, catComNo, catNoiTang, catAnVatBoSung, catCombo
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 3. INGREDIENTS (DRINK & FOOD & WAREHOUSE)
        // ==========================================
        var ingBlackTea = new Ingredient { Name = "Cốt Hồng Trà Ceylon", Unit = "ml", CurrentStock = 25000, MinAlertThreshold = 3000, UnitCost = 30, CategoryType = "BEVERAGE" };
        var ingGreenTea = new Ingredient { Name = "Cốt Lục Trà Lài", Unit = "ml", CurrentStock = 20000, MinAlertThreshold = 3000, UnitCost = 35, CategoryType = "BEVERAGE" };
        var ingOolongTea = new Ingredient { Name = "Cốt Ô Long Rang Khói", Unit = "ml", CurrentStock = 18000, MinAlertThreshold = 2500, UnitCost = 45, CategoryType = "BEVERAGE" };
        var ingFreshMilk = new Ingredient { Name = "Sữa Tươi Thanh Trùng Dalat", Unit = "ml", CurrentStock = 30000, MinAlertThreshold = 5000, UnitCost = 28, CategoryType = "BEVERAGE" };
        var ingSugarSyrup = new Ingredient { Name = "Nước Đường Mía Tự Nhiên", Unit = "ml", CurrentStock = 40000, MinAlertThreshold = 5000, UnitCost = 15, CategoryType = "BEVERAGE" };
        var ingPearl = new Ingredient { Name = "Trân Châu Đen Hoàng Kim", Unit = "g", CurrentStock = 15000, MinAlertThreshold = 2000, UnitCost = 50, CategoryType = "BEVERAGE" };
        var ingWhitePearl = new Ingredient { Name = "Trân Châu Trắng 3Q Giòn", Unit = "g", CurrentStock = 12000, MinAlertThreshold = 1500, UnitCost = 65, CategoryType = "BEVERAGE" };
        var ingCheeseFoam = new Ingredient { Name = "Kem Phô Mai Cheese Foam", Unit = "g", CurrentStock = 8000, MinAlertThreshold = 1000, UnitCost = 110, CategoryType = "BEVERAGE" };

        // Food Ingredients
        var ingFishBall = new Ingredient { Name = "Cá Viên Thượng Hạng", Unit = "piece", CurrentStock = 1200, MinAlertThreshold = 200, UnitCost = 800, CategoryType = "FOOD" };
        var ingBeefBall = new Ingredient { Name = "Bò Viên Gân Giòn", Unit = "piece", CurrentStock = 900, MinAlertThreshold = 150, UnitCost = 1200, CategoryType = "FOOD" };
        var ingChickenFeet = new Ingredient { Name = "Chân Gà Rút Xương Tươi", Unit = "g", CurrentStock = 20000, MinAlertThreshold = 3000, UnitCost = 110, CategoryType = "FOOD" };
        var ingPotato = new Ingredient { Name = "Khoai Tây Sợi Nhập Khẩu", Unit = "g", CurrentStock = 25000, MinAlertThreshold = 4000, UnitCost = 65, CategoryType = "FOOD" };
        var ingCheeseStick = new Ingredient { Name = "Phô Mai Que Mozzarella", Unit = "piece", CurrentStock = 450, MinAlertThreshold = 80, UnitCost = 4500, CategoryType = "FOOD" };
        var ingRicePaper = new Ingredient { Name = "Bánh Tráng Phơi Sương Tây Ninh", Unit = "g", CurrentStock = 10000, MinAlertThreshold = 2000, UnitCost = 40, CategoryType = "FOOD" };
        var ingQuailEgg = new Ingredient { Name = "Trứng Cút Tươi", Unit = "piece", CurrentStock = 1500, MinAlertThreshold = 300, UnitCost = 1000, CategoryType = "FOOD" };
        var ingNoodles = new Ingredient { Name = "Mì Sợi Hàn Quốc Nongshim", Unit = "portion", CurrentStock = 300, MinAlertThreshold = 50, UnitCost = 9000, CategoryType = "FOOD" };
        var ingBeefSlice = new Ingredient { Name = "Bò Mỹ Ba Chỉ Cuộn", Unit = "g", CurrentStock = 15000, MinAlertThreshold = 2500, UnitCost = 250, CategoryType = "FOOD" };
        var ingRice = new Ingredient { Name = "Cơm Nguội Chiên Hạt Tơi", Unit = "portion", CurrentStock = 200, MinAlertThreshold = 40, UnitCost = 5000, CategoryType = "FOOD" };
        var ingSausage = new Ingredient { Name = "Xúc Xích Đức Xông Khói", Unit = "piece", CurrentStock = 500, MinAlertThreshold = 100, UnitCost = 4000, CategoryType = "FOOD" };
        var ingThaiSauce = new Ingredient { Name = "Nước Sốt Thái Chua Cay Đặc Chế", Unit = "ml", CurrentStock = 10000, MinAlertThreshold = 1500, UnitCost = 80, CategoryType = "FOOD" };

        context.Ingredients.AddRange(
            ingBlackTea, ingGreenTea, ingOolongTea, ingFreshMilk, ingSugarSyrup, ingPearl, ingWhitePearl, ingCheeseFoam,
            ingFishBall, ingBeefBall, ingChickenFeet, ingPotato, ingCheeseStick, ingRicePaper,
            ingQuailEgg, ingNoodles, ingBeefSlice, ingRice, ingSausage, ingThaiSauce
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 4. TOPPINGS (DRINKS & FOOD: 20 DRINK TOPPINGS + FOOD TOPPINGS)
        // ==========================================
        var topPearl = new Topping { IngredientId = ingPearl.Id, Name = "Trân châu hoàng kim", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topWhitePearl = new Topping { IngredientId = ingPearl.Id, Name = "Trân châu trắng", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topBlackPearl = new Topping { IngredientId = ingPearl.Id, Name = "Trân châu đen", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topBrownSugarPearl = new Topping { IngredientId = ingPearl.Id, Name = "Trân châu đường đen", Price = 10000, PortionAmount = 50, IsAvailable = true };
        var topMiniPearl = new Topping { IngredientId = ingPearl.Id, Name = "Trân châu mini", Price = 8000, PortionAmount = 40, IsAvailable = true };
        var topEggPudding = new Topping { IngredientId = ingPearl.Id, Name = "Pudding trứng", Price = 10000, PortionAmount = 60, IsAvailable = true };
        var topChocoPudding = new Topping { IngredientId = ingPearl.Id, Name = "Pudding socola", Price = 10000, PortionAmount = 60, IsAvailable = true };
        var topFruitJelly = new Topping { IngredientId = ingPearl.Id, Name = "Thạch trái cây", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCoconutJelly = new Topping { IngredientId = ingPearl.Id, Name = "Thạch dừa", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCoffeeJelly = new Topping { IngredientId = ingPearl.Id, Name = "Thạch cà phê", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topAloeJelly = new Topping { IngredientId = ingPearl.Id, Name = "Thạch nha đam", Price = 8000, PortionAmount = 50, IsAvailable = true };
        var topCheeseJelly = new Topping { IngredientId = ingPearl.Id, Name = "Thạch phô mai", Price = 12000, PortionAmount = 40, IsAvailable = true };
        var topFlan = new Topping { IngredientId = ingPearl.Id, Name = "Flan", Price = 12000, PortionAmount = 70, IsAvailable = true };
        var topRedBean = new Topping { IngredientId = ingPearl.Id, Name = "Đậu đỏ", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCrystalBobas = new Topping { IngredientId = ingPearl.Id, Name = "Hạt thủy tinh", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topCheeseFoam = new Topping { IngredientId = ingPearl.Id, Name = "Kem cheese", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topEggCream = new Topping { IngredientId = ingPearl.Id, Name = "Kem trứng", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topSaltCream = new Topping { IngredientId = ingPearl.Id, Name = "Kem muối", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topOreoCrushed = new Topping { IngredientId = ingPearl.Id, Name = "Oreo nghiền", Price = 8000, PortionAmount = 30, IsAvailable = true };
        var topGrassJelly = new Topping { IngredientId = ingPearl.Id, Name = "Sương sáo", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topExtraCheese = new Topping { IngredientId = ingCheeseStick.Id, Name = "Thêm Phô Mai Kéo Sợi (Món Ăn)", Price = 12000, PortionAmount = 40, IsAvailable = true };
        var topExtraEgg = new Topping { IngredientId = ingQuailEgg.Id, Name = "Thêm Trứng Ốp La Lòng Đào", Price = 8000, PortionAmount = 1, IsAvailable = true };
        var topExtraSausage = new Topping { IngredientId = ingSausage.Id, Name = "Thêm Xúc Xích Đức Chiên", Price = 12000, PortionAmount = 1, IsAvailable = true };

        context.Toppings.AddRange(topPearl, topWhitePearl, topBlackPearl, topBrownSugarPearl, topMiniPearl, topEggPudding, topChocoPudding, topFruitJelly, topCoconutJelly, topCoffeeJelly, topAloeJelly, topCheeseJelly, topFlan, topRedBean, topCrystalBobas, topCheeseFoam, topEggCream, topSaltCream, topOreoCrushed, topGrassJelly, topExtraCheese, topExtraEgg, topExtraSausage);
        await context.SaveChangesAsync();

        // ==========================================
        // 5. PRODUCTS (DRINKS + 10 FOOD CATEGORIES + COMBOS)
        // ==========================================
        var products = new List<Product>();

        // --- 5.1 Complete 163 Drink Products ---
        // --- 5.1.1 Trà Sữa Truyền Thống ---
        var p1 = new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa truyền thống",
            Slug = "tra-sua-truyen-thong",
            ProductType = "DRINK",
            Description = "Cốt hồng trà hảo hạng hòa quyện sữa tươi thanh trùng chuẩn vị nguyên bản.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        };
        products.Add(p1);
        var p2 = new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa trân châu đen",
            Slug = "tra-sua-tran-chau-den",
            ProductType = "DRINK",
            Description = "Trà sữa truyền thống đậm vị kết hợp trân châu đen dai giòn sần sật nấu mật mía.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        };
        products.Add(p2);
        var p3 = new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa trân châu trắng",
            Slug = "tra-sua-tran-chau-trang",
            ProductType = "DRINK",
            Description = "Trà sữa thơm béo kèm trân châu 3Q trắng ngọc trai giòn tan thanh nhiệt.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        };
        products.Add(p3);
        var p4 = new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa trân châu hoàng kim",
            Slug = "tra-sua-tran-chau-hoang-kim",
            ProductType = "DRINK",
            Description = "Trân châu hoàng kim nấu đường nâu sánh óng, thơm dẻo đặc trưng của MilkyVerse.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        };
        products.Add(p4);
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa đường đen",
            Slug = "tra-sua-duong-den",
            ProductType = "DRINK",
            Description = "Vị ngọt dịu thơm nồng của mật mía đường đen cô đặc thủ công.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa trân châu đường đen",
            Slug = "tra-sua-tran-chau-duong-den",
            ProductType = "DRINK",
            Description = "Sự kết hợp kinh điển giữa sữa trà đậm đà và dòng trân châu đường đen nóng dẻo.",
            BasePrice = 50000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa khoai môn",
            Slug = "tra-sua-khoai-mon",
            ProductType = "DRINK",
            Description = "Vị khoai môn bùi béo tự nhiên, màu tím pastel ngọt ngào quyến rũ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa matcha",
            Slug = "tra-sua-matcha",
            ProductType = "DRINK",
            Description = "Bột trà xanh Uji Nhật Bản nguyên chất thơm thanh mát, cân bằng vị chát nhẹ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa socola",
            Slug = "tra-sua-socola",
            ProductType = "DRINK",
            Description = "Socola đen nguyên chất đắng nhẹ hòa quyện cốt sữa ngọt ngào sánh mịn.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa cacao",
            Slug = "tra-sua-cacao",
            ProductType = "DRINK",
            Description = "Cacao Bến Tre nguyên chất thơm lừng nồng nàn, đậm đà vị truyền thống.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa dâu",
            Slug = "tra-sua-dau",
            ProductType = "DRINK",
            Description = "Hương dâu tây Đà Lạt tươi mới hòa tan trong cốt trà sữa thơm mát.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa caramel",
            Slug = "tra-sua-caramel",
            ProductType = "DRINK",
            Description = "Sốt caramel nấu thủ công thơm ngậy mùi bơ đường cháy cuốn hút.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa bạc hà",
            Slug = "tra-sua-bac-ha",
            ProductType = "DRINK",
            Description = "Vị the mát sảng khoái của lá bạc hà bừng tỉnh mọi giác quan.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa hạnh nhân",
            Slug = "tra-sua-hanh-nhan",
            ProductType = "DRINK",
            Description = "Hạnh nhân nướng thơm bùi đặc trưng phong cách phương Tây.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa Thái xanh",
            Slug = "tra-sua-thai-xanh",
            ProductType = "DRINK",
            Description = "Lá trà Thái xanh thơm mát, màu xanh ngọc bích bắt mắt giải nhiệt sảng khoái.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa Thái đỏ",
            Slug = "tra-sua-thai-do",
            ProductType = "DRINK",
            Description = "Trà Thái đỏ truyền thống thơm nồng thảo mộc, vị ngọt béo đậm sâu.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa Oreo",
            Slug = "tra-sua-oreo",
            ProductType = "DRINK",
            Description = "Trà sữa béo ngậy rắc vụn bánh quy Oreo giòn rụm thơm ngon khó cưỡng.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa tiramisu",
            Slug = "tra-sua-tiramisu",
            ProductType = "DRINK",
            Description = "Hương vị bánh Tiramisu trứ danh nước Ý hòa quyện lớp kem mascarpone béo ngậy.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa kem cheese",
            Slug = "tra-sua-kem-cheese",
            ProductType = "DRINK",
            Description = "Lớp cheese foam bồng bềnh béo mặn phủ trên nền trà sữa truyền thống.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa kem trứng",
            Slug = "tra-sua-kem-trung",
            ProductType = "DRINK",
            Description = "Kem trứng vàng óng nướng khò mặt thơm lừng béo ngậy ngọt ngào.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa kem muối",
            Slug = "tra-sua-kem-muoi",
            ProductType = "DRINK",
            Description = "Lớp kem muối biển sánh mịn đậm vị mặn ngọt hài hòa độc đáo.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa pudding",
            Slug = "tra-sua-pudding",
            ProductType = "DRINK",
            Description = "Pudding trứng tươi mềm mịn tan ngay đầu lưỡi kết hợp trà sữa êm dịu.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa flan",
            Slug = "tra-sua-flan",
            ProductType = "DRINK",
            Description = "Bánh flan caramen béo ngậy mềm mượt nguyên chiếc thả trong ly trà sữa.",
            BasePrice = 50000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa thạch trái cây",
            Slug = "tra-sua-thach-trai-cay",
            ProductType = "DRINK",
            Description = "Thạch trái cây cầu vồng giòn dai thanh mát giải nhiệt mùa hè.",
            BasePrice = 46000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catClassic.Id,
            Name = "Trà sữa nhiều topping",
            Slug = "tra-sua-nhieu-topping",
            ProductType = "DRINK",
            Description = "Bữa tiệc topping thịnh soạn: trân châu đen, thạch 3Q, pudding trứng và sương sáo.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.2 Trà Trái Cây ---
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà đào cam sả",
            Slug = "tra-dao-cam-sa",
            ProductType = "DRINK",
            Description = "Thức uống quốc dân: Đào miếng giòn rụm, cam vàng mọng nước và hương sả ấm nồng.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà vải",
            Slug = "tra-vai",
            ProductType = "DRINK",
            Description = "Hương vải thiều ngọt ngào thanh khiết kết hợp nền lục trà lài thanh tao.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà chanh dây",
            Slug = "tra-chanh-day",
            ProductType = "DRINK",
            Description = "Vị chua ngọt thanh thoát của chanh dây tươi nguyên hạt bừng tỉnh năng lượng.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà dâu tây",
            Slug = "tra-dau-tay",
            ProductType = "DRINK",
            Description = "Dâu tây tươi ngào đường thơm phức, màu đỏ hồng rực rỡ bắt mắt.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà xoài",
            Slug = "tra-xoai",
            ProductType = "DRINK",
            Description = "Xoài cát chín mọng ngọt thơm nhiệt đới hòa cùng trà xanh thanh mát.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà tắc",
            Slug = "tra-tac",
            ProductType = "DRINK",
            Description = "Trà tắc truyền thống chua chua ngọt ngọt, giải khát tức thì ngày nắng nóng.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà cam chanh",
            Slug = "tra-cam-chanh",
            ProductType = "DRINK",
            Description = "Sự kết hợp vitamin C dồi dào từ cam vàng và chanh tươi mát lành.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà dưa hấu",
            Slug = "tra-dua-hau",
            ProductType = "DRINK",
            Description = "Nước ép dưa hấu đỏ mọng ngọt mát tự nhiên pha trên nền lục trà.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà ổi hồng",
            Slug = "tra-oi-hong",
            ProductType = "DRINK",
            Description = "Ổi hồng xá lị thơm lừng hương hoa, vị ngọt thanh lưu luyến khó quên.",
            BasePrice = 46000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà dứa",
            Slug = "tra-dua",
            ProductType = "DRINK",
            Description = "Dứa nhiệt đới thơm ngọt dịu kết hợp trà xanh mát rượi sảng khoái.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà kiwi",
            Slug = "tra-kiwi",
            ProductType = "DRINK",
            Description = "Kiwi xanh nhập khẩu mọng nước, vị chua thanh giàu dưỡng chất và chất xơ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà táo xanh",
            Slug = "tra-tao-xanh",
            ProductType = "DRINK",
            Description = "Táo xanh giòn ngọt thanh tao, hương vị dịu nhẹ êm ái.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà nho",
            Slug = "tra-nho",
            ProductType = "DRINK",
            Description = "Nho tím mọng nước, vị ngọt dịu và màu sắc bắt mắt thanh lịch.",
            BasePrice = 46000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà dâu tằm",
            Slug = "tra-dau-tam",
            ProductType = "DRINK",
            Description = "Dâu tằm Đà Lạt chua ngọt đậm đà, giàu chất chống oxy hóa tự nhiên.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà chanh mật ong",
            Slug = "tra-chanh-mat-ong-trai-cay",
            ProductType = "DRINK",
            Description = "Mật ong hoa rừng nguyên chất kết hợp nước cốt chanh tươi ấm áp dịu cổ họng.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà chanh dây cam",
            Slug = "tra-chanh-day-cam",
            ProductType = "DRINK",
            Description = "Bản hòa tấu cam vàng tươi và chanh dây chua ngọt tươi mát.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà nhiệt đới thập cẩm",
            Slug = "tra-nhiet-doi-thap-cam",
            ProductType = "DRINK",
            Description = "Ngập tràn các lát trái cây tươi: cam, dưa hấu, táo, dâu tây và chanh dây.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà trái cây nhiệt đới",
            Slug = "tra-trai-cay-nhiet-doi",
            ProductType = "DRINK",
            Description = "Trái cây tuyển chọn tươi mới mỗi ngày trên nền trà ô long thanh nhã.",
            BasePrice = 50000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà mãng cầu",
            Slug = "tra-mang-cau",
            ProductType = "DRINK",
            Description = "Mãng cầu xiêm chua ngọt dẻo thơm, món hot trend tươi mát khó chối từ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà mận",
            Slug = "tra-man",
            ProductType = "DRINK",
            Description = "Mận Hậu ngâm chua ngọt đậm đà phong vị núi rừng Tây Bắc.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "Trà dưa lưới",
            Slug = "tra-dua-luoi",
            ProductType = "DRINK",
            Description = "Dưa lưới hoàng kim thơm dịu, ngọt lành thanh mát giải nhiệt ngày hè.",
            BasePrice = 46000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.3 Trà Nguyên Chất & Trà Pha Vị ---
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà xanh",
            Slug = "tra-xanh-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lục trà thái nguyên xanh trong thanh khiết, tiền chát dịu hậu ngọt sâu.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà đen",
            Slug = "tra-den-nguyen-chat",
            ProductType = "DRINK",
            Description = "Hồng trà ủ men toàn phần màu hổ phách đậm đà hương hoa cỏ khô.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà ô long",
            Slug = "tra-o-long-nguyen-chat",
            ProductType = "DRINK",
            Description = "Trà ô long cao nguyên thơm hương hoa mộc, vị trà đằm thắm tao nhã.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà lài",
            Slug = "tra-lai-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lục trà ướp hoa nhài tự nhiên thơm ngát thanh tao thư giãn tinh thần.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Hồng trà",
            Slug = "hong-tra-nguyen-chat",
            ProductType = "DRINK",
            Description = "Cốt hồng trà nguyên bản đậm vị thích hợp thưởng thức mộc hoặc thêm đá.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Lục trà",
            Slug = "luc-tra-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lục trà non thơm hương cốm, vị thanh mát dễ chịu.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà nhài mật ong",
            Slug = "tra-nhai-mat-ong",
            ProductType = "DRINK",
            Description = "Hương hoa lài quyện mật ong rừng ngọt thanh tao nhã.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà chanh",
            Slug = "tra-chanh-pho-co",
            ProductType = "DRINK",
            Description = "Trà chanh phố cổ truyền thống đậm đà chua ngọt giải khát dân dã.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà chanh mật ong",
            Slug = "tra-chanh-mat-ong-pha-vi",
            ProductType = "DRINK",
            Description = "Nước cốt chanh tươi hòa mật ong hoa cà phê thanh khiết.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà gừng mật ong",
            Slug = "tra-gung-mat-ong",
            ProductType = "DRINK",
            Description = "Gừng tươi đập dập thơm cay ấm nồng kết hợp mật ong tốt cho sức khỏe.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà quất mật ong",
            Slug = "tra-quat-mat-ong",
            ProductType = "DRINK",
            Description = "Quất tươi mọng nước thơm tinh dầu hòa cùng mật ong dịu dàng.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà vải lài",
            Slug = "tra-vai-lai",
            ProductType = "DRINK",
            Description = "Lục trà lài êm dịu kết hợp hương vải thiều ngọt ngào thanh lịch.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà ô long đào",
            Slug = "tra-o-long-dao",
            ProductType = "DRINK",
            Description = "Trà ô long nướng thơm hương kết hợp đào tươi mọng nước.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà ô long dâu",
            Slug = "tra-o-long-dau",
            ProductType = "DRINK",
            Description = "Sự gặp gỡ giữa trà ô long đằm thắm và dâu tây ngọt ngào.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà xanh cam",
            Slug = "tra-xanh-cam",
            ProductType = "DRINK",
            Description = "Lục trà thanh mát phối tép cam sành tươi nguyên chất.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà lài tắc",
            Slug = "tra-lai-tac",
            ProductType = "DRINK",
            Description = "Hương thơm hoa lài điểm xuyết vị chua thanh sảng khoái của tắc tươi.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "Trà atiso đỏ",
            Slug = "tra-atiso-do",
            ProductType = "DRINK",
            Description = "Trà hoa Hibiscus màu đỏ ruby rực rỡ, vị chua thanh tự nhiên đẹp da.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.4 Trà Macchiato & Kem Cheese ---
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà xanh macchiato",
            Slug = "tra-xanh-macchiato",
            ProductType = "DRINK",
            Description = "Lục trà thanh mát phủ lớp kem macchiato trắng muốt sánh mịn như tuyết.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà ô long kem cheese",
            Slug = "tra-o-long-kem-cheese",
            ProductType = "DRINK",
            Description = "Trà ô long thơm đậm vị khói kết hợp cheese foam phô mai béo mặn tuyệt đỉnh.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Hồng trà kem muối",
            Slug = "hong-tra-kem-muoi",
            ProductType = "DRINK",
            Description = "Hồng trà Ceylon đậm đà nâng niu bởi lớp bọt kem muối biển mặn mà quyến rũ.",
            BasePrice = 50000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà lài kem cheese",
            Slug = "tra-lai-kem-cheese",
            ProductType = "DRINK",
            Description = "Hoa lài thơm ngát dịu dàng hòa trong lớp kem phô mai béo ngậy mượt mà.",
            BasePrice = 50000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà đào kem cheese",
            Slug = "tra-dao-kem-cheese",
            ProductType = "DRINK",
            Description = "Hương đào ngọt thơm thanh thoát kết hợp kem phô mai thơm ngậy ngất ngây.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà dâu kem cheese",
            Slug = "tra-dau-kem-cheese",
            ProductType = "DRINK",
            Description = "Vị chua ngọt của dâu tây đỏ mọng hòa quyện lớp kem béo mặn đỉnh cao.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà xoài kem cheese",
            Slug = "tra-xoai-kem-cheese",
            ProductType = "DRINK",
            Description = "Xoài chín vàng ươm nhiệt đới cùng kem cheese béo mặn bồng bềnh.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà sữa macchiato",
            Slug = "tra-sua-macchiato",
            ProductType = "DRINK",
            Description = "Trà sữa truyền thống nhân đôi độ béo ngậy với lớp kem macchiato đặc chế.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà đen kem trứng",
            Slug = "tra-den-kem-trung",
            ProductType = "DRINK",
            Description = "Nền trà đen đậm vị nâng đỡ lớp kem trứng vàng nướng cháy thơm phức.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "Trà ô long kem muối",
            Slug = "tra-o-long-kem-muoi",
            ProductType = "DRINK",
            Description = "Ô long cao nguyên thượng hạng hòa quyện kem muối biển tinh khiết.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.5 Sữa Tươi & Đồ Uống Sữa ---
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi trân châu đường đen",
            Slug = "sua-tuoi-tran-chau-duong-den",
            ProductType = "DRINK",
            Description = "Món 'hot trend' vượt thời gian: Sữa tươi thanh trùng lạnh cùng trân châu đường đen dẻo nóng.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi đường nâu",
            Slug = "sua-tuoi-duong-nau",
            ProductType = "DRINK",
            Description = "Vân đường nâu hổ phách quệt thành ly ôm lấy dòng sữa tươi thanh mát béo ngậy.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi matcha",
            Slug = "sua-tuoi-matcha",
            ProductType = "DRINK",
            Description = "Matcha Uji xanh mướt phân tầng hoàn hảo trên nền sữa tươi trắng ngần.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi socola",
            Slug = "sua-tuoi-socola",
            ProductType = "DRINK",
            Description = "Sốt socola đen sánh đậm hòa vào sữa tươi béo thơm ngọt lịm tim.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi dâu",
            Slug = "sua-tuoi-dau",
            ProductType = "DRINK",
            Description = "Mứt dâu tây tươi ngào đường sánh mịn kết hợp sữa tươi thanh trùng.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi khoai môn",
            Slug = "sua-tuoi-khoai-mon",
            ProductType = "DRINK",
            Description = "Khoai môn nghiền dẻo bùi béo tự nhiên hòa trong dòng sữa thơm ngon.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi kem cheese",
            Slug = "sua-tuoi-kem-cheese",
            ProductType = "DRINK",
            Description = "Sữa tươi mát lạnh phủ lớp kem phô mai béo mặn sánh mịn bồng bềnh.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi trân châu trắng",
            Slug = "sua-tuoi-tran-chau-trang",
            ProductType = "DRINK",
            Description = "Sữa tươi béo ngọt thanh tao cùng trân châu ngọc trai trắng giòn sần sật.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi cacao",
            Slug = "sua-tuoi-cacao",
            ProductType = "DRINK",
            Description = "Cacao nguyên chất hòa cùng sữa tươi tạo hương vị ngọt đắng quyến rũ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi caramel",
            Slug = "sua-tuoi-caramel",
            ProductType = "DRINK",
            Description = "Caramel nấu bơ đường thơm nồng hòa tan trong sữa tươi mát lành.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sữa tươi mật ong",
            Slug = "sua-tuoi-mat-ong",
            ProductType = "DRINK",
            Description = "Vị ngọt thanh khiết của mật ong hoa rừng nguyên chất và sữa tươi dinh dưỡng.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.6 Matcha & Đồ Uống Cacao ---
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha latte",
            Slug = "matcha-latte",
            ProductType = "DRINK",
            Description = "Matcha Uji cao cấp đánh bọt mịn màng cùng sữa tươi thanh trùng nóng/đá.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha latte trân châu",
            Slug = "matcha-latte-tran-chau",
            ProductType = "DRINK",
            Description = "Matcha latte thơm thanh kết hợp trân châu hoàng kim dẻo ngọt đậm đà.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha latte kem cheese",
            Slug = "matcha-latte-kem-cheese",
            ProductType = "DRINK",
            Description = "Vị chát nhẹ của matcha hòa quyện cùng lớp kem phô mai béo ngậy mặn mòi.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha latte đậu đỏ",
            Slug = "matcha-latte-dau-do",
            ProductType = "DRINK",
            Description = "Sự kết hợp chuẩn vị Nhật: Matcha thanh mát và hạt đậu đỏ bùi ngọt dẻo thơm.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha dâu",
            Slug = "matcha-dau",
            ProductType = "DRINK",
            Description = "Hai tầng màu xanh - đỏ bắt mắt, vị dâu tây ngọt ngào cân bằng vị chát matcha.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha dừa",
            Slug = "matcha-dua",
            ProductType = "DRINK",
            Description = "Matcha Nhật Bản thơm dịu kết hợp nước cốt dừa Bến Tre béo bùi thanh mát.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha đá xay",
            Slug = "matcha-da-xay-cacao",
            ProductType = "DRINK",
            Description = "Matcha xay nhuyễn cùng đá tuyết và sữa, phủ ngọn kem tươi whipping cream mịn màng.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Cacao sữa đá",
            Slug = "cacao-sua-da",
            ProductType = "DRINK",
            Description = "Cacao nguyên chất đậm đà sánh quyện cùng sữa đặc và đá mát lạnh.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Cacao nóng",
            Slug = "cacao-nong",
            ProductType = "DRINK",
            Description = "Tách cacao nóng ấm lòng, thơm lừng hương socola xua tan ngày lạnh.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Cacao kem cheese",
            Slug = "cacao-kem-cheese",
            ProductType = "DRINK",
            Description = "Cacao đậm đà sánh mịn phủ lớp kem béo mặn cheese foam khó cưỡng.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Cacao đá xay",
            Slug = "cacao-da-xay-matcha",
            ProductType = "DRINK",
            Description = "Cacao xay đá tuyết mát lạnh sảng khoái phủ sốt socola và kem whipping.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Socola sữa đá",
            Slug = "socola-sua-da",
            ProductType = "DRINK",
            Description = "Socola sữa ngọt ngào êm dịu, món khoái khẩu của tín đồ đồ ngọt.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Socola bạc hà",
            Slug = "socola-bac-ha",
            ProductType = "DRINK",
            Description = "Vị ngọt đắng của socola hòa cùng vị the mát sảng khoái của siro bạc hà.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.7 Frappe & Đồ Uống Đá Xay ---
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Matcha đá xay Frappe",
            Slug = "matcha-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Matcha Uji Nhật xay nhuyễn mịn cùng sữa và đá tuyết, phủ kem whipping béo ngậy.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Socola đá xay Frappe",
            Slug = "socola-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Socola đen thượng hạng xay cùng đá tuyết mát lạnh ngập tràn năng lượng.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Cacao đá xay Frappe",
            Slug = "cacao-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Hương cacao nồng nàn xay mịn sánh như kem tuyết mùa hè.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Cookies and cream đá xay",
            Slug = "cookies-and-cream-da-xay",
            ProductType = "DRINK",
            Description = "Bánh quy cookies giòn rụm xay cùng sữa vani và phủ vụn bánh giòn tan.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Oreo đá xay Frappe",
            Slug = "oreo-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Bánh Oreo đen kẹp kem xay nhuyễn cùng kem béo và sốt socola đậm đặc.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Dâu đá xay Frappe",
            Slug = "dau-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Dâu tây tươi chua ngọt xay tuyết mát lạnh, màu hồng phấn đáng yêu.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Xoài đá xay Frappe",
            Slug = "xoai-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Xoài cát chín vàng ươm xay mát lạnh cùng sữa chua và đá tuyết sảng khoái.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Việt quất đá xay Frappe",
            Slug = "viet-quat-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Việt quất Bắc Mỹ mọng nước xay đá tuyết giàu chất chống lão hóa.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Caramel đá xay Frappe",
            Slug = "caramel-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Sốt caramel nấu thơm bơ xay cùng đá sữa ngọt ngào êm ái.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Cà phê đá xay Frappe",
            Slug = "ca-phe-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Cà phê Robusta Đắk Lắk đậm đà xay đá tuyết sảng khoái tỉnh táo cả ngày.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Mocha đá xay Frappe",
            Slug = "mocha-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Bản hòa tấu giữa cà phê espresso và socola đen xay mịn phủ kem bông tuyết.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Frappuccino caramel",
            Slug = "frappuccino-caramel",
            ProductType = "DRINK",
            Description = "Cà phê frappuccino phong cách Ý đượm vị caramel béo ngậy và sốt kem vani.",
            BasePrice = 60000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Frappuccino socola",
            Slug = "frappuccino-socola",
            ProductType = "DRINK",
            Description = "Frappuccino socola thượng hạng rưới sốt fudge socola đậm đà.",
            BasePrice = 60000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.8 Nước Ép Trái Cây ---
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép cam",
            Slug = "nuoc-ep-cam",
            ProductType = "DRINK",
            Description = "Cam sành tươi vắt nguyên chất mọng nước dồi dào vitamin C tăng đề kháng.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép dưa hấu",
            Slug = "nuoc-ep-dua-hau",
            ProductType = "DRINK",
            Description = "Dưa hấu đỏ ngọt mát tự nhiên giải nhiệt tức thì trong ngày oi bức.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép dứa",
            Slug = "nuoc-ep-dua",
            ProductType = "DRINK",
            Description = "Dứa chín thơm vàng ươm chua ngọt tự nhiên hỗ trợ tiêu hóa và giữ dáng.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép táo",
            Slug = "nuoc-ep-tao",
            ProductType = "DRINK",
            Description = "Táo New Zealand ép nguyên chất ngọt thanh giòn mát tốt cho tim mạch.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép cà rốt",
            Slug = "nuoc-ep-ca-rot",
            ProductType = "DRINK",
            Description = "Cà rốt tươi giàu beta-carotene sáng mắt và đẹp da mịn màng.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép ổi",
            Slug = "nuoc-ep-oi",
            ProductType = "DRINK",
            Description = "Ổi ruột hồng tươi ngon giàu vitamin C gấp 4 lần cam tươi.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép dâu",
            Slug = "nuoc-ep-dau",
            ProductType = "DRINK",
            Description = "Dâu tây Đà Lạt ép nguyên quả màu đỏ thắm quyến rũ vị chua ngọt tao nhã.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép xoài",
            Slug = "nuoc-ep-xoai",
            ProductType = "DRINK",
            Description = "Xoài cát Hòa Lộc ép mịn ngọt đậm đà hương vị trái cây nhiệt đới.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép chanh dây",
            Slug = "nuoc-ep-chanh-day",
            ProductType = "DRINK",
            Description = "Chanh dây tươi lọc hạt chua ngọt thanh tao kích thích vị giác.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép cóc",
            Slug = "nuoc-ep-coc",
            ProductType = "DRINK",
            Description = "Cóc non chua giòn rụm ép nguyên chất điểm xuyết xíu muối ớt nhẹ.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép thơm cà rốt",
            Slug = "nuoc-ep-thom-ca-rot",
            ProductType = "DRINK",
            Description = "Sự kết hợp hoàn hảo giữa vị ngọt thanh cà rốt và chua nhẹ của thơm.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép cam cà rốt",
            Slug = "nuoc-ep-cam-ca-rot",
            ProductType = "DRINK",
            Description = "Bộ đôi cam vàng và cà rốt giàu vitamin bổ sung năng lượng tươi mới.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép táo dứa",
            Slug = "nuoc-ep-tao-dua",
            ProductType = "DRINK",
            Description = "Táo ngọt dịu quyện dứa thơm chua thanh tạo hương vị thanh lọc tuyệt vời.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép dưa hấu dứa",
            Slug = "nuoc-ep-dua-hau-dua",
            ProductType = "DRINK",
            Description = "Dưa hấu ngọt nước phối dứa nhiệt đới giải nhiệt sảng khoái.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "Nước ép trái cây hỗn hợp",
            Slug = "nuoc-ep-trai-cay-hon-hop",
            ProductType = "DRINK",
            Description = "Công thức detox tổng hợp cam, táo, cà rốt và dứa tươi thanh lọc cơ thể.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.9 Soda & Đồ Uống Có Gas ---
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda chanh",
            Slug = "soda-chanh",
            ProductType = "DRINK",
            Description = "Soda chanh tươi sủi bọt mát lạnh cổ điển giải nhiệt cực nhanh.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda việt quất",
            Slug = "soda-viet-quat",
            ProductType = "DRINK",
            Description = "Màu tím biếc huyền ảo, vị việt quất ngọt thơm hòa cùng bọt ga sảng khoái.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda dâu",
            Slug = "soda-dau",
            ProductType = "DRINK",
            Description = "Soda dâu tây đỏ tươi chua ngọt dịu dàng điểm xuyết lá bạc hà.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda đào",
            Slug = "soda-dao",
            ProductType = "DRINK",
            Description = "Hương đào thơm nức quyện cùng bọt ga sủi tăm kích thích vị giác.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda kiwi",
            Slug = "soda-kiwi",
            ProductType = "DRINK",
            Description = "Màu xanh ngọc bích từ kiwi tươi chua thanh mát rượi.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda chanh dây",
            Slug = "soda-chanh-day",
            ProductType = "DRINK",
            Description = "Chanh dây tươi chua ngọt hòa cùng soda bọt ga bừng tỉnh tức thì.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda bạc hà",
            Slug = "soda-bac-ha",
            ProductType = "DRINK",
            Description = "Hương bạc hà xanh mát the the lạnh buốt cuốn hút mọi ánh nhìn.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda cam",
            Slug = "soda-cam",
            ProductType = "DRINK",
            Description = "Cam vàng sủi bọt tràn đầy sức sống và vitamin C.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda táo xanh",
            Slug = "soda-tao-xanh",
            ProductType = "DRINK",
            Description = "Táo xanh giòn ngọt kết hợp soda ga tươi mát trẻ trung.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda nhiệt đới",
            Slug = "soda-nhiet-doi",
            ProductType = "DRINK",
            Description = "Hỗn hợp trái cây nhiệt đới thơm mát bùng nổ cùng bọt ga lấp lánh.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda xoài",
            Slug = "soda-xoai",
            ProductType = "DRINK",
            Description = "Xoài chín vàng ngọt ngào kết hợp bọt ga mát lạnh sảng khoái.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda vải",
            Slug = "soda-vai",
            ProductType = "DRINK",
            Description = "Hương vải ngọt dịu quý phái trong từng ngụm soda sủi tăm.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda dưa lưới",
            Slug = "soda-dua-luoi",
            ProductType = "DRINK",
            Description = "Dưa lưới thanh tao ngọt mát sảng khoái ngày hè.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda blue ocean",
            Slug = "soda-blue-ocean",
            ProductType = "DRINK",
            Description = "Sắc xanh đại dương sâu thẳm từ siro Blue Curacao cam đắng và soda sảng khoái.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.10 Nước Chanh & Đồ Uống Giải Khát ---
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh tươi",
            Slug = "chanh-tuoi-da-duong",
            ProductType = "DRINK",
            Description = "Nước chanh tươi đá đường truyền thống thanh mát bình dị.",
            BasePrice = 28000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh mật ong",
            Slug = "chanh-mat-ong-giai-khat",
            ProductType = "DRINK",
            Description = "Chanh tươi kết hợp mật ong rừng nguyên chất ngọt thanh ấm áp.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh dây",
            Slug = "chanh-day-da-duong",
            ProductType = "DRINK",
            Description = "Nước cốt chanh dây thơm lừng hạt giòn ngọt ngào giải khát.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Tắc mật ong",
            Slug = "tac-mat-ong-giai-khat",
            ProductType = "DRINK",
            Description = "Tắc thơm mùi tinh dầu quất hòa cùng mật ong hoa cà phê thanh ngọt.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Tắc xí muội",
            Slug = "tac-xi-muoi",
            ProductType = "DRINK",
            Description = "Vị mặn ngọt chua cay hài hòa của xí muội Bắc ngâm tắc giải cảm thanh giọng.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh muối",
            Slug = "chanh-muoi-lau-nam",
            ProductType = "DRINK",
            Description = "Chanh muối lâu năm ngâm mật đường, bù điện giải cực tốt sau khi vận động.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh sả",
            Slug = "chanh-sa-giai-nhiet",
            ProductType = "DRINK",
            Description = "Chanh tươi thơm lừng hương sả đập dập the mát thanh lọc cơ thể.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước mơ",
            Slug = "nuoc-mo-chua-huong",
            ProductType = "DRINK",
            Description = "Mơ ngâm chùa Hương chua ngọt đậm đà phong vị Hà thành xưa.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước sấu",
            Slug = "nuoc-sau-ngam-gung",
            ProductType = "DRINK",
            Description = "Quả sấu giòn sần sật ngâm gừng ớt chua cay ngọt dịu mát ruột.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước nha đam",
            Slug = "nuoc-nha-dam-duong-phen",
            ProductType = "DRINK",
            Description = "Thạch nha đam tươi giòn mát thanh nhiệt làm mát cơ thể tức thì.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước dừa tươi",
            Slug = "nuoc-dua-tuoi-ben-tre",
            ProductType = "DRINK",
            Description = "Dừa xiêm Bến Tre ngọt nước thanh khiết nguyên quả tự nhiên.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước dừa tắc",
            Slug = "nuoc-dua-tac-sai-gon",
            ProductType = "DRINK",
            Description = "Nước dừa xiêm ngọt mát thêm chút nước cốt tắc thơm dịu và cùi dừa non giòn.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Nước ép chanh dây mật ong",
            Slug = "nuoc-ep-chanh-day-mat-ong",
            ProductType = "DRINK",
            Description = "Sự hòa quyện sánh mịn giữa chanh dây và mật ong hoa rừng nguyên chất.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.11 Cà Phê & Đồ Uống Bổ Sung ---
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Cà phê đen",
            Slug = "ca-phe-den-da",
            ProductType = "DRINK",
            Description = "Cà phê phin truyền thống đậm đà vị đắng nguyên bản và hậu vị ngọt sâu.",
            BasePrice = 28000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Cà phê sữa",
            Slug = "ca-phe-sua-da-sai-gon",
            ProductType = "DRINK",
            Description = "Cà phê sữa đá Sài Gòn thơm nồng, đậm đà vị ngọt béo của sữa đặc hòa cùng cà phê đậm.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Bạc xỉu",
            Slug = "bac-xiu-da-ngot-ngao",
            ProductType = "DRINK",
            Description = "Nhiều sữa ít cà phê, béo ngậy ngọt ngào thích hợp cho người thích cà phê nhẹ nhàng.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Cà phê muối",
            Slug = "ca-phe-muoi-hue",
            ProductType = "DRINK",
            Description = "Cà phê đậm vị nâng đỡ lớp kem muối béo ngậy mặn mà phong cách xứ Huế.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Latte",
            Slug = "latte-art-y",
            ProductType = "DRINK",
            Description = "Cà phê espresso Ý hòa cùng sữa nóng đánh bọt mịn màng nghệ thuật latte art.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Cappuccino",
            Slug = "cappuccino-y",
            ProductType = "DRINK",
            Description = "Lớp bọt sữa dày xốp phủ bột quế cacao thơm nồng trên nền cà phê espresso.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Americano",
            Slug = "americano-da",
            ProductType = "DRINK",
            Description = "Espresso pha loãng với nước thanh khiết, thơm nồng dịu êm không gắt.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Mocha",
            Slug = "mocha-socola-nong-da",
            ProductType = "DRINK",
            Description = "Sự kết hợp ngọt ngào giữa espresso, socola đen và sữa nóng sánh mịn.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Caramel macchiato",
            Slug = "caramel-macchiato-coffee",
            ProductType = "DRINK",
            Description = "Sữa tươi đánh bọt, cà phê espresso và những vệt sốt caramel óng ả ngọt ngào.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Espresso",
            Slug = "espresso-shot",
            ProductType = "DRINK",
            Description = "Một shot cà phê đậm đặc chiết xuất áp suất cao với lớp crema vàng dày mịn.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "Cà phê cốt dừa",
            Slug = "ca-phe-cot-dua-da-xay",
            ProductType = "DRINK",
            Description = "Cà phê đậm đà hòa cùng nước cốt dừa xay đá tuyết béo bùi thơm lừng.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });

        // --- 5.2 Category 2.1: Cá viên, bò viên & các loại viên chiên ---
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Cá Viên Chiên Thập Cẩm Mẹt Lớn",
            Slug = "ca-vien-chien-thap-cam-met-lon",
            ProductType = "FOOD",
            Description = "Mẹt viên chiên ngập tràn cá viên, bò viên, tôm viên, hồ lô, xúc xích và chả giò mini chiên ráo dầu vàng óng, ăn kèm dưa leo giòn và tương đen xào đặc biệt.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương đen & Tương ớt xào tỏi",
            PortionDescription = "1 mẹt (khoảng 25 viên đủ 2-3 người ăn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Cá Viên Chiên Truyền Thống",
            Slug = "ca-vien-chien-truyen-thong",
            ProductType = "FOOD",
            Description = "Cá viên chiên giòn thơm, dai ngọt tự nhiên, phồng tròn chấm tương ớt hoặc tương đen đậm vị.",
            BasePrice = 25000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt & tương đen",
            PortionDescription = "1 phần (10 viên)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Bò Viên Chiên Gân Giòn",
            Slug = "bo-vien-chien-gan-gion",
            ProductType = "FOOD",
            Description = "Bò viên gân giòn sần sật, thơm ngậy mùi tiêu đen và bò tươi chiên nóng sốt.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương đen sa tế",
            PortionDescription = "1 phần (8 viên)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Tôm Viên Chiên Giòn Rụm",
            Slug = "tom-vien-chien-gion-rum",
            ProductType = "FOOD",
            Description = "Thịt tôm tươi xay nhuyễn tạo nên viên tôm thơm ngậy vị ngọt hải sản.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sốt bơ trứng & tương ớt",
            PortionDescription = "1 phần (8 viên)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Mực Viên Chiên Nóng",
            Slug = "muc-vien-chien-nong",
            ProductType = "FOOD",
            Description = "Mực viên dai ngọt đậm đà vị biển, chiên vàng giòn ráo dầu.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt cay nồng",
            PortionDescription = "1 phần (8 viên)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Hồ Lô Nướng / Chiên Giòn",
            Slug = "ho-lo-nuong-chien-gion",
            ProductType = "FOOD",
            Description = "Hồ lô thịt tươi ngọt thơm mỡ màng, chiên giòn vỏ bên ngoài mọng nước bên trong.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương đen & tương ớt xào sa tế",
            PortionDescription = "1 phần (8 viên hồ lô)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Phô Mai Viên Chiên Giòn Tan",
            Slug = "pho-mai-vien-chien-gion-tan",
            ProductType = "FOOD",
            Description = "Lớp vỏ ngoài giòn rụm bao bọc nhân phô mai béo ngậy chảy tràn khi cắn.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sốt bơ tỏi & tương ớt ngọt",
            PortionDescription = "1 phần (6 viên)"
        });

        // --- 5.3 Category 2.2: Gà và các món chân gà ---
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "Chân Gà Rút Xương Sốt Thái Cay Giòn",
            Slug = "chan-ga-rut-xuong-sot-thai-cay-gion",
            ProductType = "FOOD",
            Description = "Chân gà tươi rút xương giòn sần sật thấm đẫm sốt Thái cay nồng đậm vị, xoài xanh non, cóc bao tử và sả tắc ngập tràn hương thơm quyến rũ.",
            BasePrice = 69000,
            ImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            AllowSauceCustomization = true,
            DefaultSauce = "Nước sốt Thái đặc chế chua cay",
            PortionDescription = "1 phần (khoảng 350g kèm xoài cóc bao tử)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "Chân Gà Ngâm Sả Tắc Truyền Thống",
            Slug = "chan-ga-ngam-sa-tac-truyen-thong",
            ProductType = "FOOD",
            Description = "Chân gà ngâm sả tắc giòn dai sần sật, hương thơm sả cây, lá chanh và vị chua thanh mát của tắc quả.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Muối tiêu chanh ớt xanh",
            PortionDescription = "1 phần (khoảng 350g)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "Cánh Gà Chiên Nước Mắm Tỏi Ớt",
            Slug = "canh-ga-chien-nuoc-mam-toi-ot",
            ProductType = "FOOD",
            Description = "Cánh gà chiên giòn tan phủ đều lớp nước mắm tỏi ớt kẹo ngọt thơm nức mũi, đậm đà khó cưỡng.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1567620832903-9fc6debc209f?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            DefaultSauce = "Sốt mắm tỏi kẹo",
            PortionDescription = "1 phần (3 cánh lớn)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "Gà Lắc Phô Mai Béo Ngậy",
            Slug = "ga-lac-pho-mai-beo-ngay",
            ProductType = "FOOD",
            Description = "Thịt gà ức phi lê cắt miếng chiên vàng rụm lắc ngập bột phô mai béo ngọt thơm phức.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt chua ngọt & Sốt mayonnaise",
            PortionDescription = "1 phần (200g thịt gà viên)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "Gà Popcorn Chiên Giòn",
            Slug = "ga-popcorn-chien-gion",
            ProductType = "FOOD",
            Description = "Gà viên popcorn giòn rụm cắn vui tai, bên trong mọng nước mềm thơm.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương cà & tương ớt",
            PortionDescription = "1 phần (khoảng 180g)"
        });

        // --- 5.4 Category 2.3: Khoai tây & khoai lang ---
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai Tây Lắc Phô Mai Đặc Biệt",
            Slug = "khoai-tay-lac-pho-mai-dac-biet",
            ProductType = "FOOD",
            Description = "Khoai tây sợi vàng ươm nhập khẩu chiên giòn tan, phủ lớp bột phô mai Hàn Quốc thơm lừng ngậy béo khó quên.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt & Mayonnaise",
            PortionDescription = "1 phần tiêu chuẩn (250g)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai Tây Lắc Rong Biển Thơm Giòn",
            Slug = "khoai-tay-lac-rong-bien-thom-gion",
            ProductType = "FOOD",
            Description = "Sự kết hợp giữa vị mằn mặn của rong biển nướng và độ giòn rụm của khoai tây sợi tươi.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt cay",
            PortionDescription = "1 phần (250g)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai Lang Kén Nước Cốt Dừa",
            Slug = "khoai-lang-ken-nuoc-cot-dua",
            ProductType = "FOOD",
            Description = "Khoai lang kén bùi ngọt, dẻo thơm thoang thoảng mùi nước cốt dừa và mè đen béo ngậy.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = false,
            DefaultSauce = "Tương ớt",
            PortionDescription = "1 phần (12 kén)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai Tây Chiên Sốt Phô Mai Tan Chảy",
            Slug = "khoai-tay-chien-sot-pho-mai-tan-chay",
            ProductType = "FOOD",
            Description = "Đĩa khoai tây chiên nóng hổi rưới ngập sốt cheddar cheese tan chảy béo mặn thơm ngất ngây.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sốt phô mai cheddar tan chảy",
            PortionDescription = "1 phần lớn"
        });

        // --- 5.5 Category 2.4: Phô mai, xúc xích & đồ chiên khác ---
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "Phô Mai Que Kéo Sợi Khổng Lồ",
            Slug = "pho-mai-que-keo-soi-khong-lo",
            ProductType = "FOOD",
            Description = "Phô mai que Mozzarella hảo hạng chiên xù vàng ruộm, kéo sợi dài miên man béo thơm nức mũi.",
            BasePrice = 36000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương cà & Mayonnaise chua ngọt",
            PortionDescription = "1 phần (4 que lớn)"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "Xúc Xích Lốc Xoáy Chiên Giòn",
            Slug = "xuc-xich-loc-xoay-chien-gion",
            ProductType = "FOOD",
            Description = "Xúc xích Đức xông khói cắt lốc xoáy tẩm bột chiên giòn rụm bên ngoài mọng nước bên trong.",
            BasePrice = 28000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt & tương cà",
            PortionDescription = "1 cây lớn"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "Nem Chua Rán Hà Nội Chuẩn Vị",
            Slug = "nem-chua-ran-ha-noi-chuan-vi",
            ProductType = "FOOD",
            Description = "Nem chua rán Hà Nội lăn bột chiên xù giòn rụm bên ngoài dẻo dính bên trong, ăn kèm dưa chuột tươi và tương ớt cay ngọt.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt Hà Nội chua cay",
            PortionDescription = "1 phần (6 chiếc)"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "Há Cảo Tôm Thịt Chiên Giòn",
            Slug = "ha-cao-tom-thit-chien-gion",
            ProductType = "FOOD",
            Description = "Há cảo nhân tôm thịt đậm vị gói vỏ mỏng chiên vàng giòn rụm, chấm giấm đen hoặc tương ớt sa tế.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Nước tương giấm đỏ xào sa tế",
            PortionDescription = "1 phần (8 chiếc)"
        });

        // --- 5.6 Category 2.5: Bánh tráng & các món trộn ---
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "Bánh Tráng Trộn Milkyverse Đặc Biệt",
            Slug = "banh-trang-tron-milkyverse-dac-biet",
            ProductType = "FOOD",
            Description = "Bánh tráng phơi sương dẻo mềm, ngập tràn topping: bò khô miếng, khô gà lá chanh, tép sấy, trứng cút lòng đào, xoài chua thái sợi, rau răm và nước sốt bò sa tế thơm cay bí truyền.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            AllowSauceCustomization = true,
            DefaultSauce = "Nước sốt bò sa tế đặc biệt",
            PortionDescription = "1 phần lớn ngập topping"
        });
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "Bánh Tráng Nướng Đà Lạt Trứng Thịt Xúc Xích",
            Slug = "banh-trang-nuong-da-lat-trung-thit-xuc-xich",
            ProductType = "FOOD",
            Description = "'Pizza Việt Nam' nướng than hồng giòn rụm, phết trứng gà béo ngậy, hành hoa, ruốc thịt, xúc xích và sốt mayonnaise bơ trứng.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Mayonnaise & tương ớt nướng",
            PortionDescription = "1 chiếc bánh lớn"
        });
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "Bánh Tráng Cuốn Bơ Sốt Me Tắc",
            Slug = "banh-trang-cuon-bo-sot-me-tac",
            ProductType = "FOOD",
            Description = "Từng cuộn bánh tráng dẻo thơm cuốn trứng cút, khô bò, tép sấy, hành phi giòn tan ngập trong sốt bơ béo ngậy và sốt me chua cay.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            AllowSauceCustomization = true,
            DefaultSauce = "Sốt bơ trứng & sốt me cay",
            PortionDescription = "1 phần (8 cuốn cắt khúc)"
        });

        // --- 5.7 Category 2.6: Trứng chén & món trứng ---
        products.Add(new Product
        {
            CategoryId = catTrungChen.Id,
            Name = "Trứng Chén Nướng Phô Mai Xúc Xích Pate",
            Slug = "trung-chen-nuong-pho-mai-xuc-xich-pate",
            ProductType = "FOOD",
            Description = "Chén sứ nướng than hoa nóng bỏng, 4 quả trứng cút tươi phủ đầy phô mai Mozzarella kéo sợi, xúc xích xông khói, pate thơm lừng và mỡ hành béo ngậy.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Nước sốt chua ngọt & muối tiêu tắc",
            PortionDescription = "1 chén nướng nóng hổi"
        });
        products.Add(new Product
        {
            CategoryId = catTrungChen.Id,
            Name = "Trứng Cút Lộn Xào Me Chua Ngọt",
            Slug = "trung-cut-lon-xao-me-chua-ngot",
            ProductType = "FOOD",
            Description = "Trứng cút lộn bùi béo đảo trong nước sốt me kẹo chua ngọt sánh mịn, rắc thêm đậu phộng rang thơm nức và rau răm.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            DefaultSauce = "Sốt me chua ngọt sánh đặc",
            PortionDescription = "1 đĩa (15 quả)"
        });

        // --- 5.8 Category 2.7: Mì & món cay 7 cấp độ ---
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "Mì Cay Hàn Quốc 7 Cấp Độ Hải Sản Thập Cẩm",
            Slug = "mi-cay-han-quoc-7-cap-do-hai-san-thap-cam",
            ProductType = "FOOD",
            Description = "Thố đá sôi sục nghi ngút khói với mì sợi Hàn Quốc dai giòn, tôm sú tươi, mực khoanh giòn ngọt, bò Mỹ cuộn, nấm kim châm và bắp cải tím trong nước súp kim chi cay tê tái chuẩn 7 cấp độ.",
            BasePrice = 69000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            PortionDescription = "1 thố đá lớn đầy đủ tôm, mực, bò Mỹ, xúc xích"
        });
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "Mì Cay Bò Mỹ Cuộn Nấm Kim Châm",
            Slug = "mi-cay-bo-my-cuon-nam-kim-cham",
            ProductType = "FOOD",
            Description = "Nước dùng cay chua ngọt kim chi cay xé lưỡi hòa quyện cùng từng dải thịt bò Mỹ mềm tan và nấm kim châm giòn sần sật.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            PortionDescription = "1 thố đá bò Mỹ hảo hạng"
        });
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "Mì Trộn Xúc Xích Phô Mai Trứng Lòng Đào",
            Slug = "mi-tron-xuc-xich-pho-mai-trung-long-dao",
            ProductType = "FOOD",
            Description = "Mì trộn Indomie sốt cay đậm đà, phủ trứng lòng đào béo ngậy, xúc xích rán và phô mai mozzarella kéo sợi thơm phức.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            PortionDescription = "1 tô mì trộn lớn"
        });

        // --- 5.9 Category 2.8: Cơm & món no ---
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "Cơm Chiên Bò Kim Chi Trứng Ốp La",
            Slug = "com-chien-bo-kim-chi-trung-op-la",
            ProductType = "FOOD",
            Description = "Cơm chiên hạt tơi vàng óng đảo cùng thịt bò Mỹ thái mỏng và kim chi cải thảo chua cay giòn tan, kèm trứng ốp la lòng đào và canh rong biển thanh mát.",
            BasePrice = 59000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            PortionDescription = "1 đĩa cơm đầy đặn kèm súp rong biển"
        });
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "Cơm Chiên Hải Sản Hoàng Kim",
            Slug = "com-chien-hai-san-hoang-kim",
            ProductType = "FOOD",
            Description = "Hạt cơm được bao bọc lớp lòng đỏ trứng muối béo ngậy, chiên giòn cùng tôm tươi, mực giòn và đậu Hà Lan ngọt bùi.",
            BasePrice = 62000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "1 đĩa lớn"
        });
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "Kimbap Chiên Xù Giòn Rụm",
            Slug = "kimbap-chien-xu-gion-rum",
            ProductType = "FOOD",
            Description = "Cơm cuộn rong biển Hàn Quốc nhân thanh cua, trứng, xúc xích và dưa chuột lăn bột xù chiên vàng rụm chấm mayonnaise tương cà.",
            BasePrice = 39000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Mayonnaise & tương ớt tương cà",
            PortionDescription = "1 cuộn lớn (10 khoanh)"
        });

        // --- 5.10 Category 2.9: Nội tạng nướng & món nướng ---
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "Lòng Non Nướng Sa Tế Cay Giòn Than Hoa",
            Slug = "long-non-nuong-sa-te-cay-gion-than-hoa",
            ProductType = "FOOD",
            Description = "Lòng non tươi làm sạch kỹ càng, tẩm ướp sa tế cay cay đượm vị mật ong, nướng trên than hoa xì xèo vàng ươm giòn sần sật.",
            BasePrice = 68000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            AllowSauceCustomization = true,
            DefaultSauce = "Muối ớt xanh chua cay & mắm nêm",
            PortionDescription = "1 đĩa nướng nóng (300g kèm dưa leo, rau răm)"
        });
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "Dồi Sụn Nướng Than Hoa Giòn Rụm",
            Slug = "doi-sun-nuong-than-hoa-gion-rum",
            ProductType = "FOOD",
            Description = "Dồi sụn thịt heo thơm nức mùi húng quế và hành hoa, sụn giòn sần sật bên trong nướng vàng ươm lớp vỏ ngoài.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt cay & sốt me",
            PortionDescription = "1 phần (6 khoanh dài)"
        });
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "Sụn Gà Chiên Nước Mắm / Rang Muối Hồng Kông",
            Slug = "sun-ga-chien-nuoc-mam-rang-muoi-hong-kong",
            ProductType = "FOOD",
            Description = "Sụn ức gà giòn sần sật rang muối Hồng Kông thơm ngát mùi sả gừng lá chanh giòn rụm khó quên.",
            BasePrice = 59000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Muối tiêu chanh ớt",
            PortionDescription = "1 đĩa (khoảng 250g)"
        });

        // --- 5.11 Category 2.10: Ăn vặt bổ sung ---
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "Bắp Xào Bơ Tép Mỡ Hành Phô Mai",
            Slug = "bap-xao-bo-tep-mo-hanh-pho-mai",
            ProductType = "FOOD",
            Description = "Bắp mỹ ngọt giòn xào cùng bơ Tường An thơm lừng, tép sấy đậm vị, hành hoa tươi và rắc phô mai kéo sợi dẻo dai béo ngậy.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Tương ớt chua cay",
            PortionDescription = "1 phần lớn nóng hổi"
        });
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "Bánh Gạo Tokbokki Cay Ngọt Sốt Phô Mai",
            Slug = "banh-gao-tokbokki-cay-ngot-sot-pho-mai",
            ProductType = "FOOD",
            Description = "Bánh gạo dẻo mềm Hàn Quốc đun trong sốt Gochujang cay ngọt đậm đà, ngập tràn chả cá, xúc xích và phô mai béo ngậy chảy tràn.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            PortionDescription = "1 phần thố tokbokki đầy ắp"
        });
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "Khô Gà Lá Chanh Cay Xé Thơm Lừng",
            Slug = "kho-ga-la-chanh-cay-xe-thom-lung",
            ProductType = "FOOD",
            Description = "Từng sợi thịt gà xé dai ngọt đượm vị mắm tỏi ớt cay nồng, thơm phức hương lá chanh tươi sấy giòn.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Hũ 150g"
        });

        // --- 5.12 Combos (Food + Drink) ---
                var combo1 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà sữa và cá viên chiên",
            Slug = "combo-tra-sua-va-ca-vien-chien",
            ProductType = "COMBO",
            Description = "1 Ly Trà Sữa Truyền Thống trân châu đen size M + 1 Phần Cá Viên Chiên sốt tương đen sa tế nóng hổi giòn rụm.",
            BasePrice = 72000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà sữa và cá viên chiên tiết kiệm"
        };
        var combo2 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà sữa và khoai tây chiên",
            Slug = "combo-tra-sua-va-khoai-tay-chien",
            ProductType = "COMBO",
            Description = "1 Ly Trà Sữa Thái Xanh size M + 1 Phần Khoai Tây Chiên lắc phô mai giòn rụm thơm lừng.",
            BasePrice = 68000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà sữa và khoai tây chiên tiết kiệm"
        };
        var combo3 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà sữa và bánh tráng trộn",
            Slug = "combo-tra-sua-va-banh-trang-tron",
            ProductType = "COMBO",
            Description = "1 Ly Trà Sữa Ô Long Nướng size M + 1 Phần Bánh Tráng Trộn bò khô tép sấy sốt sa tế đặc biệt.",
            BasePrice = 69000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà sữa và bánh tráng trộn tiết kiệm"
        };
        var combo4 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà trái cây và chân gà sả tắc",
            Slug = "combo-tra-trai-cay-va-chan-ga-sa-tac",
            ProductType = "COMBO",
            Description = "1 Ly Trà Đào Cam Sả size L giải nhiệt + 1 Phần Chân Gà Sả Tắc giòn sần sật chua cay tê lưỡi.",
            BasePrice = 89000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà trái cây và chân gà sả tắc tiết kiệm"
        };
        var combo5 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà sữa và phô mai que",
            Slug = "combo-tra-sua-va-pho-mai-que",
            ProductType = "COMBO",
            Description = "1 Ly Trà Sữa Socola size M + 3 Cây Phô Mai Que kéo sợi ngập tràn chấm tương cà sốt bơ.",
            BasePrice = 72000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà sữa và phô mai que tiết kiệm"
        };
        var combo6 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trà sữa và xúc xích chiên",
            Slug = "combo-tra-sua-va-xuc-xich-chien",
            ProductType = "COMBO",
            Description = "1 Ly Trà Sữa Trân Châu Hoàng Kim size M + 2 Cây Xúc Xích Đức chiên khía lốc xoáy nóng sốt.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trà sữa và xúc xích chiên tiết kiệm"
        };
        var combo7 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo nước ép và đồ ăn vặt",
            Slug = "combo-nuoc-ep-va-do-an-vat",
            ProductType = "COMBO",
            Description = "1 Ly Nước Ép Ổi Hồng nguyên chất giàu vitamin C + 1 Phần Nem Chua Rán phố cổ giòn rụm.",
            BasePrice = 75000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo nước ép và đồ ăn vặt tiết kiệm"
        };
        var combo8 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo đồ uống và mì cay",
            Slug = "combo-do-uong-va-mi-cay",
            ProductType = "COMBO",
            Description = "1 Thố Mì Cay Hàn Quốc hải sản bò Mỹ cấp độ cay tùy chọn + 1 Ly Hồng Trà Macchiato mát lạnh chữa cay.",
            BasePrice = 99000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo đồ uống và mì cay tiết kiệm"
        };
        var combo9 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo đồ uống và cơm chiên",
            Slug = "combo-do-uong-va-com-chien",
            ProductType = "COMBO",
            Description = "1 Đĩa Cơm Chiên Kim Chi Trứng Lòng Đào thơm nức + 1 Ly Trà Tắc khổng lồ mát ruột chống ngấy.",
            BasePrice = 95000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo đồ uống và cơm chiên tiết kiệm"
        };
        var combo10 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo một người",
            Slug = "combo-mot-nguoi-solo-treat",
            ProductType = "COMBO",
            Description = "Khẩu phần solo chuẩn vị: 1 Ly Trà Sữa MilkyVerse Signature size M + 1 Phần Hồ Lô Chiên giòn thơm.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo một người tiết kiệm"
        };
        var combo11 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo hai người",
            Slug = "combo-hai-nguoi-doi-ban-than",
            ProductType = "COMBO",
            Description = "Bữa tiệc đôi bạn: 2 Ly Trà Sữa Tự Chọn size M + 1 Mẹt Viên Chiên Thập Cẩm + 1 Phần Khoai Tây Lắc Phô Mai.",
            BasePrice = 135000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo hai người tiết kiệm"
        };
        var combo12 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo nhóm bạn",
            Slug = "combo-nhom-ban-party-sieu-vui",
            ProductType = "COMBO",
            Description = "Party thịnh soạn 4 người: 4 Ly Đồ Uống Size L + 1 Mẹt Viên Chiên Lớn + 1 Hộp Chân Gà Sốt Thái + 2 Bánh Tráng Trộn.",
            BasePrice = 260000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo nhóm bạn tiết kiệm"
        };
        var combo13 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo tự chọn đồ uống và đồ ăn",
            Slug = "combo-tu-chon-do-uong-va-do-an",
            ProductType = "COMBO",
            Description = "Tự do ghép đôi 1 món đồ uống bất kỳ (size M) và 1 món ăn vặt yêu thích với ưu đãi tiết kiệm 15%.",
            BasePrice = 85000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo tự chọn đồ uống và đồ ăn tiết kiệm"
        };
        products.AddRange(new[] { combo1, combo2, combo3, combo4, combo5, combo6, combo7, combo8, combo9, combo10, combo11, combo12, combo13 });

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // ==========================================
        // 6. RECIPES FOR PRODUCTS (BOM / COSTING)
        // ==========================================
        var r1M = new Recipe { ProductId = p1.Id, Size = "M", Notes = "Công thức chuẩn Size M" };
        var r1L = new Recipe { ProductId = p1.Id, Size = "L", Notes = "Công thức chuẩn Size L" };
        context.Recipes.AddRange(r1M, r1L);
        await context.SaveChangesAsync();

        context.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = r1M.Id, IngredientId = ingBlackTea.Id, Amount = 150 },
            new RecipeIngredient { RecipeId = r1M.Id, IngredientId = ingFreshMilk.Id, Amount = 80 },
            new RecipeIngredient { RecipeId = r1M.Id, IngredientId = ingSugarSyrup.Id, Amount = 25 },
            new RecipeIngredient { RecipeId = r1L.Id, IngredientId = ingBlackTea.Id, Amount = 200 },
            new RecipeIngredient { RecipeId = r1L.Id, IngredientId = ingFreshMilk.Id, Amount = 110 },
            new RecipeIngredient { RecipeId = r1L.Id, IngredientId = ingSugarSyrup.Id, Amount = 35 }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 7. STOCK BATCHES (FEFO: First Expired First Out)
        // ==========================================
        context.StockBatches.AddRange(
            new StockBatch
            {
                IngredientId = ingBlackTea.Id,
                BatchNumber = "LOT-TEA-20261001",
                ManufactureDate = DateTime.UtcNow.AddDays(-15),
                ExpiryDate = DateTime.UtcNow.AddMonths(12),
                InitialQuantity = 30000,
                CurrentQuantity = 25000,
                UnitCost = 30
            },
            new StockBatch
            {
                IngredientId = ingFreshMilk.Id,
                BatchNumber = "LOT-MILK-20261014",
                ManufactureDate = DateTime.UtcNow.AddDays(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(12), // FEFO short shelf life
                InitialQuantity = 40000,
                CurrentQuantity = 30000,
                UnitCost = 28
            },
            new StockBatch
            {
                IngredientId = ingFishBall.Id,
                BatchNumber = "LOT-FBALL-20261010",
                ManufactureDate = DateTime.UtcNow.AddDays(-6),
                ExpiryDate = DateTime.UtcNow.AddMonths(3),
                InitialQuantity = 1500,
                CurrentQuantity = 1200,
                UnitCost = 800
            },
            new StockBatch
            {
                IngredientId = ingChickenFeet.Id,
                BatchNumber = "LOT-CHICK-20261012",
                ManufactureDate = DateTime.UtcNow.AddDays(-4),
                ExpiryDate = DateTime.UtcNow.AddMonths(2),
                InitialQuantity = 25000,
                CurrentQuantity = 20000,
                UnitCost = 110
            },
            new StockBatch
            {
                IngredientId = ingPotato.Id,
                BatchNumber = "LOT-POT-20261005",
                ManufactureDate = DateTime.UtcNow.AddDays(-11),
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                InitialQuantity = 30000,
                CurrentQuantity = 25000,
                UnitCost = 65
            }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 8. PURCHASE RECEIPTS (NHẬP KHO) & AUDIT
        // ==========================================
        var receipt1 = new PurchaseReceipt
        {
            ReceiptCode = "PNK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-01",
            SupplierName = "Công ty TNHH Thực Phẩm Tươi Sạch Hoàng Gia",
            CreatedByUserId = warehouse.Id,
            TotalCost = 4500000,
            Status = "Completed",
            Notes = "Nhập nguyên liệu tươi cho thực đơn đồ ăn và trà sữa tuần mới",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        context.PurchaseReceipts.Add(receipt1);
        await context.SaveChangesAsync();

        context.PurchaseReceiptItems.AddRange(
            new PurchaseReceiptItem
            {
                PurchaseReceiptId = receipt1.Id,
                IngredientId = ingFishBall.Id,
                Quantity = 1000,
                UnitPrice = 800,
                BatchNumber = "LOT-FBALL-20261010",
                ExpiryDate = DateTime.UtcNow.AddMonths(3)
            },
            new PurchaseReceiptItem
            {
                PurchaseReceiptId = receipt1.Id,
                IngredientId = ingChickenFeet.Id,
                Quantity = 20000,
                UnitPrice = 110,
                BatchNumber = "LOT-CHICK-20261012",
                ExpiryDate = DateTime.UtcNow.AddMonths(2)
            },
            new PurchaseReceiptItem
            {
                PurchaseReceiptId = receipt1.Id,
                IngredientId = ingPotato.Id,
                Quantity = 20000,
                UnitPrice = 65,
                BatchNumber = "LOT-POT-20261005",
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            }
        );

        var audit1 = new StockAudit
        {
            AuditCode = "KK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-01",
            AuditedByUserId = warehouse.Id,
            AuditDate = DateTime.UtcNow.AddDays(-1),
            Status = "Completed",
            Notes = "Kiểm kê định kỳ đầu tuần - chênh lệch trong mức hao hụt chế biến cho phép"
        };
        context.StockAudits.Add(audit1);
        await context.SaveChangesAsync();

        context.StockAuditItems.AddRange(
            new StockAuditItem
            {
                StockAuditId = audit1.Id,
                IngredientId = ingFreshMilk.Id,
                SystemQuantity = 30200,
                ActualQuantity = 30000,
                Variance = -200,
                Reason = "Hao hụt tự nhiên khi đong rót pha chế"
            },
            new StockAuditItem
            {
                StockAuditId = audit1.Id,
                IngredientId = ingFishBall.Id,
                SystemQuantity = 1200,
                ActualQuantity = 1200,
                Variance = 0,
                Reason = "Số lượng khớp hoàn toàn"
            }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 9. PROMOTIONS
        // ==========================================
        context.Promotions.AddRange(
            new Promotion
            {
                Code = "MILKYNEW",
                Description = "Giảm 20% cho khách hàng mới trải nghiệm vũ trụ Milkyverse",
                DiscountPercent = 20,
                DiscountAmount = 0,
                MinOrderValue = 50000,
                MaxUsage = 500,
                UsedCount = 12,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true
            },
            new Promotion
            {
                Code = "FREESHIP",
                Description = "Miễn phí vận chuyển 15.000đ cho đơn hàng từ 99.000đ",
                DiscountPercent = 0,
                DiscountAmount = 15000,
                MinOrderValue = 99000,
                MaxUsage = 1000,
                UsedCount = 45,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(6),
                IsActive = true
            },
            new Promotion
            {
                Code = "COMBOANVAT",
                Description = "Giảm thêm 25.000đ cho hóa đơn có món ăn vặt và trà sữa từ 150.000đ",
                DiscountPercent = 0,
                DiscountAmount = 25000,
                MinOrderValue = 150000,
                MaxUsage = 300,
                UsedCount = 19,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 10. REAL ORDERS WITH FOOD & COMBOS FOR ALL ROLES
        // ==========================================
        // 10.1 Order 1: Completed order (Đã giao thành công)
        var sampleOrder1 = new Order
        {
            OrderCode = "MV" + DateTime.UtcNow.ToString("yyyyMMdd") + "001",
            UserId = customer.Id,
            Subtotal = 164000,
            DiscountAmount = 15000,
            ShippingFee = 15000,
            TotalAmount = 164000,
            Status = "Completed",
            PaymentStatus = "Paid",
            PaymentMethod = "VIETQR",
            CustomerName = customer.FullName,
            CustomerPhone = customer.PhoneNumber,
            ShippingAddress = customer.Address,
            Note = "Giao giờ hành chính, nhiều đá riêng, tương ớt thêm",
            CreatedAt = DateTime.UtcNow.AddHours(-4)
        };
        context.Orders.Add(sampleOrder1);
        await context.SaveChangesAsync();

        var detail1_1 = new OrderDetail
        {
            OrderId = sampleOrder1.Id,
            ProductId = p1.Id,
            ProductName = p1.Name,
            ItemType = "DRINK",
            Size = "L",
            SugarLevel = "50%",
            IceLevel = "70%",
            UnitPrice = 52000,
            Quantity = 1,
            TotalPrice = 62000,
            CustomNotes = "Ít ngọt thanh mát"
        };
        var detail1_2 = new OrderDetail
        {
            OrderId = sampleOrder1.Id,
            ProductName = "Cá Viên Chiên Thập Cẩm Mẹt Lớn",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay nhẹ (Cấp 1)",
            DippingSauce = "Tương đen & Tương ớt xào tỏi",
            PortionSize = "1 mẹt lớn",
            UnitPrice = 65000,
            Quantity = 1,
            TotalPrice = 65000,
            CustomNotes = "Nhiều dưa leo và tương đen xào"
        };
        var detail1_3 = new OrderDetail
        {
            OrderId = sampleOrder1.Id,
            ProductName = "Bánh Tráng Trộn Milkyverse Đặc Biệt",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay vừa (Cấp 2)",
            DippingSauce = "Sốt bò sa tế",
            PortionSize = "Phần lớn",
            UnitPrice = 35000,
            Quantity = 1,
            TotalPrice = 35000,
            CustomNotes = "Nhiều xoài và rau răm"
        };
        context.OrderDetails.AddRange(detail1_1, detail1_2, detail1_3);
        await context.SaveChangesAsync();

        context.OrderItemOptions.Add(new OrderItemOption
        {
            OrderDetailId = detail1_1.Id,
            ToppingId = topPearl.Id,
            ToppingName = topPearl.Name,
            Price = 10000,
            Quantity = 1
        });

        context.OrderStatusHistories.AddRange(
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Pending", Note = "Khách hàng đặt đơn thanh toán VietQR", CreatedAt = DateTime.UtcNow.AddHours(-4) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Confirmed", Note = "Quầy đã xác nhận đơn hàng", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(50) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Preparing", Note = "Bếp & quầy đang làm món và pha chế", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(40) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Delivering", Note = "Shipper nhận đơn và đang giao", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(20) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Completed", Note = "Giao hàng thành công tới người nhận", CreatedAt = DateTime.UtcNow.AddHours(-3) }
        );

        // 10.2 Order 2: In Delivering (Đang giao hàng - COD - phân công cho Shipper)
        var sampleOrder2 = new Order
        {
            OrderCode = "MV" + DateTime.UtcNow.ToString("yyyyMMdd") + "002",
            UserId = customer.Id,
            Subtotal = 114000,
            DiscountAmount = 0,
            ShippingFee = 15000,
            TotalAmount = 129000,
            Status = "Delivering",
            PaymentStatus = "Pending",
            PaymentMethod = "COD",
            CustomerName = "Trần Thị Mai Phương",
            CustomerPhone = "0933445566",
            ShippingAddress = "Tòa nhà Bitexco, 2 Hải Triều, Bến Nghé, Quận 1, TP.HCM",
            Note = "Gọi trước khi đến 5 phút, thanh toán COD tiền mặt",
            CreatedAt = DateTime.UtcNow.AddMinutes(-45)
        };
        context.Orders.Add(sampleOrder2);
        await context.SaveChangesAsync();

        var detail2_1 = new OrderDetail
        {
            OrderId = sampleOrder2.Id,
            ProductName = "Chân Gà Rút Xương Sốt Thái Cay Giòn",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay vừa (Cấp 3)",
            DippingSauce = "Nước sốt Thái chua cay",
            PortionSize = "1 phần lớn",
            UnitPrice = 69000,
            Quantity = 1,
            TotalPrice = 69000,
            CustomNotes = "Nhiều cóc bao tử giòn"
        };
        var detail2_2 = new OrderDetail
        {
            OrderId = sampleOrder2.Id,
            ProductName = "Trà Sữa Milkyverse Signature",
            ItemType = "DRINK",
            Size = "M",
            SugarLevel = "70%",
            IceLevel = "100%",
            UnitPrice = 45000,
            Quantity = 1,
            TotalPrice = 45000,
            CustomNotes = "Chuẩn vị"
        };
        context.OrderDetails.AddRange(detail2_1, detail2_2);
        await context.SaveChangesAsync();

        context.OrderStatusHistories.AddRange(
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Pending", Note = "Đơn mới COD", CreatedAt = DateTime.UtcNow.AddMinutes(-45) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Confirmed", Note = "Quầy đã duyệt", CreatedAt = DateTime.UtcNow.AddMinutes(-40) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Preparing", Note = "Bếp chiên chân gà và quầy lắc trà", CreatedAt = DateTime.UtcNow.AddMinutes(-35) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Delivering", Note = "Bàn giao Shipper Hoàng Giao Hàng mang đi", CreatedAt = DateTime.UtcNow.AddMinutes(-15) }
        );

        // Shipper assignment
        context.DeliveryAssignments.Add(new DeliveryAssignment
        {
            OrderId = sampleOrder2.Id,
            ShipperId = shipper.Id,
            AssignedAt = DateTime.UtcNow.AddMinutes(-20),
            PickedUpAt = DateTime.UtcNow.AddMinutes(-15),
            Status = "InTransit",
            CodAmount = 129000,
            IsCodCollected = false,
            CodReconciled = false,
            Notes = "Khách dặn đứng sảnh chờ tại Bitexco"
        });

        // 10.3 Order 3: Pending Order (Chờ bếp / quầy xác nhận)
        var sampleOrder3 = new Order
        {
            OrderCode = "MV" + DateTime.UtcNow.ToString("yyyyMMdd") + "003",
            UserId = customer.Id,
            Subtotal = 109000,
            DiscountAmount = 0,
            ShippingFee = 15000,
            TotalAmount = 124000,
            Status = "Pending",
            PaymentStatus = "Pending",
            PaymentMethod = "COD",
            CustomerName = "Nguyễn Văn Hùng",
            CustomerPhone = "0912348899",
            ShippingAddress = "45 Nguyễn Thị Minh Khai, Phường Bến Nghé, Quận 1, TP.HCM",
            Note = "Giao gấp trước 12h trưa",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.Orders.Add(sampleOrder3);
        await context.SaveChangesAsync();

        var detail3_1 = new OrderDetail
        {
            OrderId = sampleOrder3.Id,
            ProductName = "Combo Siêu Cay: Mì Cay Hàn Quốc 7 Cấp Độ + Trà Ô Long Nướng Khói",
            ItemType = "COMBO",
            Size = "Standard",
            SpicyLevel = "Cấp độ 3 (Cay vừa)",
            DippingSauce = "Muối ớt chanh",
            PortionSize = "1 phần combo",
            UnitPrice = 109000,
            Quantity = 1,
            TotalPrice = 109000,
            CustomNotes = "Mì cay cấp 3, trà ít ngọt 50%"
        };
        context.OrderDetails.Add(detail3_1);

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = sampleOrder3.Id,
            Status = "Pending",
            Note = "Đơn mới nhận trên hệ thống MilkyVerse",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        await context.SaveChangesAsync();
    }
}
