using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Data;

public static class DbInitializer
{
    private static async Task EnsureColumnExistsAsync(MilkyVerseDbContext context, string tableName, string columnName, string columnType)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            bool columnExists = false;
            if (context.Database.IsSqlite())
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"PRAGMA table_info({tableName});";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        columnExists = true;
                        break;
                    }
                }
            }
            else
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}';";
                var result = await cmd.ExecuteScalarAsync();
                columnExists = Convert.ToInt32(result) > 0;
            }

            if (!columnExists)
            {
                await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};");
            }
        }
        catch
        {
            // Best effort migration fallback
        }
    }

    public static async Task SeedAsync(MilkyVerseDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureColumnExistsAsync(context, "Users", "AvatarUrl", "TEXT");
        await EnsureColumnExistsAsync(context, "Orders", "CancelReason", "TEXT");

        if (context.Users.Any())
        {
            if (!context.Users.Any(u => u.Email == "quay@milkyverse.com"))
            {
                context.Users.Add(new User
                {
                    FullName = "Nguyễn Thu Ngân (Phục vụ tại quầy)",
                    Email = "quay@milkyverse.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    PhoneNumber = "0908889999",
                    Address = "Chi nhánh MilkyVerse Quận 1, TP.HCM",
                    Role = "Staff",
                    AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            if (!context.Promotions.Any(p => p.Code == "MK2095F54" || p.Code == "#MK2095F54"))
            {
                context.Promotions.Add(new Promotion
                {
                    Code = "#MK2095F54",
                    Description = "Voucher ưu đãi tri ân khách hàng MilkyVerse - Giảm ngay 25.000đ",
                    DiscountPercent = 0,
                    DiscountAmount = 25000,
                    MinOrderValue = 40000,
                    MaxUsage = 10000,
                    UsedCount = 0,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true
                });
            }
            await context.SaveChangesAsync();
            return; // DB has been seeded
        }

        // ==========================================
        // 1. USERS & ROLES
        // ==========================================
        var admin = new User
        {
            FullName = "Quáº£n trá»‹ viÃªn MilkyVerse",
            Email = "admin@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            PhoneNumber = "0901234567",
            Address = "Sá»‘ 1 ÄÆ°á»ng ThiÃªn HÃ , Quáº­n 1, TP.HCM",
            Role = "Admin",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var manager = new User
        {
            FullName = "LÃª Quáº£n LÃ½ (Cá»­a hÃ ng trÆ°á»Ÿng)",
            Email = "manager@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            PhoneNumber = "0903334455",
            Address = "Sá»‘ 88 ÄÆ°á»ng VÅ© Trá»¥, Quáº­n 1, TP.HCM",
            Role = "Manager",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var staff = new User
        {
            FullName = "Nguyá»…n Barista & Báº¿p (NhÃ¢n viÃªn quáº§y)",
            Email = "staff@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
            PhoneNumber = "0909876543",
            Address = "Chi nhÃ¡nh MilkyVerse Quáº­n 3, TP.HCM",
            Role = "Staff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var warehouse = new User
        {
            FullName = "Pháº¡m Thá»§ Kho (Quáº£n lÃ½ kho & FEFO)",
            Email = "warehouse@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Warehouse@123"),
            PhoneNumber = "0911223344",
            Address = "Kho Tá»•ng MilkyVerse, BÃ¬nh Tháº¡nh, TP.HCM",
            Role = "WarehouseStaff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var shipper = new User
        {
            FullName = "HoÃ ng Giao HÃ ng (Shipper MilkyVerse)",
            Email = "shipper@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Shipper@123"),
            PhoneNumber = "0977889900",
            Address = "Äá»™i Giao Váº­n MilkyVerse Galaxy Express",
            Role = "DeliveryStaff",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer = new User
        {
            FullName = "Tráº§n Báº£o Ngá»c (KhÃ¡ch VIP)",
            Email = "khachhang@milkyverse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            PhoneNumber = "0988776655",
            Address = "123 ÄÆ°á»ng Sá»¯a TÆ°Æ¡i, PhÆ°á»ng Báº¿n NghÃ©, Quáº­n 1, TP.HCM",
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
            Name = "TrÃ  Sá»¯a Truyá»n Thá»‘ng",
            Slug = "tra-sua-truyen-thong",
            CategoryType = "DRINK",
            Description = "HÆ°Æ¡ng vá»‹ trÃ  sá»¯a cá»• Ä‘iá»ƒn Ä‘áº­m Ä‘Ã  tá»« lÃ¡ há»“ng trÃ  tuyá»ƒn chá»n vÃ  sá»¯a bÃ©o ngáº­y chuáº©n vá»‹ ÄÃ i Loan.",
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 1,
            IsActive = true
        };

        var catFruit = new Category
        {
            Name = "TrÃ  TrÃ¡i CÃ¢y",
            Slug = "tra-trai-cay",
            CategoryType = "DRINK",
            Description = "Thanh mÃ¡t giáº£i nhiá»‡t vá»›i 100% trÃ¡i cÃ¢y nhiá»‡t Ä‘á»›i tÆ°Æ¡i ngon tá»± nhiÃªn giÃ u vitamin.",
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 2,
            IsActive = true
        };

        var catPureTea = new Category
        {
            Name = "TrÃ  NguyÃªn Cháº¥t & TrÃ  Pha Vá»‹",
            Slug = "tra-nguyen-chat-pha-vi",
            CategoryType = "DRINK",
            Description = "ThÆ°á»Ÿng thá»©c cá»‘t trÃ  má»™c thanh khiáº¿t tuyá»ƒn chá»n tá»« nhá»¯ng Ä‘á»“i trÃ  danh tiáº¿ng vÃ  cÃ¡c biáº¿n táº¥u pha vá»‹ má»™c máº¡c.",
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 3,
            IsActive = true
        };

        var catCheese = new Category
        {
            Name = "TrÃ  Macchiato & Kem Cheese",
            Slug = "tra-macchiato-kem-cheese",
            CategoryType = "DRINK",
            Description = "Lá»›p kem sá»¯a phÃ´ mai Anchor bá»“ng bá»nh bÃ©o máº·n sÃ¡nh má»‹n káº¿t há»£p ná»n trÃ  háº£o háº¡ng thÆ¡m mÃ¡t.",
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 4,
            IsActive = true
        };

        var catFreshMilkDrink = new Category
        {
            Name = "Sá»¯a TÆ°Æ¡i & Äá»“ Uá»‘ng Sá»¯a",
            Slug = "sua-tuoi-do-uong-sua",
            CategoryType = "DRINK",
            Description = "Sá»¯a tÆ°Æ¡i thanh trÃ¹ng ÄÃ  Láº¡t nguyÃªn cháº¥t 100% bÃ©o ngáº­y tá»± nhiÃªn káº¿t há»£p cÃ¡c nguyÃªn liá»‡u thÆ°á»£ng háº¡ng.",
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 5,
            IsActive = true
        };

        var catMatchaCacao = new Category
        {
            Name = "Matcha & Äá»“ Uá»‘ng Cacao",
            Slug = "matcha-cacao",
            CategoryType = "DRINK",
            Description = "Nghá»‡ thuáº­t pha cháº¿ bá»™t trÃ  xanh Uji Nháº­t Báº£n vÃ  cacao nguyÃªn cháº¥t thÆ¡m lá»«ng ná»“ng nÃ n.",
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 6,
            IsActive = true
        };

        var catFrappeIceBlended = new Category
        {
            Name = "Frappe & Äá»“ Uá»‘ng ÄÃ¡ Xay",
            Slug = "frappe-da-xay",
            CategoryType = "DRINK",
            Description = "Thá»©c uá»‘ng Ä‘Ã¡ xay tuyáº¿t sÃ¡nh má»‹n mÃ¡t láº¡nh, phá»§ kem whipping bÃ©o ngáº­y bá»“ng bá»nh phong cÃ¡ch hiá»‡n Ä‘áº¡i.",
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 7,
            IsActive = true
        };

        var catFruitJuice = new Category
        {
            Name = "NÆ°á»›c Ã‰p TrÃ¡i CÃ¢y",
            Slug = "nuoc-ep-trai-cay",
            CategoryType = "DRINK",
            Description = "100% nÆ°á»›c Ã©p nguyÃªn cháº¥t tá»« hoa quáº£ tÆ°Æ¡i há»¯u cÆ¡, khÃ´ng thÃªm Ä‘Æ°á»ng hÃ³a há»c, thanh lá»c cÆ¡ thá»ƒ.",
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 8,
            IsActive = true
        };

        var catSodaGas = new Category
        {
            Name = "Soda & Äá»“ Uá»‘ng CÃ³ Gas",
            Slug = "soda-do-uong-co-gas",
            CategoryType = "DRINK",
            Description = "Soda bá»t ga sáº£ng khoÃ¡i káº¿t há»£p siro trÃ¡i cÃ¢y nháº­p kháº©u vÃ  lÃ¡ báº¡c hÃ  mÃ¡t láº¡nh xua tan náº¯ng hÃ¨.",
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 9,
            IsActive = true
        };

        var catLemonRefreshment = new Category
        {
            Name = "NÆ°á»›c Chanh & Äá»“ Uá»‘ng Giáº£i KhÃ¡t",
            Slug = "nuoc-chanh-giai-khat",
            CategoryType = "DRINK",
            Description = "CÃ¡c mÃ³n nÆ°á»›c giáº£i khÃ¡t dÃ¢n dÃ£ má»™c máº¡c tá»« chanh tÆ°Æ¡i, táº¯c, mÆ¡, sáº¥u, dá»«a xiÃªm tá»± nhiÃªn mÃ¡t ruá»™t.",
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 10,
            IsActive = true
        };

        var catCoffee = new Category
        {
            Name = "CÃ  PhÃª & Äá»“ Uá»‘ng Bá»• Sung",
            Slug = "ca-phe-do-uong-bo-sung",
            CategoryType = "DRINK",
            Description = "Háº¡t cÃ  phÃª Robusta BuÃ´n Ma Thuá»™t & Arabica Cáº§u Äáº¥t phá»‘i trá»™n tinh táº¿, Ä‘Ã¡nh thá»©c má»i nguá»“n cáº£m há»©ng.",
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 11,
            IsActive = true
        };

        var catOolong = new Category
        {
            Name = "TrÃ  Sá»¯a Oolong & Rang",
            Slug = "tra-sua-oolong-rang",
            CategoryType = "DRINK",
            Description = "Háº­u vá»‹ sÃ¢u láº¯ng, thÆ¡m ná»“ng khÃ³i gá»— vÃ  trÃ  Ã´ long thÆ°á»£ng háº¡ng tá»« cao nguyÃªn.",
            ImageUrl = "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 12,
            IsActive = true
        };

        // Food Categories (10 categories required)
        var catVienChien = new Category
        {
            Name = "CÃ¡ ViÃªn & CÃ¡c Loáº¡i ViÃªn ChiÃªn",
            Slug = "vien-chien",
            CategoryType = "FOOD",
            Description = "CÃ¡c loáº¡i viÃªn chiÃªn vÃ ng ruá»™m giÃ²n rá»¥m: cÃ¡ viÃªn, bÃ² viÃªn, má»±c viÃªn, thanh cua cháº¥m tÆ°Æ¡ng Ä‘en & sá»‘t bÆ¡ trá»©ng.",
            ImageUrl = "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 5,
            IsActive = true
        };

        var catGaChanGa = new Category
        {
            Name = "GÃ  & CÃ¡c MÃ³n ChÃ¢n GÃ ",
            Slug = "ga-chan-ga",
            CategoryType = "FOOD",
            Description = "ChÃ¢n gÃ  sá»‘t ThÃ¡i rÃºt xÆ°Æ¡ng giÃ²n sáº§n sáº­t, sáº£ táº¯c thÆ¡m lá»«ng, cÃ¡nh gÃ  chiÃªn máº¯m vÃ  gÃ  láº¯c phÃ´ mai Ä‘áº­m Ä‘Ã .",
            ImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 6,
            IsActive = true
        };

        var catKhoai = new Category
        {
            Name = "Khoai TÃ¢y & CÃ¡c MÃ³n Tá»« Khoai",
            Slug = "khoai-tay-khoai-lang",
            CategoryType = "FOOD",
            Description = "Khoai tÃ¢y chiÃªn giÃ²n rá»¥m láº¯c phÃ´ mai, rong biá»ƒn, BBQ, khoai lang kÃ©n bÃ©o bÃ¹i chuáº©n vá»‹ Äƒn váº·t tuá»•i thÆ¡.",
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 7,
            IsActive = true
        };

        var catPhoMaiXucXich = new Category
        {
            Name = "PhÃ´ Mai, XÃºc XÃ­ch & Äá»“ ChiÃªn",
            Slug = "pho-mai-xuc-xich-do-chien",
            CategoryType = "FOOD",
            Description = "PhÃ´ mai que kÃ©o sá»£i ngáº­p trÃ n, xÃºc xÃ­ch lá»‘c xoÃ¡y, nem chua rÃ¡n phá»‘ cá»• vÃ  hÃ¡ cáº£o chiÃªn giÃ²n tan.",
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 8,
            IsActive = true
        };

        var catBanhTrang = new Category
        {
            Name = "BÃ¡nh TrÃ¡ng & CÃ¡c MÃ³n Trá»™n",
            Slug = "banh-trang-mon-tron",
            CategoryType = "FOOD",
            Description = "BÃ¡nh trÃ¡ng trá»™n bÃ² khÃ´ sáº£ táº¿ cay ná»“ng, bÃ¡nh trÃ¡ng nÆ°á»›ng ÄÃ  Láº¡t giÃ²n rá»¥m, bÃ¡nh trÃ¡ng cuá»‘n sá»‘t me bÆ¡ trá»©ng bÃ©o ngáº­y.",
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 9,
            IsActive = true
        };

        var catTrungChen = new Category
        {
            Name = "Trá»©ng ChÃ©n & MÃ³n Trá»©ng NÆ°á»›ng",
            Slug = "trung-chen-mon-trung",
            CategoryType = "FOOD",
            Description = "Trá»©ng chÃ©n nÆ°á»›ng má»¡ hÃ nh phÃ´ mai kÃ©o sá»£i thÆ¡m phá»©c, cÃºt lá»™n xÃ o me chua ngá»t Ä‘áº­m Ä‘Ã  cuá»‘n hÃºt.",
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 10,
            IsActive = true
        };

        var catMiCay = new Category
        {
            Name = "MÃ¬ & CÃ¡c MÃ³n Cay 7 Cáº¥p Äá»™",
            Slug = "mi-mon-cay",
            CategoryType = "FOOD",
            Description = "MÃ¬ cay HÃ n Quá»‘c 7 cáº¥p Ä‘á»™ bÃ² Má»¹ háº£i sáº£n cay xuÃ½t xoa, mÃ¬ trá»™n xÃºc xÃ­ch phÃ´ mai trá»©ng lÃ²ng Ä‘Ã o nÃ³ng há»•i.",
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 11,
            IsActive = true
        };

        var catComNo = new Category
        {
            Name = "CÆ¡m ChiÃªn & MÃ³n Ä‚n No",
            Slug = "com-mon-no",
            CategoryType = "FOOD",
            Description = "CÆ¡m chiÃªn kim chi bÃ² Má»¹ háº¡t cÆ¡m tÆ¡i giÃ²n, cÆ¡m chiÃªn háº£i sáº£n háº¡t vÃ ng Æ°Æ¡m, kimbap chiÃªn giÃ²n áº¥m bá»¥ng.",
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 12,
            IsActive = true
        };

        var catNoiTang = new Category
        {
            Name = "Ná»™i Táº¡ng NÆ°á»›ng & XiÃªn NÆ°á»›ng",
            Slug = "noi-tang-mon-nuong",
            CategoryType = "FOOD",
            Description = "LÃ²ng nÆ°á»›ng sa táº¿ Ä‘áº­m Ä‘Ã  than há»“ng, dá»“i sá»¥n nÆ°á»›ng giÃ²n sáº§n sáº­t, sá»¥n gÃ  rang muá»‘i vÃ  xiÃªn que nÆ°á»›ng cay.",
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 13,
            IsActive = true
        };

        var catAnVatBoSung = new Category
        {
            Name = "Äá»“ Ä‚n Váº·t Bá»• Sung",
            Slug = "an-vat-bo-sung",
            CategoryType = "FOOD",
            Description = "Báº¯p xÃ o bÆ¡ tÃ©p thÆ¡m ná»©c mÅ©i, bÃ¡nh gáº¡o tokbokki cay ngá»t phÃ´ mai, khÃ´ gÃ  lÃ¡ chanh cay xÃ© nhÃ¢m nhi cá»±c Ä‘Ã£.",
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            DisplayOrder = 14,
            IsActive = true
        };

        var catCombo = new Category
        {
            Name = "Combo Thá»©c Ä‚n & NÆ°á»›c Uá»‘ng",
            Slug = "combo-an-uong",
            CategoryType = "COMBO",
            Description = "Combo káº¿t há»£p tuyá»‡t háº£o giá»¯a trÃ  sá»¯a mÃ¡t láº¡nh vÃ  cÃ¡c mÃ³n Äƒn váº·t nÃ³ng giÃ²n vá»›i má»©c giÃ¡ siÃªu Æ°u Ä‘Ã£i.",
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
        var ingBlackTea = new Ingredient { Name = "Cá»‘t Há»“ng TrÃ  Ceylon", Unit = "ml", CurrentStock = 25000, MinAlertThreshold = 3000, UnitCost = 30, CategoryType = "BEVERAGE" };
        var ingGreenTea = new Ingredient { Name = "Cá»‘t Lá»¥c TrÃ  LÃ i", Unit = "ml", CurrentStock = 20000, MinAlertThreshold = 3000, UnitCost = 35, CategoryType = "BEVERAGE" };
        var ingOolongTea = new Ingredient { Name = "Cá»‘t Ã” Long Rang KhÃ³i", Unit = "ml", CurrentStock = 18000, MinAlertThreshold = 2500, UnitCost = 45, CategoryType = "BEVERAGE" };
        var ingFreshMilk = new Ingredient { Name = "Sá»¯a TÆ°Æ¡i Thanh TrÃ¹ng Dalat", Unit = "ml", CurrentStock = 30000, MinAlertThreshold = 5000, UnitCost = 28, CategoryType = "BEVERAGE" };
        var ingSugarSyrup = new Ingredient { Name = "NÆ°á»›c ÄÆ°á»ng MÃ­a Tá»± NhiÃªn", Unit = "ml", CurrentStock = 40000, MinAlertThreshold = 5000, UnitCost = 15, CategoryType = "BEVERAGE" };
        var ingPearl = new Ingredient { Name = "TrÃ¢n ChÃ¢u Äen HoÃ ng Kim", Unit = "g", CurrentStock = 15000, MinAlertThreshold = 2000, UnitCost = 50, CategoryType = "BEVERAGE" };
        var ingWhitePearl = new Ingredient { Name = "TrÃ¢n ChÃ¢u Tráº¯ng 3Q GiÃ²n", Unit = "g", CurrentStock = 12000, MinAlertThreshold = 1500, UnitCost = 65, CategoryType = "BEVERAGE" };
        var ingCheeseFoam = new Ingredient { Name = "Kem PhÃ´ Mai Cheese Foam", Unit = "g", CurrentStock = 8000, MinAlertThreshold = 1000, UnitCost = 110, CategoryType = "BEVERAGE" };

        // Food Ingredients
        var ingFishBall = new Ingredient { Name = "CÃ¡ ViÃªn ThÆ°á»£ng Háº¡ng", Unit = "piece", CurrentStock = 1200, MinAlertThreshold = 200, UnitCost = 800, CategoryType = "FOOD" };
        var ingBeefBall = new Ingredient { Name = "BÃ² ViÃªn GÃ¢n GiÃ²n", Unit = "piece", CurrentStock = 900, MinAlertThreshold = 150, UnitCost = 1200, CategoryType = "FOOD" };
        var ingChickenFeet = new Ingredient { Name = "ChÃ¢n GÃ  RÃºt XÆ°Æ¡ng TÆ°Æ¡i", Unit = "g", CurrentStock = 20000, MinAlertThreshold = 3000, UnitCost = 110, CategoryType = "FOOD" };
        var ingPotato = new Ingredient { Name = "Khoai TÃ¢y Sá»£i Nháº­p Kháº©u", Unit = "g", CurrentStock = 25000, MinAlertThreshold = 4000, UnitCost = 65, CategoryType = "FOOD" };
        var ingCheeseStick = new Ingredient { Name = "PhÃ´ Mai Que Mozzarella", Unit = "piece", CurrentStock = 450, MinAlertThreshold = 80, UnitCost = 4500, CategoryType = "FOOD" };
        var ingRicePaper = new Ingredient { Name = "BÃ¡nh TrÃ¡ng PhÆ¡i SÆ°Æ¡ng TÃ¢y Ninh", Unit = "g", CurrentStock = 10000, MinAlertThreshold = 2000, UnitCost = 40, CategoryType = "FOOD" };
        var ingQuailEgg = new Ingredient { Name = "Trá»©ng CÃºt TÆ°Æ¡i", Unit = "piece", CurrentStock = 1500, MinAlertThreshold = 300, UnitCost = 1000, CategoryType = "FOOD" };
        var ingNoodles = new Ingredient { Name = "MÃ¬ Sá»£i HÃ n Quá»‘c Nongshim", Unit = "portion", CurrentStock = 300, MinAlertThreshold = 50, UnitCost = 9000, CategoryType = "FOOD" };
        var ingBeefSlice = new Ingredient { Name = "BÃ² Má»¹ Ba Chá»‰ Cuá»™n", Unit = "g", CurrentStock = 15000, MinAlertThreshold = 2500, UnitCost = 250, CategoryType = "FOOD" };
        var ingRice = new Ingredient { Name = "CÆ¡m Nguá»™i ChiÃªn Háº¡t TÆ¡i", Unit = "portion", CurrentStock = 200, MinAlertThreshold = 40, UnitCost = 5000, CategoryType = "FOOD" };
        var ingSausage = new Ingredient { Name = "XÃºc XÃ­ch Äá»©c XÃ´ng KhÃ³i", Unit = "piece", CurrentStock = 500, MinAlertThreshold = 100, UnitCost = 4000, CategoryType = "FOOD" };
        var ingThaiSauce = new Ingredient { Name = "NÆ°á»›c Sá»‘t ThÃ¡i Chua Cay Äáº·c Cháº¿", Unit = "ml", CurrentStock = 10000, MinAlertThreshold = 1500, UnitCost = 80, CategoryType = "FOOD" };

        context.Ingredients.AddRange(
            ingBlackTea, ingGreenTea, ingOolongTea, ingFreshMilk, ingSugarSyrup, ingPearl, ingWhitePearl, ingCheeseFoam,
            ingFishBall, ingBeefBall, ingChickenFeet, ingPotato, ingCheeseStick, ingRicePaper,
            ingQuailEgg, ingNoodles, ingBeefSlice, ingRice, ingSausage, ingThaiSauce
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 4. TOPPINGS (DRINKS & FOOD: 20 DRINK TOPPINGS + FOOD TOPPINGS)
        // ==========================================
        var topPearl = new Topping { IngredientId = ingPearl.Id, Name = "TrÃ¢n chÃ¢u hoÃ ng kim", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topWhitePearl = new Topping { IngredientId = ingPearl.Id, Name = "TrÃ¢n chÃ¢u tráº¯ng", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topBlackPearl = new Topping { IngredientId = ingPearl.Id, Name = "TrÃ¢n chÃ¢u Ä‘en", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topBrownSugarPearl = new Topping { IngredientId = ingPearl.Id, Name = "TrÃ¢n chÃ¢u Ä‘Æ°á»ng Ä‘en", Price = 10000, PortionAmount = 50, IsAvailable = true };
        var topMiniPearl = new Topping { IngredientId = ingPearl.Id, Name = "TrÃ¢n chÃ¢u mini", Price = 8000, PortionAmount = 40, IsAvailable = true };
        var topEggPudding = new Topping { IngredientId = ingPearl.Id, Name = "Pudding trá»©ng", Price = 10000, PortionAmount = 60, IsAvailable = true };
        var topChocoPudding = new Topping { IngredientId = ingPearl.Id, Name = "Pudding socola", Price = 10000, PortionAmount = 60, IsAvailable = true };
        var topFruitJelly = new Topping { IngredientId = ingPearl.Id, Name = "Tháº¡ch trÃ¡i cÃ¢y", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCoconutJelly = new Topping { IngredientId = ingPearl.Id, Name = "Tháº¡ch dá»«a", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCoffeeJelly = new Topping { IngredientId = ingPearl.Id, Name = "Tháº¡ch cÃ  phÃª", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topAloeJelly = new Topping { IngredientId = ingPearl.Id, Name = "Tháº¡ch nha Ä‘am", Price = 8000, PortionAmount = 50, IsAvailable = true };
        var topCheeseJelly = new Topping { IngredientId = ingPearl.Id, Name = "Tháº¡ch phÃ´ mai", Price = 12000, PortionAmount = 40, IsAvailable = true };
        var topFlan = new Topping { IngredientId = ingPearl.Id, Name = "Flan", Price = 12000, PortionAmount = 70, IsAvailable = true };
        var topRedBean = new Topping { IngredientId = ingPearl.Id, Name = "Äáº­u Ä‘á»", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topCrystalBobas = new Topping { IngredientId = ingPearl.Id, Name = "Háº¡t thá»§y tinh", Price = 10000, PortionAmount = 45, IsAvailable = true };
        var topCheeseFoam = new Topping { IngredientId = ingPearl.Id, Name = "Kem cheese", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topEggCream = new Topping { IngredientId = ingPearl.Id, Name = "Kem trá»©ng", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topSaltCream = new Topping { IngredientId = ingPearl.Id, Name = "Kem muá»‘i", Price = 15000, PortionAmount = 50, IsAvailable = true };
        var topOreoCrushed = new Topping { IngredientId = ingPearl.Id, Name = "Oreo nghiá»n", Price = 8000, PortionAmount = 30, IsAvailable = true };
        var topGrassJelly = new Topping { IngredientId = ingPearl.Id, Name = "SÆ°Æ¡ng sÃ¡o", Price = 8000, PortionAmount = 45, IsAvailable = true };
        var topExtraCheese = new Topping { IngredientId = ingCheeseStick.Id, Name = "ThÃªm PhÃ´ Mai KÃ©o Sá»£i (MÃ³n Ä‚n)", Price = 12000, PortionAmount = 40, IsAvailable = true };
        var topExtraEgg = new Topping { IngredientId = ingQuailEgg.Id, Name = "ThÃªm Trá»©ng á»p La LÃ²ng ÄÃ o", Price = 8000, PortionAmount = 1, IsAvailable = true };
        var topExtraSausage = new Topping { IngredientId = ingSausage.Id, Name = "ThÃªm XÃºc XÃ­ch Äá»©c ChiÃªn", Price = 12000, PortionAmount = 1, IsAvailable = true };

        context.Toppings.AddRange(topPearl, topWhitePearl, topBlackPearl, topBrownSugarPearl, topMiniPearl, topEggPudding, topChocoPudding, topFruitJelly, topCoconutJelly, topCoffeeJelly, topAloeJelly, topCheeseJelly, topFlan, topRedBean, topCrystalBobas, topCheeseFoam, topEggCream, topSaltCream, topOreoCrushed, topGrassJelly, topExtraCheese, topExtraEgg, topExtraSausage);
        await context.SaveChangesAsync();

        // ==========================================
        // 5. PRODUCTS (DRINKS + 10 FOOD CATEGORIES + COMBOS)
        // ==========================================
        var products = new List<Product>();

        // --- 5.1 Complete 163 Drink Products ---
        // --- 5.1.1 TrÃ  Sá»¯a Truyá»n Thá»‘ng ---
        var p1 = new Product
        {
            CategoryId = catClassic.Id,
            Name = "TrÃ  sá»¯a truyá»n thá»‘ng",
            Slug = "tra-sua-truyen-thong",
            ProductType = "DRINK",
            Description = "Cá»‘t há»“ng trÃ  háº£o háº¡ng hÃ²a quyá»‡n sá»¯a tÆ°Æ¡i thanh trÃ¹ng chuáº©n vá»‹ nguyÃªn báº£n.",
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
            Name = "TrÃ  sá»¯a trÃ¢n chÃ¢u Ä‘en",
            Slug = "tra-sua-tran-chau-den",
            ProductType = "DRINK",
            Description = "TrÃ  sá»¯a truyá»n thá»‘ng Ä‘áº­m vá»‹ káº¿t há»£p trÃ¢n chÃ¢u Ä‘en dai giÃ²n sáº§n sáº­t náº¥u máº­t mÃ­a.",
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
            Name = "TrÃ  sá»¯a trÃ¢n chÃ¢u tráº¯ng",
            Slug = "tra-sua-tran-chau-trang",
            ProductType = "DRINK",
            Description = "TrÃ  sá»¯a thÆ¡m bÃ©o kÃ¨m trÃ¢n chÃ¢u 3Q tráº¯ng ngá»c trai giÃ²n tan thanh nhiá»‡t.",
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
            Name = "TrÃ  sá»¯a trÃ¢n chÃ¢u hoÃ ng kim",
            Slug = "tra-sua-tran-chau-hoang-kim",
            ProductType = "DRINK",
            Description = "TrÃ¢n chÃ¢u hoÃ ng kim náº¥u Ä‘Æ°á»ng nÃ¢u sÃ¡nh Ã³ng, thÆ¡m dáº»o Ä‘áº·c trÆ°ng cá»§a MilkyVerse.",
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
            Name = "TrÃ  sá»¯a Ä‘Æ°á»ng Ä‘en",
            Slug = "tra-sua-duong-den",
            ProductType = "DRINK",
            Description = "Vá»‹ ngá»t dá»‹u thÆ¡m ná»“ng cá»§a máº­t mÃ­a Ä‘Æ°á»ng Ä‘en cÃ´ Ä‘áº·c thá»§ cÃ´ng.",
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
            Name = "TrÃ  sá»¯a trÃ¢n chÃ¢u Ä‘Æ°á»ng Ä‘en",
            Slug = "tra-sua-tran-chau-duong-den",
            ProductType = "DRINK",
            Description = "Sá»± káº¿t há»£p kinh Ä‘iá»ƒn giá»¯a sá»¯a trÃ  Ä‘áº­m Ä‘Ã  vÃ  dÃ²ng trÃ¢n chÃ¢u Ä‘Æ°á»ng Ä‘en nÃ³ng dáº»o.",
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
            Name = "TrÃ  sá»¯a khoai mÃ´n",
            Slug = "tra-sua-khoai-mon",
            ProductType = "DRINK",
            Description = "Vá»‹ khoai mÃ´n bÃ¹i bÃ©o tá»± nhiÃªn, mÃ u tÃ­m pastel ngá»t ngÃ o quyáº¿n rÅ©.",
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
            Name = "TrÃ  sá»¯a matcha",
            Slug = "tra-sua-matcha",
            ProductType = "DRINK",
            Description = "Bá»™t trÃ  xanh Uji Nháº­t Báº£n nguyÃªn cháº¥t thÆ¡m thanh mÃ¡t, cÃ¢n báº±ng vá»‹ chÃ¡t nháº¹.",
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
            Name = "TrÃ  sá»¯a socola",
            Slug = "tra-sua-socola",
            ProductType = "DRINK",
            Description = "Socola Ä‘en nguyÃªn cháº¥t Ä‘áº¯ng nháº¹ hÃ²a quyá»‡n cá»‘t sá»¯a ngá»t ngÃ o sÃ¡nh má»‹n.",
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
            Name = "TrÃ  sá»¯a cacao",
            Slug = "tra-sua-cacao",
            ProductType = "DRINK",
            Description = "Cacao Báº¿n Tre nguyÃªn cháº¥t thÆ¡m lá»«ng ná»“ng nÃ n, Ä‘áº­m Ä‘Ã  vá»‹ truyá»n thá»‘ng.",
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
            Name = "TrÃ  sá»¯a dÃ¢u",
            Slug = "tra-sua-dau",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng dÃ¢u tÃ¢y ÄÃ  Láº¡t tÆ°Æ¡i má»›i hÃ²a tan trong cá»‘t trÃ  sá»¯a thÆ¡m mÃ¡t.",
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
            Name = "TrÃ  sá»¯a caramel",
            Slug = "tra-sua-caramel",
            ProductType = "DRINK",
            Description = "Sá»‘t caramel náº¥u thá»§ cÃ´ng thÆ¡m ngáº­y mÃ¹i bÆ¡ Ä‘Æ°á»ng chÃ¡y cuá»‘n hÃºt.",
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
            Name = "TrÃ  sá»¯a báº¡c hÃ ",
            Slug = "tra-sua-bac-ha",
            ProductType = "DRINK",
            Description = "Vá»‹ the mÃ¡t sáº£ng khoÃ¡i cá»§a lÃ¡ báº¡c hÃ  bá»«ng tá»‰nh má»i giÃ¡c quan.",
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
            Name = "TrÃ  sá»¯a háº¡nh nhÃ¢n",
            Slug = "tra-sua-hanh-nhan",
            ProductType = "DRINK",
            Description = "Háº¡nh nhÃ¢n nÆ°á»›ng thÆ¡m bÃ¹i Ä‘áº·c trÆ°ng phong cÃ¡ch phÆ°Æ¡ng TÃ¢y.",
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
            Name = "TrÃ  sá»¯a ThÃ¡i xanh",
            Slug = "tra-sua-thai-xanh",
            ProductType = "DRINK",
            Description = "LÃ¡ trÃ  ThÃ¡i xanh thÆ¡m mÃ¡t, mÃ u xanh ngá»c bÃ­ch báº¯t máº¯t giáº£i nhiá»‡t sáº£ng khoÃ¡i.",
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
            Name = "TrÃ  sá»¯a ThÃ¡i Ä‘á»",
            Slug = "tra-sua-thai-do",
            ProductType = "DRINK",
            Description = "TrÃ  ThÃ¡i Ä‘á» truyá»n thá»‘ng thÆ¡m ná»“ng tháº£o má»™c, vá»‹ ngá»t bÃ©o Ä‘áº­m sÃ¢u.",
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
            Name = "TrÃ  sá»¯a Oreo",
            Slug = "tra-sua-oreo",
            ProductType = "DRINK",
            Description = "TrÃ  sá»¯a bÃ©o ngáº­y ráº¯c vá»¥n bÃ¡nh quy Oreo giÃ²n rá»¥m thÆ¡m ngon khÃ³ cÆ°á»¡ng.",
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
            Name = "TrÃ  sá»¯a tiramisu",
            Slug = "tra-sua-tiramisu",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng vá»‹ bÃ¡nh Tiramisu trá»© danh nÆ°á»›c Ã hÃ²a quyá»‡n lá»›p kem mascarpone bÃ©o ngáº­y.",
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
            Name = "TrÃ  sá»¯a kem cheese",
            Slug = "tra-sua-kem-cheese",
            ProductType = "DRINK",
            Description = "Lá»›p cheese foam bá»“ng bá»nh bÃ©o máº·n phá»§ trÃªn ná»n trÃ  sá»¯a truyá»n thá»‘ng.",
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
            Name = "TrÃ  sá»¯a kem trá»©ng",
            Slug = "tra-sua-kem-trung",
            ProductType = "DRINK",
            Description = "Kem trá»©ng vÃ ng Ã³ng nÆ°á»›ng khÃ² máº·t thÆ¡m lá»«ng bÃ©o ngáº­y ngá»t ngÃ o.",
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
            Name = "TrÃ  sá»¯a kem muá»‘i",
            Slug = "tra-sua-kem-muoi",
            ProductType = "DRINK",
            Description = "Lá»›p kem muá»‘i biá»ƒn sÃ¡nh má»‹n Ä‘áº­m vá»‹ máº·n ngá»t hÃ i hÃ²a Ä‘á»™c Ä‘Ã¡o.",
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
            Name = "TrÃ  sá»¯a pudding",
            Slug = "tra-sua-pudding",
            ProductType = "DRINK",
            Description = "Pudding trá»©ng tÆ°Æ¡i má»m má»‹n tan ngay Ä‘áº§u lÆ°á»¡i káº¿t há»£p trÃ  sá»¯a Ãªm dá»‹u.",
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
            Name = "TrÃ  sá»¯a flan",
            Slug = "tra-sua-flan",
            ProductType = "DRINK",
            Description = "BÃ¡nh flan caramen bÃ©o ngáº­y má»m mÆ°á»£t nguyÃªn chiáº¿c tháº£ trong ly trÃ  sá»¯a.",
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
            Name = "TrÃ  sá»¯a tháº¡ch trÃ¡i cÃ¢y",
            Slug = "tra-sua-thach-trai-cay",
            ProductType = "DRINK",
            Description = "Tháº¡ch trÃ¡i cÃ¢y cáº§u vá»“ng giÃ²n dai thanh mÃ¡t giáº£i nhiá»‡t mÃ¹a hÃ¨.",
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
            Name = "TrÃ  sá»¯a nhiá»u topping",
            Slug = "tra-sua-nhieu-topping",
            ProductType = "DRINK",
            Description = "Bá»¯a tiá»‡c topping thá»‹nh soáº¡n: trÃ¢n chÃ¢u Ä‘en, tháº¡ch 3Q, pudding trá»©ng vÃ  sÆ°Æ¡ng sÃ¡o.",
            BasePrice = 58000,
            ImageUrl = "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.2 TrÃ  TrÃ¡i CÃ¢y ---
        products.Add(new Product
        {
            CategoryId = catFruit.Id,
            Name = "TrÃ  Ä‘Ã o cam sáº£",
            Slug = "tra-dao-cam-sa",
            ProductType = "DRINK",
            Description = "Thá»©c uá»‘ng quá»‘c dÃ¢n: ÄÃ o miáº¿ng giÃ²n rá»¥m, cam vÃ ng má»ng nÆ°á»›c vÃ  hÆ°Æ¡ng sáº£ áº¥m ná»“ng.",
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
            Name = "TrÃ  váº£i",
            Slug = "tra-vai",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng váº£i thiá»u ngá»t ngÃ o thanh khiáº¿t káº¿t há»£p ná»n lá»¥c trÃ  lÃ i thanh tao.",
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
            Name = "TrÃ  chanh dÃ¢y",
            Slug = "tra-chanh-day",
            ProductType = "DRINK",
            Description = "Vá»‹ chua ngá»t thanh thoÃ¡t cá»§a chanh dÃ¢y tÆ°Æ¡i nguyÃªn háº¡t bá»«ng tá»‰nh nÄƒng lÆ°á»£ng.",
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
            Name = "TrÃ  dÃ¢u tÃ¢y",
            Slug = "tra-dau-tay",
            ProductType = "DRINK",
            Description = "DÃ¢u tÃ¢y tÆ°Æ¡i ngÃ o Ä‘Æ°á»ng thÆ¡m phá»©c, mÃ u Ä‘á» há»“ng rá»±c rá»¡ báº¯t máº¯t.",
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
            Name = "TrÃ  xoÃ i",
            Slug = "tra-xoai",
            ProductType = "DRINK",
            Description = "XoÃ i cÃ¡t chÃ­n má»ng ngá»t thÆ¡m nhiá»‡t Ä‘á»›i hÃ²a cÃ¹ng trÃ  xanh thanh mÃ¡t.",
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
            Name = "TrÃ  táº¯c",
            Slug = "tra-tac",
            ProductType = "DRINK",
            Description = "TrÃ  táº¯c truyá»n thá»‘ng chua chua ngá»t ngá»t, giáº£i khÃ¡t tá»©c thÃ¬ ngÃ y náº¯ng nÃ³ng.",
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
            Name = "TrÃ  cam chanh",
            Slug = "tra-cam-chanh",
            ProductType = "DRINK",
            Description = "Sá»± káº¿t há»£p vitamin C dá»“i dÃ o tá»« cam vÃ ng vÃ  chanh tÆ°Æ¡i mÃ¡t lÃ nh.",
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
            Name = "TrÃ  dÆ°a háº¥u",
            Slug = "tra-dua-hau",
            ProductType = "DRINK",
            Description = "NÆ°á»›c Ã©p dÆ°a háº¥u Ä‘á» má»ng ngá»t mÃ¡t tá»± nhiÃªn pha trÃªn ná»n lá»¥c trÃ .",
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
            Name = "TrÃ  á»•i há»“ng",
            Slug = "tra-oi-hong",
            ProductType = "DRINK",
            Description = "á»”i há»“ng xÃ¡ lá»‹ thÆ¡m lá»«ng hÆ°Æ¡ng hoa, vá»‹ ngá»t thanh lÆ°u luyáº¿n khÃ³ quÃªn.",
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
            Name = "TrÃ  dá»©a",
            Slug = "tra-dua",
            ProductType = "DRINK",
            Description = "Dá»©a nhiá»‡t Ä‘á»›i thÆ¡m ngá»t dá»‹u káº¿t há»£p trÃ  xanh mÃ¡t rÆ°á»£i sáº£ng khoÃ¡i.",
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
            Name = "TrÃ  kiwi",
            Slug = "tra-kiwi",
            ProductType = "DRINK",
            Description = "Kiwi xanh nháº­p kháº©u má»ng nÆ°á»›c, vá»‹ chua thanh giÃ u dÆ°á»¡ng cháº¥t vÃ  cháº¥t xÆ¡.",
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
            Name = "TrÃ  tÃ¡o xanh",
            Slug = "tra-tao-xanh",
            ProductType = "DRINK",
            Description = "TÃ¡o xanh giÃ²n ngá»t thanh tao, hÆ°Æ¡ng vá»‹ dá»‹u nháº¹ Ãªm Ã¡i.",
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
            Name = "TrÃ  nho",
            Slug = "tra-nho",
            ProductType = "DRINK",
            Description = "Nho tÃ­m má»ng nÆ°á»›c, vá»‹ ngá»t dá»‹u vÃ  mÃ u sáº¯c báº¯t máº¯t thanh lá»‹ch.",
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
            Name = "TrÃ  dÃ¢u táº±m",
            Slug = "tra-dau-tam",
            ProductType = "DRINK",
            Description = "DÃ¢u táº±m ÄÃ  Láº¡t chua ngá»t Ä‘áº­m Ä‘Ã , giÃ u cháº¥t chá»‘ng oxy hÃ³a tá»± nhiÃªn.",
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
            Name = "TrÃ  chanh máº­t ong",
            Slug = "tra-chanh-mat-ong-trai-cay",
            ProductType = "DRINK",
            Description = "Máº­t ong hoa rá»«ng nguyÃªn cháº¥t káº¿t há»£p nÆ°á»›c cá»‘t chanh tÆ°Æ¡i áº¥m Ã¡p dá»‹u cá»• há»ng.",
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
            Name = "TrÃ  chanh dÃ¢y cam",
            Slug = "tra-chanh-day-cam",
            ProductType = "DRINK",
            Description = "Báº£n hÃ²a táº¥u cam vÃ ng tÆ°Æ¡i vÃ  chanh dÃ¢y chua ngá»t tÆ°Æ¡i mÃ¡t.",
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
            Name = "TrÃ  nhiá»‡t Ä‘á»›i tháº­p cáº©m",
            Slug = "tra-nhiet-doi-thap-cam",
            ProductType = "DRINK",
            Description = "Ngáº­p trÃ n cÃ¡c lÃ¡t trÃ¡i cÃ¢y tÆ°Æ¡i: cam, dÆ°a háº¥u, tÃ¡o, dÃ¢u tÃ¢y vÃ  chanh dÃ¢y.",
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
            Name = "TrÃ  trÃ¡i cÃ¢y nhiá»‡t Ä‘á»›i",
            Slug = "tra-trai-cay-nhiet-doi",
            ProductType = "DRINK",
            Description = "TrÃ¡i cÃ¢y tuyá»ƒn chá»n tÆ°Æ¡i má»›i má»—i ngÃ y trÃªn ná»n trÃ  Ã´ long thanh nhÃ£.",
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
            Name = "TrÃ  mÃ£ng cáº§u",
            Slug = "tra-mang-cau",
            ProductType = "DRINK",
            Description = "MÃ£ng cáº§u xiÃªm chua ngá»t dáº»o thÆ¡m, mÃ³n hot trend tÆ°Æ¡i mÃ¡t khÃ³ chá»‘i tá»«.",
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
            Name = "TrÃ  máº­n",
            Slug = "tra-man",
            ProductType = "DRINK",
            Description = "Máº­n Háº­u ngÃ¢m chua ngá»t Ä‘áº­m Ä‘Ã  phong vá»‹ nÃºi rá»«ng TÃ¢y Báº¯c.",
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
            Name = "TrÃ  dÆ°a lÆ°á»›i",
            Slug = "tra-dua-luoi",
            ProductType = "DRINK",
            Description = "DÆ°a lÆ°á»›i hoÃ ng kim thÆ¡m dá»‹u, ngá»t lÃ nh thanh mÃ¡t giáº£i nhiá»‡t ngÃ y hÃ¨.",
            BasePrice = 46000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.3 TrÃ  NguyÃªn Cháº¥t & TrÃ  Pha Vá»‹ ---
        products.Add(new Product
        {
            CategoryId = catPureTea.Id,
            Name = "TrÃ  xanh",
            Slug = "tra-xanh-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  thÃ¡i nguyÃªn xanh trong thanh khiáº¿t, tiá»n chÃ¡t dá»‹u háº­u ngá»t sÃ¢u.",
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
            Name = "TrÃ  Ä‘en",
            Slug = "tra-den-nguyen-chat",
            ProductType = "DRINK",
            Description = "Há»“ng trÃ  á»§ men toÃ n pháº§n mÃ u há»• phÃ¡ch Ä‘áº­m Ä‘Ã  hÆ°Æ¡ng hoa cá» khÃ´.",
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
            Name = "TrÃ  Ã´ long",
            Slug = "tra-o-long-nguyen-chat",
            ProductType = "DRINK",
            Description = "TrÃ  Ã´ long cao nguyÃªn thÆ¡m hÆ°Æ¡ng hoa má»™c, vá»‹ trÃ  Ä‘áº±m tháº¯m tao nhÃ£.",
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
            Name = "TrÃ  lÃ i",
            Slug = "tra-lai-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  Æ°á»›p hoa nhÃ i tá»± nhiÃªn thÆ¡m ngÃ¡t thanh tao thÆ° giÃ£n tinh tháº§n.",
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
            Name = "Há»“ng trÃ ",
            Slug = "hong-tra-nguyen-chat",
            ProductType = "DRINK",
            Description = "Cá»‘t há»“ng trÃ  nguyÃªn báº£n Ä‘áº­m vá»‹ thÃ­ch há»£p thÆ°á»Ÿng thá»©c má»™c hoáº·c thÃªm Ä‘Ã¡.",
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
            Name = "Lá»¥c trÃ ",
            Slug = "luc-tra-nguyen-chat",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  non thÆ¡m hÆ°Æ¡ng cá»‘m, vá»‹ thanh mÃ¡t dá»… chá»‹u.",
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
            Name = "TrÃ  nhÃ i máº­t ong",
            Slug = "tra-nhai-mat-ong",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng hoa lÃ i quyá»‡n máº­t ong rá»«ng ngá»t thanh tao nhÃ£.",
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
            Name = "TrÃ  chanh",
            Slug = "tra-chanh-pho-co",
            ProductType = "DRINK",
            Description = "TrÃ  chanh phá»‘ cá»• truyá»n thá»‘ng Ä‘áº­m Ä‘Ã  chua ngá»t giáº£i khÃ¡t dÃ¢n dÃ£.",
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
            Name = "TrÃ  chanh máº­t ong",
            Slug = "tra-chanh-mat-ong-pha-vi",
            ProductType = "DRINK",
            Description = "NÆ°á»›c cá»‘t chanh tÆ°Æ¡i hÃ²a máº­t ong hoa cÃ  phÃª thanh khiáº¿t.",
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
            Name = "TrÃ  gá»«ng máº­t ong",
            Slug = "tra-gung-mat-ong",
            ProductType = "DRINK",
            Description = "Gá»«ng tÆ°Æ¡i Ä‘áº­p dáº­p thÆ¡m cay áº¥m ná»“ng káº¿t há»£p máº­t ong tá»‘t cho sá»©c khá»e.",
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
            Name = "TrÃ  quáº¥t máº­t ong",
            Slug = "tra-quat-mat-ong",
            ProductType = "DRINK",
            Description = "Quáº¥t tÆ°Æ¡i má»ng nÆ°á»›c thÆ¡m tinh dáº§u hÃ²a cÃ¹ng máº­t ong dá»‹u dÃ ng.",
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
            Name = "TrÃ  váº£i lÃ i",
            Slug = "tra-vai-lai",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  lÃ i Ãªm dá»‹u káº¿t há»£p hÆ°Æ¡ng váº£i thiá»u ngá»t ngÃ o thanh lá»‹ch.",
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
            Name = "TrÃ  Ã´ long Ä‘Ã o",
            Slug = "tra-o-long-dao",
            ProductType = "DRINK",
            Description = "TrÃ  Ã´ long nÆ°á»›ng thÆ¡m hÆ°Æ¡ng káº¿t há»£p Ä‘Ã o tÆ°Æ¡i má»ng nÆ°á»›c.",
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
            Name = "TrÃ  Ã´ long dÃ¢u",
            Slug = "tra-o-long-dau",
            ProductType = "DRINK",
            Description = "Sá»± gáº·p gá»¡ giá»¯a trÃ  Ã´ long Ä‘áº±m tháº¯m vÃ  dÃ¢u tÃ¢y ngá»t ngÃ o.",
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
            Name = "TrÃ  xanh cam",
            Slug = "tra-xanh-cam",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  thanh mÃ¡t phá»‘i tÃ©p cam sÃ nh tÆ°Æ¡i nguyÃªn cháº¥t.",
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
            Name = "TrÃ  lÃ i táº¯c",
            Slug = "tra-lai-tac",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng thÆ¡m hoa lÃ i Ä‘iá»ƒm xuyáº¿t vá»‹ chua thanh sáº£ng khoÃ¡i cá»§a táº¯c tÆ°Æ¡i.",
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
            Name = "TrÃ  atiso Ä‘á»",
            Slug = "tra-atiso-do",
            ProductType = "DRINK",
            Description = "TrÃ  hoa Hibiscus mÃ u Ä‘á» ruby rá»±c rá»¡, vá»‹ chua thanh tá»± nhiÃªn Ä‘áº¹p da.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.4 TrÃ  Macchiato & Kem Cheese ---
        products.Add(new Product
        {
            CategoryId = catCheese.Id,
            Name = "TrÃ  xanh macchiato",
            Slug = "tra-xanh-macchiato",
            ProductType = "DRINK",
            Description = "Lá»¥c trÃ  thanh mÃ¡t phá»§ lá»›p kem macchiato tráº¯ng muá»‘t sÃ¡nh má»‹n nhÆ° tuyáº¿t.",
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
            Name = "TrÃ  Ã´ long kem cheese",
            Slug = "tra-o-long-kem-cheese",
            ProductType = "DRINK",
            Description = "TrÃ  Ã´ long thÆ¡m Ä‘áº­m vá»‹ khÃ³i káº¿t há»£p cheese foam phÃ´ mai bÃ©o máº·n tuyá»‡t Ä‘á»‰nh.",
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
            Name = "Há»“ng trÃ  kem muá»‘i",
            Slug = "hong-tra-kem-muoi",
            ProductType = "DRINK",
            Description = "Há»“ng trÃ  Ceylon Ä‘áº­m Ä‘Ã  nÃ¢ng niu bá»Ÿi lá»›p bá»t kem muá»‘i biá»ƒn máº·n mÃ  quyáº¿n rÅ©.",
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
            Name = "TrÃ  lÃ i kem cheese",
            Slug = "tra-lai-kem-cheese",
            ProductType = "DRINK",
            Description = "Hoa lÃ i thÆ¡m ngÃ¡t dá»‹u dÃ ng hÃ²a trong lá»›p kem phÃ´ mai bÃ©o ngáº­y mÆ°á»£t mÃ .",
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
            Name = "TrÃ  Ä‘Ã o kem cheese",
            Slug = "tra-dao-kem-cheese",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng Ä‘Ã o ngá»t thÆ¡m thanh thoÃ¡t káº¿t há»£p kem phÃ´ mai thÆ¡m ngáº­y ngáº¥t ngÃ¢y.",
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
            Name = "TrÃ  dÃ¢u kem cheese",
            Slug = "tra-dau-kem-cheese",
            ProductType = "DRINK",
            Description = "Vá»‹ chua ngá»t cá»§a dÃ¢u tÃ¢y Ä‘á» má»ng hÃ²a quyá»‡n lá»›p kem bÃ©o máº·n Ä‘á»‰nh cao.",
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
            Name = "TrÃ  xoÃ i kem cheese",
            Slug = "tra-xoai-kem-cheese",
            ProductType = "DRINK",
            Description = "XoÃ i chÃ­n vÃ ng Æ°Æ¡m nhiá»‡t Ä‘á»›i cÃ¹ng kem cheese bÃ©o máº·n bá»“ng bá»nh.",
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
            Name = "TrÃ  sá»¯a macchiato",
            Slug = "tra-sua-macchiato",
            ProductType = "DRINK",
            Description = "TrÃ  sá»¯a truyá»n thá»‘ng nhÃ¢n Ä‘Ã´i Ä‘á»™ bÃ©o ngáº­y vá»›i lá»›p kem macchiato Ä‘áº·c cháº¿.",
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
            Name = "TrÃ  Ä‘en kem trá»©ng",
            Slug = "tra-den-kem-trung",
            ProductType = "DRINK",
            Description = "Ná»n trÃ  Ä‘en Ä‘áº­m vá»‹ nÃ¢ng Ä‘á»¡ lá»›p kem trá»©ng vÃ ng nÆ°á»›ng chÃ¡y thÆ¡m phá»©c.",
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
            Name = "TrÃ  Ã´ long kem muá»‘i",
            Slug = "tra-o-long-kem-muoi",
            ProductType = "DRINK",
            Description = "Ã” long cao nguyÃªn thÆ°á»£ng háº¡ng hÃ²a quyá»‡n kem muá»‘i biá»ƒn tinh khiáº¿t.",
            BasePrice = 52000,
            ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.5 Sá»¯a TÆ°Æ¡i & Äá»“ Uá»‘ng Sá»¯a ---
        products.Add(new Product
        {
            CategoryId = catFreshMilkDrink.Id,
            Name = "Sá»¯a tÆ°Æ¡i trÃ¢n chÃ¢u Ä‘Æ°á»ng Ä‘en",
            Slug = "sua-tuoi-tran-chau-duong-den",
            ProductType = "DRINK",
            Description = "MÃ³n 'hot trend' vÆ°á»£t thá»i gian: Sá»¯a tÆ°Æ¡i thanh trÃ¹ng láº¡nh cÃ¹ng trÃ¢n chÃ¢u Ä‘Æ°á»ng Ä‘en dáº»o nÃ³ng.",
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
            Name = "Sá»¯a tÆ°Æ¡i Ä‘Æ°á»ng nÃ¢u",
            Slug = "sua-tuoi-duong-nau",
            ProductType = "DRINK",
            Description = "VÃ¢n Ä‘Æ°á»ng nÃ¢u há»• phÃ¡ch quá»‡t thÃ nh ly Ã´m láº¥y dÃ²ng sá»¯a tÆ°Æ¡i thanh mÃ¡t bÃ©o ngáº­y.",
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
            Name = "Sá»¯a tÆ°Æ¡i matcha",
            Slug = "sua-tuoi-matcha",
            ProductType = "DRINK",
            Description = "Matcha Uji xanh mÆ°á»›t phÃ¢n táº§ng hoÃ n háº£o trÃªn ná»n sá»¯a tÆ°Æ¡i tráº¯ng ngáº§n.",
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
            Name = "Sá»¯a tÆ°Æ¡i socola",
            Slug = "sua-tuoi-socola",
            ProductType = "DRINK",
            Description = "Sá»‘t socola Ä‘en sÃ¡nh Ä‘áº­m hÃ²a vÃ o sá»¯a tÆ°Æ¡i bÃ©o thÆ¡m ngá»t lá»‹m tim.",
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
            Name = "Sá»¯a tÆ°Æ¡i dÃ¢u",
            Slug = "sua-tuoi-dau",
            ProductType = "DRINK",
            Description = "Má»©t dÃ¢u tÃ¢y tÆ°Æ¡i ngÃ o Ä‘Æ°á»ng sÃ¡nh má»‹n káº¿t há»£p sá»¯a tÆ°Æ¡i thanh trÃ¹ng.",
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
            Name = "Sá»¯a tÆ°Æ¡i khoai mÃ´n",
            Slug = "sua-tuoi-khoai-mon",
            ProductType = "DRINK",
            Description = "Khoai mÃ´n nghiá»n dáº»o bÃ¹i bÃ©o tá»± nhiÃªn hÃ²a trong dÃ²ng sá»¯a thÆ¡m ngon.",
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
            Name = "Sá»¯a tÆ°Æ¡i kem cheese",
            Slug = "sua-tuoi-kem-cheese",
            ProductType = "DRINK",
            Description = "Sá»¯a tÆ°Æ¡i mÃ¡t láº¡nh phá»§ lá»›p kem phÃ´ mai bÃ©o máº·n sÃ¡nh má»‹n bá»“ng bá»nh.",
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
            Name = "Sá»¯a tÆ°Æ¡i trÃ¢n chÃ¢u tráº¯ng",
            Slug = "sua-tuoi-tran-chau-trang",
            ProductType = "DRINK",
            Description = "Sá»¯a tÆ°Æ¡i bÃ©o ngá»t thanh tao cÃ¹ng trÃ¢n chÃ¢u ngá»c trai tráº¯ng giÃ²n sáº§n sáº­t.",
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
            Name = "Sá»¯a tÆ°Æ¡i cacao",
            Slug = "sua-tuoi-cacao",
            ProductType = "DRINK",
            Description = "Cacao nguyÃªn cháº¥t hÃ²a cÃ¹ng sá»¯a tÆ°Æ¡i táº¡o hÆ°Æ¡ng vá»‹ ngá»t Ä‘áº¯ng quyáº¿n rÅ©.",
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
            Name = "Sá»¯a tÆ°Æ¡i caramel",
            Slug = "sua-tuoi-caramel",
            ProductType = "DRINK",
            Description = "Caramel náº¥u bÆ¡ Ä‘Æ°á»ng thÆ¡m ná»“ng hÃ²a tan trong sá»¯a tÆ°Æ¡i mÃ¡t lÃ nh.",
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
            Name = "Sá»¯a tÆ°Æ¡i máº­t ong",
            Slug = "sua-tuoi-mat-ong",
            ProductType = "DRINK",
            Description = "Vá»‹ ngá»t thanh khiáº¿t cá»§a máº­t ong hoa rá»«ng nguyÃªn cháº¥t vÃ  sá»¯a tÆ°Æ¡i dinh dÆ°á»¡ng.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.6 Matcha & Äá»“ Uá»‘ng Cacao ---
        products.Add(new Product
        {
            CategoryId = catMatchaCacao.Id,
            Name = "Matcha latte",
            Slug = "matcha-latte",
            ProductType = "DRINK",
            Description = "Matcha Uji cao cáº¥p Ä‘Ã¡nh bá»t má»‹n mÃ ng cÃ¹ng sá»¯a tÆ°Æ¡i thanh trÃ¹ng nÃ³ng/Ä‘Ã¡.",
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
            Name = "Matcha latte trÃ¢n chÃ¢u",
            Slug = "matcha-latte-tran-chau",
            ProductType = "DRINK",
            Description = "Matcha latte thÆ¡m thanh káº¿t há»£p trÃ¢n chÃ¢u hoÃ ng kim dáº»o ngá»t Ä‘áº­m Ä‘Ã .",
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
            Description = "Vá»‹ chÃ¡t nháº¹ cá»§a matcha hÃ²a quyá»‡n cÃ¹ng lá»›p kem phÃ´ mai bÃ©o ngáº­y máº·n mÃ²i.",
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
            Name = "Matcha latte Ä‘áº­u Ä‘á»",
            Slug = "matcha-latte-dau-do",
            ProductType = "DRINK",
            Description = "Sá»± káº¿t há»£p chuáº©n vá»‹ Nháº­t: Matcha thanh mÃ¡t vÃ  háº¡t Ä‘áº­u Ä‘á» bÃ¹i ngá»t dáº»o thÆ¡m.",
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
            Name = "Matcha dÃ¢u",
            Slug = "matcha-dau",
            ProductType = "DRINK",
            Description = "Hai táº§ng mÃ u xanh - Ä‘á» báº¯t máº¯t, vá»‹ dÃ¢u tÃ¢y ngá»t ngÃ o cÃ¢n báº±ng vá»‹ chÃ¡t matcha.",
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
            Name = "Matcha dá»«a",
            Slug = "matcha-dua",
            ProductType = "DRINK",
            Description = "Matcha Nháº­t Báº£n thÆ¡m dá»‹u káº¿t há»£p nÆ°á»›c cá»‘t dá»«a Báº¿n Tre bÃ©o bÃ¹i thanh mÃ¡t.",
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
            Name = "Matcha Ä‘Ã¡ xay",
            Slug = "matcha-da-xay-cacao",
            ProductType = "DRINK",
            Description = "Matcha xay nhuyá»…n cÃ¹ng Ä‘Ã¡ tuyáº¿t vÃ  sá»¯a, phá»§ ngá»n kem tÆ°Æ¡i whipping cream má»‹n mÃ ng.",
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
            Name = "Cacao sá»¯a Ä‘Ã¡",
            Slug = "cacao-sua-da",
            ProductType = "DRINK",
            Description = "Cacao nguyÃªn cháº¥t Ä‘áº­m Ä‘Ã  sÃ¡nh quyá»‡n cÃ¹ng sá»¯a Ä‘áº·c vÃ  Ä‘Ã¡ mÃ¡t láº¡nh.",
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
            Name = "Cacao nÃ³ng",
            Slug = "cacao-nong",
            ProductType = "DRINK",
            Description = "TÃ¡ch cacao nÃ³ng áº¥m lÃ²ng, thÆ¡m lá»«ng hÆ°Æ¡ng socola xua tan ngÃ y láº¡nh.",
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
            Description = "Cacao Ä‘áº­m Ä‘Ã  sÃ¡nh má»‹n phá»§ lá»›p kem bÃ©o máº·n cheese foam khÃ³ cÆ°á»¡ng.",
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
            Name = "Cacao Ä‘Ã¡ xay",
            Slug = "cacao-da-xay-matcha",
            ProductType = "DRINK",
            Description = "Cacao xay Ä‘Ã¡ tuyáº¿t mÃ¡t láº¡nh sáº£ng khoÃ¡i phá»§ sá»‘t socola vÃ  kem whipping.",
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
            Name = "Socola sá»¯a Ä‘Ã¡",
            Slug = "socola-sua-da",
            ProductType = "DRINK",
            Description = "Socola sá»¯a ngá»t ngÃ o Ãªm dá»‹u, mÃ³n khoÃ¡i kháº©u cá»§a tÃ­n Ä‘á»“ Ä‘á»“ ngá»t.",
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
            Name = "Socola báº¡c hÃ ",
            Slug = "socola-bac-ha",
            ProductType = "DRINK",
            Description = "Vá»‹ ngá»t Ä‘áº¯ng cá»§a socola hÃ²a cÃ¹ng vá»‹ the mÃ¡t sáº£ng khoÃ¡i cá»§a siro báº¡c hÃ .",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.7 Frappe & Äá»“ Uá»‘ng ÄÃ¡ Xay ---
        products.Add(new Product
        {
            CategoryId = catFrappeIceBlended.Id,
            Name = "Matcha Ä‘Ã¡ xay Frappe",
            Slug = "matcha-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Matcha Uji Nháº­t xay nhuyá»…n má»‹n cÃ¹ng sá»¯a vÃ  Ä‘Ã¡ tuyáº¿t, phá»§ kem whipping bÃ©o ngáº­y.",
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
            Name = "Socola Ä‘Ã¡ xay Frappe",
            Slug = "socola-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Socola Ä‘en thÆ°á»£ng háº¡ng xay cÃ¹ng Ä‘Ã¡ tuyáº¿t mÃ¡t láº¡nh ngáº­p trÃ n nÄƒng lÆ°á»£ng.",
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
            Name = "Cacao Ä‘Ã¡ xay Frappe",
            Slug = "cacao-da-xay-frappe",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng cacao ná»“ng nÃ n xay má»‹n sÃ¡nh nhÆ° kem tuyáº¿t mÃ¹a hÃ¨.",
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
            Name = "Cookies and cream Ä‘Ã¡ xay",
            Slug = "cookies-and-cream-da-xay",
            ProductType = "DRINK",
            Description = "BÃ¡nh quy cookies giÃ²n rá»¥m xay cÃ¹ng sá»¯a vani vÃ  phá»§ vá»¥n bÃ¡nh giÃ²n tan.",
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
            Name = "Oreo Ä‘Ã¡ xay Frappe",
            Slug = "oreo-da-xay-frappe",
            ProductType = "DRINK",
            Description = "BÃ¡nh Oreo Ä‘en káº¹p kem xay nhuyá»…n cÃ¹ng kem bÃ©o vÃ  sá»‘t socola Ä‘áº­m Ä‘áº·c.",
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
            Name = "DÃ¢u Ä‘Ã¡ xay Frappe",
            Slug = "dau-da-xay-frappe",
            ProductType = "DRINK",
            Description = "DÃ¢u tÃ¢y tÆ°Æ¡i chua ngá»t xay tuyáº¿t mÃ¡t láº¡nh, mÃ u há»“ng pháº¥n Ä‘Ã¡ng yÃªu.",
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
            Name = "XoÃ i Ä‘Ã¡ xay Frappe",
            Slug = "xoai-da-xay-frappe",
            ProductType = "DRINK",
            Description = "XoÃ i cÃ¡t chÃ­n vÃ ng Æ°Æ¡m xay mÃ¡t láº¡nh cÃ¹ng sá»¯a chua vÃ  Ä‘Ã¡ tuyáº¿t sáº£ng khoÃ¡i.",
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
            Name = "Viá»‡t quáº¥t Ä‘Ã¡ xay Frappe",
            Slug = "viet-quat-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Viá»‡t quáº¥t Báº¯c Má»¹ má»ng nÆ°á»›c xay Ä‘Ã¡ tuyáº¿t giÃ u cháº¥t chá»‘ng lÃ£o hÃ³a.",
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
            Name = "Caramel Ä‘Ã¡ xay Frappe",
            Slug = "caramel-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Sá»‘t caramel náº¥u thÆ¡m bÆ¡ xay cÃ¹ng Ä‘Ã¡ sá»¯a ngá»t ngÃ o Ãªm Ã¡i.",
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
            Name = "CÃ  phÃª Ä‘Ã¡ xay Frappe",
            Slug = "ca-phe-da-xay-frappe",
            ProductType = "DRINK",
            Description = "CÃ  phÃª Robusta Äáº¯k Láº¯k Ä‘áº­m Ä‘Ã  xay Ä‘Ã¡ tuyáº¿t sáº£ng khoÃ¡i tá»‰nh tÃ¡o cáº£ ngÃ y.",
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
            Name = "Mocha Ä‘Ã¡ xay Frappe",
            Slug = "mocha-da-xay-frappe",
            ProductType = "DRINK",
            Description = "Báº£n hÃ²a táº¥u giá»¯a cÃ  phÃª espresso vÃ  socola Ä‘en xay má»‹n phá»§ kem bÃ´ng tuyáº¿t.",
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
            Description = "CÃ  phÃª frappuccino phong cÃ¡ch Ã Ä‘Æ°á»£m vá»‹ caramel bÃ©o ngáº­y vÃ  sá»‘t kem vani.",
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
            Description = "Frappuccino socola thÆ°á»£ng háº¡ng rÆ°á»›i sá»‘t fudge socola Ä‘áº­m Ä‘Ã .",
            BasePrice = 60000,
            ImageUrl = "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.8 NÆ°á»›c Ã‰p TrÃ¡i CÃ¢y ---
        products.Add(new Product
        {
            CategoryId = catFruitJuice.Id,
            Name = "NÆ°á»›c Ã©p cam",
            Slug = "nuoc-ep-cam",
            ProductType = "DRINK",
            Description = "Cam sÃ nh tÆ°Æ¡i váº¯t nguyÃªn cháº¥t má»ng nÆ°á»›c dá»“i dÃ o vitamin C tÄƒng Ä‘á» khÃ¡ng.",
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
            Name = "NÆ°á»›c Ã©p dÆ°a háº¥u",
            Slug = "nuoc-ep-dua-hau",
            ProductType = "DRINK",
            Description = "DÆ°a háº¥u Ä‘á» ngá»t mÃ¡t tá»± nhiÃªn giáº£i nhiá»‡t tá»©c thÃ¬ trong ngÃ y oi bá»©c.",
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
            Name = "NÆ°á»›c Ã©p dá»©a",
            Slug = "nuoc-ep-dua",
            ProductType = "DRINK",
            Description = "Dá»©a chÃ­n thÆ¡m vÃ ng Æ°Æ¡m chua ngá»t tá»± nhiÃªn há»— trá»£ tiÃªu hÃ³a vÃ  giá»¯ dÃ¡ng.",
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
            Name = "NÆ°á»›c Ã©p tÃ¡o",
            Slug = "nuoc-ep-tao",
            ProductType = "DRINK",
            Description = "TÃ¡o New Zealand Ã©p nguyÃªn cháº¥t ngá»t thanh giÃ²n mÃ¡t tá»‘t cho tim máº¡ch.",
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
            Name = "NÆ°á»›c Ã©p cÃ  rá»‘t",
            Slug = "nuoc-ep-ca-rot",
            ProductType = "DRINK",
            Description = "CÃ  rá»‘t tÆ°Æ¡i giÃ u beta-carotene sÃ¡ng máº¯t vÃ  Ä‘áº¹p da má»‹n mÃ ng.",
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
            Name = "NÆ°á»›c Ã©p á»•i",
            Slug = "nuoc-ep-oi",
            ProductType = "DRINK",
            Description = "á»”i ruá»™t há»“ng tÆ°Æ¡i ngon giÃ u vitamin C gáº¥p 4 láº§n cam tÆ°Æ¡i.",
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
            Name = "NÆ°á»›c Ã©p dÃ¢u",
            Slug = "nuoc-ep-dau",
            ProductType = "DRINK",
            Description = "DÃ¢u tÃ¢y ÄÃ  Láº¡t Ã©p nguyÃªn quáº£ mÃ u Ä‘á» tháº¯m quyáº¿n rÅ© vá»‹ chua ngá»t tao nhÃ£.",
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
            Name = "NÆ°á»›c Ã©p xoÃ i",
            Slug = "nuoc-ep-xoai",
            ProductType = "DRINK",
            Description = "XoÃ i cÃ¡t HÃ²a Lá»™c Ã©p má»‹n ngá»t Ä‘áº­m Ä‘Ã  hÆ°Æ¡ng vá»‹ trÃ¡i cÃ¢y nhiá»‡t Ä‘á»›i.",
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
            Name = "NÆ°á»›c Ã©p chanh dÃ¢y",
            Slug = "nuoc-ep-chanh-day",
            ProductType = "DRINK",
            Description = "Chanh dÃ¢y tÆ°Æ¡i lá»c háº¡t chua ngá»t thanh tao kÃ­ch thÃ­ch vá»‹ giÃ¡c.",
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
            Name = "NÆ°á»›c Ã©p cÃ³c",
            Slug = "nuoc-ep-coc",
            ProductType = "DRINK",
            Description = "CÃ³c non chua giÃ²n rá»¥m Ã©p nguyÃªn cháº¥t Ä‘iá»ƒm xuyáº¿t xÃ­u muá»‘i á»›t nháº¹.",
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
            Name = "NÆ°á»›c Ã©p thÆ¡m cÃ  rá»‘t",
            Slug = "nuoc-ep-thom-ca-rot",
            ProductType = "DRINK",
            Description = "Sá»± káº¿t há»£p hoÃ n háº£o giá»¯a vá»‹ ngá»t thanh cÃ  rá»‘t vÃ  chua nháº¹ cá»§a thÆ¡m.",
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
            Name = "NÆ°á»›c Ã©p cam cÃ  rá»‘t",
            Slug = "nuoc-ep-cam-ca-rot",
            ProductType = "DRINK",
            Description = "Bá»™ Ä‘Ã´i cam vÃ ng vÃ  cÃ  rá»‘t giÃ u vitamin bá»• sung nÄƒng lÆ°á»£ng tÆ°Æ¡i má»›i.",
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
            Name = "NÆ°á»›c Ã©p tÃ¡o dá»©a",
            Slug = "nuoc-ep-tao-dua",
            ProductType = "DRINK",
            Description = "TÃ¡o ngá»t dá»‹u quyá»‡n dá»©a thÆ¡m chua thanh táº¡o hÆ°Æ¡ng vá»‹ thanh lá»c tuyá»‡t vá»i.",
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
            Name = "NÆ°á»›c Ã©p dÆ°a háº¥u dá»©a",
            Slug = "nuoc-ep-dua-hau-dua",
            ProductType = "DRINK",
            Description = "DÆ°a háº¥u ngá»t nÆ°á»›c phá»‘i dá»©a nhiá»‡t Ä‘á»›i giáº£i nhiá»‡t sáº£ng khoÃ¡i.",
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
            Name = "NÆ°á»›c Ã©p trÃ¡i cÃ¢y há»—n há»£p",
            Slug = "nuoc-ep-trai-cay-hon-hop",
            ProductType = "DRINK",
            Description = "CÃ´ng thá»©c detox tá»•ng há»£p cam, tÃ¡o, cÃ  rá»‘t vÃ  dá»©a tÆ°Æ¡i thanh lá»c cÆ¡ thá»ƒ.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.9 Soda & Äá»“ Uá»‘ng CÃ³ Gas ---
        products.Add(new Product
        {
            CategoryId = catSodaGas.Id,
            Name = "Soda chanh",
            Slug = "soda-chanh",
            ProductType = "DRINK",
            Description = "Soda chanh tÆ°Æ¡i sá»§i bá»t mÃ¡t láº¡nh cá»• Ä‘iá»ƒn giáº£i nhiá»‡t cá»±c nhanh.",
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
            Name = "Soda viá»‡t quáº¥t",
            Slug = "soda-viet-quat",
            ProductType = "DRINK",
            Description = "MÃ u tÃ­m biáº¿c huyá»n áº£o, vá»‹ viá»‡t quáº¥t ngá»t thÆ¡m hÃ²a cÃ¹ng bá»t ga sáº£ng khoÃ¡i.",
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
            Name = "Soda dÃ¢u",
            Slug = "soda-dau",
            ProductType = "DRINK",
            Description = "Soda dÃ¢u tÃ¢y Ä‘á» tÆ°Æ¡i chua ngá»t dá»‹u dÃ ng Ä‘iá»ƒm xuyáº¿t lÃ¡ báº¡c hÃ .",
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
            Name = "Soda Ä‘Ã o",
            Slug = "soda-dao",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng Ä‘Ã o thÆ¡m ná»©c quyá»‡n cÃ¹ng bá»t ga sá»§i tÄƒm kÃ­ch thÃ­ch vá»‹ giÃ¡c.",
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
            Description = "MÃ u xanh ngá»c bÃ­ch tá»« kiwi tÆ°Æ¡i chua thanh mÃ¡t rÆ°á»£i.",
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
            Name = "Soda chanh dÃ¢y",
            Slug = "soda-chanh-day",
            ProductType = "DRINK",
            Description = "Chanh dÃ¢y tÆ°Æ¡i chua ngá»t hÃ²a cÃ¹ng soda bá»t ga bá»«ng tá»‰nh tá»©c thÃ¬.",
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
            Name = "Soda báº¡c hÃ ",
            Slug = "soda-bac-ha",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng báº¡c hÃ  xanh mÃ¡t the the láº¡nh buá»‘t cuá»‘n hÃºt má»i Ã¡nh nhÃ¬n.",
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
            Description = "Cam vÃ ng sá»§i bá»t trÃ n Ä‘áº§y sá»©c sá»‘ng vÃ  vitamin C.",
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
            Name = "Soda tÃ¡o xanh",
            Slug = "soda-tao-xanh",
            ProductType = "DRINK",
            Description = "TÃ¡o xanh giÃ²n ngá»t káº¿t há»£p soda ga tÆ°Æ¡i mÃ¡t tráº» trung.",
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
            Name = "Soda nhiá»‡t Ä‘á»›i",
            Slug = "soda-nhiet-doi",
            ProductType = "DRINK",
            Description = "Há»—n há»£p trÃ¡i cÃ¢y nhiá»‡t Ä‘á»›i thÆ¡m mÃ¡t bÃ¹ng ná»• cÃ¹ng bá»t ga láº¥p lÃ¡nh.",
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
            Name = "Soda xoÃ i",
            Slug = "soda-xoai",
            ProductType = "DRINK",
            Description = "XoÃ i chÃ­n vÃ ng ngá»t ngÃ o káº¿t há»£p bá»t ga mÃ¡t láº¡nh sáº£ng khoÃ¡i.",
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
            Name = "Soda váº£i",
            Slug = "soda-vai",
            ProductType = "DRINK",
            Description = "HÆ°Æ¡ng váº£i ngá»t dá»‹u quÃ½ phÃ¡i trong tá»«ng ngá»¥m soda sá»§i tÄƒm.",
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
            Name = "Soda dÆ°a lÆ°á»›i",
            Slug = "soda-dua-luoi",
            ProductType = "DRINK",
            Description = "DÆ°a lÆ°á»›i thanh tao ngá»t mÃ¡t sáº£ng khoÃ¡i ngÃ y hÃ¨.",
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
            Description = "Sáº¯c xanh Ä‘áº¡i dÆ°Æ¡ng sÃ¢u tháº³m tá»« siro Blue Curacao cam Ä‘áº¯ng vÃ  soda sáº£ng khoÃ¡i.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.10 NÆ°á»›c Chanh & Äá»“ Uá»‘ng Giáº£i KhÃ¡t ---
        products.Add(new Product
        {
            CategoryId = catLemonRefreshment.Id,
            Name = "Chanh tÆ°Æ¡i",
            Slug = "chanh-tuoi-da-duong",
            ProductType = "DRINK",
            Description = "NÆ°á»›c chanh tÆ°Æ¡i Ä‘Ã¡ Ä‘Æ°á»ng truyá»n thá»‘ng thanh mÃ¡t bÃ¬nh dá»‹.",
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
            Name = "Chanh máº­t ong",
            Slug = "chanh-mat-ong-giai-khat",
            ProductType = "DRINK",
            Description = "Chanh tÆ°Æ¡i káº¿t há»£p máº­t ong rá»«ng nguyÃªn cháº¥t ngá»t thanh áº¥m Ã¡p.",
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
            Name = "Chanh dÃ¢y",
            Slug = "chanh-day-da-duong",
            ProductType = "DRINK",
            Description = "NÆ°á»›c cá»‘t chanh dÃ¢y thÆ¡m lá»«ng háº¡t giÃ²n ngá»t ngÃ o giáº£i khÃ¡t.",
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
            Name = "Táº¯c máº­t ong",
            Slug = "tac-mat-ong-giai-khat",
            ProductType = "DRINK",
            Description = "Táº¯c thÆ¡m mÃ¹i tinh dáº§u quáº¥t hÃ²a cÃ¹ng máº­t ong hoa cÃ  phÃª thanh ngá»t.",
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
            Name = "Táº¯c xÃ­ muá»™i",
            Slug = "tac-xi-muoi",
            ProductType = "DRINK",
            Description = "Vá»‹ máº·n ngá»t chua cay hÃ i hÃ²a cá»§a xÃ­ muá»™i Báº¯c ngÃ¢m táº¯c giáº£i cáº£m thanh giá»ng.",
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
            Name = "Chanh muá»‘i",
            Slug = "chanh-muoi-lau-nam",
            ProductType = "DRINK",
            Description = "Chanh muá»‘i lÃ¢u nÄƒm ngÃ¢m máº­t Ä‘Æ°á»ng, bÃ¹ Ä‘iá»‡n giáº£i cá»±c tá»‘t sau khi váº­n Ä‘á»™ng.",
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
            Name = "Chanh sáº£",
            Slug = "chanh-sa-giai-nhiet",
            ProductType = "DRINK",
            Description = "Chanh tÆ°Æ¡i thÆ¡m lá»«ng hÆ°Æ¡ng sáº£ Ä‘áº­p dáº­p the mÃ¡t thanh lá»c cÆ¡ thá»ƒ.",
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
            Name = "NÆ°á»›c mÆ¡",
            Slug = "nuoc-mo-chua-huong",
            ProductType = "DRINK",
            Description = "MÆ¡ ngÃ¢m chÃ¹a HÆ°Æ¡ng chua ngá»t Ä‘áº­m Ä‘Ã  phong vá»‹ HÃ  thÃ nh xÆ°a.",
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
            Name = "NÆ°á»›c sáº¥u",
            Slug = "nuoc-sau-ngam-gung",
            ProductType = "DRINK",
            Description = "Quáº£ sáº¥u giÃ²n sáº§n sáº­t ngÃ¢m gá»«ng á»›t chua cay ngá»t dá»‹u mÃ¡t ruá»™t.",
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
            Name = "NÆ°á»›c nha Ä‘am",
            Slug = "nuoc-nha-dam-duong-phen",
            ProductType = "DRINK",
            Description = "Tháº¡ch nha Ä‘am tÆ°Æ¡i giÃ²n mÃ¡t thanh nhiá»‡t lÃ m mÃ¡t cÆ¡ thá»ƒ tá»©c thÃ¬.",
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
            Name = "NÆ°á»›c dá»«a tÆ°Æ¡i",
            Slug = "nuoc-dua-tuoi-ben-tre",
            ProductType = "DRINK",
            Description = "Dá»«a xiÃªm Báº¿n Tre ngá»t nÆ°á»›c thanh khiáº¿t nguyÃªn quáº£ tá»± nhiÃªn.",
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
            Name = "NÆ°á»›c dá»«a táº¯c",
            Slug = "nuoc-dua-tac-sai-gon",
            ProductType = "DRINK",
            Description = "NÆ°á»›c dá»«a xiÃªm ngá»t mÃ¡t thÃªm chÃºt nÆ°á»›c cá»‘t táº¯c thÆ¡m dá»‹u vÃ  cÃ¹i dá»«a non giÃ²n.",
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
            Name = "NÆ°á»›c Ã©p chanh dÃ¢y máº­t ong",
            Slug = "nuoc-ep-chanh-day-mat-ong",
            ProductType = "DRINK",
            Description = "Sá»± hÃ²a quyá»‡n sÃ¡nh má»‹n giá»¯a chanh dÃ¢y vÃ  máº­t ong hoa rá»«ng nguyÃªn cháº¥t.",
            BasePrice = 40000,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1
        });
        // --- 5.1.11 CÃ  PhÃª & Äá»“ Uá»‘ng Bá»• Sung ---
        products.Add(new Product
        {
            CategoryId = catCoffee.Id,
            Name = "CÃ  phÃª Ä‘en",
            Slug = "ca-phe-den-da",
            ProductType = "DRINK",
            Description = "CÃ  phÃª phin truyá»n thá»‘ng Ä‘áº­m Ä‘Ã  vá»‹ Ä‘áº¯ng nguyÃªn báº£n vÃ  háº­u vá»‹ ngá»t sÃ¢u.",
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
            Name = "CÃ  phÃª sá»¯a",
            Slug = "ca-phe-sua-da-sai-gon",
            ProductType = "DRINK",
            Description = "CÃ  phÃª sá»¯a Ä‘Ã¡ SÃ i GÃ²n thÆ¡m ná»“ng, Ä‘áº­m Ä‘Ã  vá»‹ ngá»t bÃ©o cá»§a sá»¯a Ä‘áº·c hÃ²a cÃ¹ng cÃ  phÃª Ä‘áº­m.",
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
            Name = "Báº¡c xá»‰u",
            Slug = "bac-xiu-da-ngot-ngao",
            ProductType = "DRINK",
            Description = "Nhiá»u sá»¯a Ã­t cÃ  phÃª, bÃ©o ngáº­y ngá»t ngÃ o thÃ­ch há»£p cho ngÆ°á»i thÃ­ch cÃ  phÃª nháº¹ nhÃ ng.",
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
            Name = "CÃ  phÃª muá»‘i",
            Slug = "ca-phe-muoi-hue",
            ProductType = "DRINK",
            Description = "CÃ  phÃª Ä‘áº­m vá»‹ nÃ¢ng Ä‘á»¡ lá»›p kem muá»‘i bÃ©o ngáº­y máº·n mÃ  phong cÃ¡ch xá»© Huáº¿.",
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
            Description = "CÃ  phÃª espresso Ã hÃ²a cÃ¹ng sá»¯a nÃ³ng Ä‘Ã¡nh bá»t má»‹n mÃ ng nghá»‡ thuáº­t latte art.",
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
            Description = "Lá»›p bá»t sá»¯a dÃ y xá»‘p phá»§ bá»™t quáº¿ cacao thÆ¡m ná»“ng trÃªn ná»n cÃ  phÃª espresso.",
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
            Description = "Espresso pha loÃ£ng vá»›i nÆ°á»›c thanh khiáº¿t, thÆ¡m ná»“ng dá»‹u Ãªm khÃ´ng gáº¯t.",
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
            Description = "Sá»± káº¿t há»£p ngá»t ngÃ o giá»¯a espresso, socola Ä‘en vÃ  sá»¯a nÃ³ng sÃ¡nh má»‹n.",
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
            Description = "Sá»¯a tÆ°Æ¡i Ä‘Ã¡nh bá»t, cÃ  phÃª espresso vÃ  nhá»¯ng vá»‡t sá»‘t caramel Ã³ng áº£ ngá»t ngÃ o.",
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
            Description = "Má»™t shot cÃ  phÃª Ä‘áº­m Ä‘áº·c chiáº¿t xuáº¥t Ã¡p suáº¥t cao vá»›i lá»›p crema vÃ ng dÃ y má»‹n.",
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
            Name = "CÃ  phÃª cá»‘t dá»«a",
            Slug = "ca-phe-cot-dua-da-xay",
            ProductType = "DRINK",
            Description = "CÃ  phÃª Ä‘áº­m Ä‘Ã  hÃ²a cÃ¹ng nÆ°á»›c cá»‘t dá»«a xay Ä‘Ã¡ tuyáº¿t bÃ©o bÃ¹i thÆ¡m lá»«ng.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1
        });

        // --- 5.2 Category 2.1: CÃ¡ viÃªn, bÃ² viÃªn & cÃ¡c loáº¡i viÃªn chiÃªn ---
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "CÃ¡ ViÃªn ChiÃªn Tháº­p Cáº©m Máº¹t Lá»›n",
            Slug = "ca-vien-chien-thap-cam-met-lon",
            ProductType = "FOOD",
            Description = "Máº¹t viÃªn chiÃªn ngáº­p trÃ n cÃ¡ viÃªn, bÃ² viÃªn, tÃ´m viÃªn, há»“ lÃ´, xÃºc xÃ­ch vÃ  cháº£ giÃ² mini chiÃªn rÃ¡o dáº§u vÃ ng Ã³ng, Äƒn kÃ¨m dÆ°a leo giÃ²n vÃ  tÆ°Æ¡ng Ä‘en xÃ o Ä‘áº·c biá»‡t.",
            BasePrice = 65000,
            ImageUrl = "/images/menu/ca-vien-chien-thap-cam-met-lon.jpg",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng Ä‘en & TÆ°Æ¡ng á»›t xÃ o tá»i",
            PortionDescription = "1 máº¹t (khoáº£ng 25 viÃªn Ä‘á»§ 2-3 ngÆ°á»i Äƒn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "CÃ¡ ViÃªn ChiÃªn Truyá»n Thá»‘ng",
            Slug = "ca-vien-chien-truyen-thong",
            ProductType = "FOOD",
            Description = "CÃ¡ viÃªn chiÃªn giÃ²n thÆ¡m, dai ngá»t tá»± nhiÃªn, phá»“ng trÃ²n cháº¥m tÆ°Æ¡ng á»›t hoáº·c tÆ°Æ¡ng Ä‘en Ä‘áº­m vá»‹.",
            BasePrice = 25000,
            ImageUrl = "/images/menu/ca-vien-chien-truyen-thong.jpg",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t & tÆ°Æ¡ng Ä‘en",
            PortionDescription = "1 pháº§n (10 viÃªn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "BÃ² ViÃªn ChiÃªn GÃ¢n GiÃ²n",
            Slug = "bo-vien-chien-gan-gion",
            ProductType = "FOOD",
            Description = "BÃ² viÃªn gÃ¢n giÃ²n sáº§n sáº­t, thÆ¡m ngáº­y mÃ¹i tiÃªu Ä‘en vÃ  bÃ² tÆ°Æ¡i chiÃªn nÃ³ng sá»‘t.",
            BasePrice = 32000,
            ImageUrl = "/images/menu/bo-vien-chien-gan-gion.jpg",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng Ä‘en sa táº¿",
            PortionDescription = "1 pháº§n (8 viÃªn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "TÃ´m ViÃªn ChiÃªn GiÃ²n Rá»¥m",
            Slug = "tom-vien-chien-gion-rum",
            ProductType = "FOOD",
            Description = "Thá»‹t tÃ´m tÆ°Æ¡i xay nhuyá»…n táº¡o nÃªn viÃªn tÃ´m thÆ¡m ngáº­y vá»‹ ngá»t háº£i sáº£n.",
            BasePrice = 30000,
            ImageUrl = "/images/menu/tom-vien-chien-gion-rum.jpg",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sá»‘t bÆ¡ trá»©ng & tÆ°Æ¡ng á»›t",
            PortionDescription = "1 pháº§n (8 viÃªn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Má»±c ViÃªn ChiÃªn NÃ³ng",
            Slug = "muc-vien-chien-nong",
            ProductType = "FOOD",
            Description = "Má»±c viÃªn dai ngá»t Ä‘áº­m Ä‘Ã  vá»‹ biá»ƒn, chiÃªn vÃ ng giÃ²n rÃ¡o dáº§u.",
            BasePrice = 32000,
            ImageUrl = "/images/menu/muc-vien-chien-nong.jpg",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t cay ná»“ng",
            PortionDescription = "1 pháº§n (8 viÃªn)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "Há»“ LÃ´ NÆ°á»›ng / ChiÃªn GiÃ²n",
            Slug = "ho-lo-nuong-chien-gion",
            ProductType = "FOOD",
            Description = "Há»“ lÃ´ thá»‹t tÆ°Æ¡i ngá»t thÆ¡m má»¡ mÃ ng, chiÃªn giÃ²n vá» bÃªn ngoÃ i má»ng nÆ°á»›c bÃªn trong.",
            BasePrice = 35000,
            ImageUrl = "/images/menu/ho-lo-nuong-chien-gion.jpg",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng Ä‘en & tÆ°Æ¡ng á»›t xÃ o sa táº¿",
            PortionDescription = "1 pháº§n (8 viÃªn há»“ lÃ´)"
        });
        products.Add(new Product
        {
            CategoryId = catVienChien.Id,
            Name = "PhÃ´ Mai ViÃªn ChiÃªn GiÃ²n Tan",
            Slug = "pho-mai-vien-chien-gion-tan",
            ProductType = "FOOD",
            Description = "Lá»›p vá» ngoÃ i giÃ²n rá»¥m bao bá»c nhÃ¢n phÃ´ mai bÃ©o ngáº­y cháº£y trÃ n khi cáº¯n.",
            BasePrice = 38000,
            ImageUrl = "/images/menu/pho-mai-vien-chien-gion-tan.jpg",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sá»‘t bÆ¡ tá»i & tÆ°Æ¡ng á»›t ngá»t",
            PortionDescription = "1 pháº§n (6 viÃªn)"
        });

        // --- 5.3 Category 2.2: GÃ  vÃ  cÃ¡c mÃ³n chÃ¢n gÃ  ---
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "ChÃ¢n GÃ  RÃºt XÆ°Æ¡ng Sá»‘t ThÃ¡i Cay GiÃ²n",
            Slug = "chan-ga-rut-xuong-sot-thai-cay-gion",
            ProductType = "FOOD",
            Description = "ChÃ¢n gÃ  tÆ°Æ¡i rÃºt xÆ°Æ¡ng giÃ²n sáº§n sáº­t tháº¥m Ä‘áº«m sá»‘t ThÃ¡i cay ná»“ng Ä‘áº­m vá»‹, xoÃ i xanh non, cÃ³c bao tá»­ vÃ  sáº£ táº¯c ngáº­p trÃ n hÆ°Æ¡ng thÆ¡m quyáº¿n rÅ©.",
            BasePrice = 69000,
            ImageUrl = "/images/menu/chan-ga-rut-xuong-sot-thai-cay-gion.jpg",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            AllowSauceCustomization = true,
            DefaultSauce = "NÆ°á»›c sá»‘t ThÃ¡i Ä‘áº·c cháº¿ chua cay",
            PortionDescription = "1 pháº§n (khoáº£ng 350g kÃ¨m xoÃ i cÃ³c bao tá»­)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "ChÃ¢n GÃ  NgÃ¢m Sáº£ Táº¯c Truyá»n Thá»‘ng",
            Slug = "chan-ga-ngam-sa-tac-truyen-thong",
            ProductType = "FOOD",
            Description = "ChÃ¢n gÃ  ngÃ¢m sáº£ táº¯c giÃ²n dai sáº§n sáº­t, hÆ°Æ¡ng thÆ¡m sáº£ cÃ¢y, lÃ¡ chanh vÃ  vá»‹ chua thanh mÃ¡t cá»§a táº¯c quáº£.",
            BasePrice = 65000,
            ImageUrl = "/images/menu/chan-ga-ngam-sa-tac-truyen-thong.jpg",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Muá»‘i tiÃªu chanh á»›t xanh",
            PortionDescription = "1 pháº§n (khoáº£ng 350g)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "CÃ¡nh GÃ  ChiÃªn NÆ°á»›c Máº¯m Tá»i á»št",
            Slug = "canh-ga-chien-nuoc-mam-toi-ot",
            ProductType = "FOOD",
            Description = "CÃ¡nh gÃ  chiÃªn giÃ²n tan phá»§ Ä‘á»u lá»›p nÆ°á»›c máº¯m tá»i á»›t káº¹o ngá»t thÆ¡m ná»©c mÅ©i, Ä‘áº­m Ä‘Ã  khÃ³ cÆ°á»¡ng.",
            BasePrice = 58000,
            ImageUrl = "/images/menu/canh-ga-chien-nuoc-mam-toi-ot.jpg",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            DefaultSauce = "Sá»‘t máº¯m tá»i káº¹o",
            PortionDescription = "1 pháº§n (3 cÃ¡nh lá»›n)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "GÃ  Láº¯c PhÃ´ Mai BÃ©o Ngáº­y",
            Slug = "ga-lac-pho-mai-beo-ngay",
            ProductType = "FOOD",
            Description = "Thá»‹t gÃ  á»©c phi lÃª cáº¯t miáº¿ng chiÃªn vÃ ng rá»¥m láº¯c ngáº­p bá»™t phÃ´ mai bÃ©o ngá»t thÆ¡m phá»©c.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t chua ngá»t & Sá»‘t mayonnaise",
            PortionDescription = "1 pháº§n (200g thá»‹t gÃ  viÃªn)"
        });
        products.Add(new Product
        {
            CategoryId = catGaChanGa.Id,
            Name = "GÃ  Popcorn ChiÃªn GiÃ²n",
            Slug = "ga-popcorn-chien-gion",
            ProductType = "FOOD",
            Description = "GÃ  viÃªn popcorn giÃ²n rá»¥m cáº¯n vui tai, bÃªn trong má»ng nÆ°á»›c má»m thÆ¡m.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng cÃ  & tÆ°Æ¡ng á»›t",
            PortionDescription = "1 pháº§n (khoáº£ng 180g)"
        });

        // --- 5.4 Category 2.3: Khoai tÃ¢y & khoai lang ---
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai TÃ¢y Láº¯c PhÃ´ Mai Äáº·c Biá»‡t",
            Slug = "khoai-tay-lac-pho-mai-dac-biet",
            ProductType = "FOOD",
            Description = "Khoai tÃ¢y sá»£i vÃ ng Æ°Æ¡m nháº­p kháº©u chiÃªn giÃ²n tan, phá»§ lá»›p bá»™t phÃ´ mai HÃ n Quá»‘c thÆ¡m lá»«ng ngáº­y bÃ©o khÃ³ quÃªn.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t & Mayonnaise",
            PortionDescription = "1 pháº§n tiÃªu chuáº©n (250g)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai TÃ¢y Láº¯c Rong Biá»ƒn ThÆ¡m GiÃ²n",
            Slug = "khoai-tay-lac-rong-bien-thom-gion",
            ProductType = "FOOD",
            Description = "Sá»± káº¿t há»£p giá»¯a vá»‹ máº±n máº·n cá»§a rong biá»ƒn nÆ°á»›ng vÃ  Ä‘á»™ giÃ²n rá»¥m cá»§a khoai tÃ¢y sá»£i tÆ°Æ¡i.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t cay",
            PortionDescription = "1 pháº§n (250g)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai Lang KÃ©n NÆ°á»›c Cá»‘t Dá»«a",
            Slug = "khoai-lang-ken-nuoc-cot-dua",
            ProductType = "FOOD",
            Description = "Khoai lang kÃ©n bÃ¹i ngá»t, dáº»o thÆ¡m thoang thoáº£ng mÃ¹i nÆ°á»›c cá»‘t dá»«a vÃ  mÃ¨ Ä‘en bÃ©o ngáº­y.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = false,
            DefaultSauce = "TÆ°Æ¡ng á»›t",
            PortionDescription = "1 pháº§n (12 kÃ©n)"
        });
        products.Add(new Product
        {
            CategoryId = catKhoai.Id,
            Name = "Khoai TÃ¢y ChiÃªn Sá»‘t PhÃ´ Mai Tan Cháº£y",
            Slug = "khoai-tay-chien-sot-pho-mai-tan-chay",
            ProductType = "FOOD",
            Description = "ÄÄ©a khoai tÃ¢y chiÃªn nÃ³ng há»•i rÆ°á»›i ngáº­p sá»‘t cheddar cheese tan cháº£y bÃ©o máº·n thÆ¡m ngáº¥t ngÃ¢y.",
            BasePrice = 42000,
            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Sá»‘t phÃ´ mai cheddar tan cháº£y",
            PortionDescription = "1 pháº§n lá»›n"
        });

        // --- 5.5 Category 2.4: PhÃ´ mai, xÃºc xÃ­ch & Ä‘á»“ chiÃªn khÃ¡c ---
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "PhÃ´ Mai Que KÃ©o Sá»£i Khá»•ng Lá»“",
            Slug = "pho-mai-que-keo-soi-khong-lo",
            ProductType = "FOOD",
            Description = "PhÃ´ mai que Mozzarella háº£o háº¡ng chiÃªn xÃ¹ vÃ ng ruá»™m, kÃ©o sá»£i dÃ i miÃªn man bÃ©o thÆ¡m ná»©c mÅ©i.",
            BasePrice = 36000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng cÃ  & Mayonnaise chua ngá»t",
            PortionDescription = "1 pháº§n (4 que lá»›n)"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "XÃºc XÃ­ch Lá»‘c XoÃ¡y ChiÃªn GiÃ²n",
            Slug = "xuc-xich-loc-xoay-chien-gion",
            ProductType = "FOOD",
            Description = "XÃºc xÃ­ch Äá»©c xÃ´ng khÃ³i cáº¯t lá»‘c xoÃ¡y táº©m bá»™t chiÃªn giÃ²n rá»¥m bÃªn ngoÃ i má»ng nÆ°á»›c bÃªn trong.",
            BasePrice = 28000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t & tÆ°Æ¡ng cÃ ",
            PortionDescription = "1 cÃ¢y lá»›n"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "Nem Chua RÃ¡n HÃ  Ná»™i Chuáº©n Vá»‹",
            Slug = "nem-chua-ran-ha-noi-chuan-vi",
            ProductType = "FOOD",
            Description = "Nem chua rÃ¡n HÃ  Ná»™i lÄƒn bá»™t chiÃªn xÃ¹ giÃ²n rá»¥m bÃªn ngoÃ i dáº»o dÃ­nh bÃªn trong, Äƒn kÃ¨m dÆ°a chuá»™t tÆ°Æ¡i vÃ  tÆ°Æ¡ng á»›t cay ngá»t.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t HÃ  Ná»™i chua cay",
            PortionDescription = "1 pháº§n (6 chiáº¿c)"
        });
        products.Add(new Product
        {
            CategoryId = catPhoMaiXucXich.Id,
            Name = "HÃ¡ Cáº£o TÃ´m Thá»‹t ChiÃªn GiÃ²n",
            Slug = "ha-cao-tom-thit-chien-gion",
            ProductType = "FOOD",
            Description = "HÃ¡ cáº£o nhÃ¢n tÃ´m thá»‹t Ä‘áº­m vá»‹ gÃ³i vá» má»ng chiÃªn vÃ ng giÃ²n rá»¥m, cháº¥m giáº¥m Ä‘en hoáº·c tÆ°Æ¡ng á»›t sa táº¿.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "NÆ°á»›c tÆ°Æ¡ng giáº¥m Ä‘á» xÃ o sa táº¿",
            PortionDescription = "1 pháº§n (8 chiáº¿c)"
        });

        // --- 5.6 Category 2.5: BÃ¡nh trÃ¡ng & cÃ¡c mÃ³n trá»™n ---
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "BÃ¡nh TrÃ¡ng Trá»™n Milkyverse Äáº·c Biá»‡t",
            Slug = "banh-trang-tron-milkyverse-dac-biet",
            ProductType = "FOOD",
            Description = "BÃ¡nh trÃ¡ng phÆ¡i sÆ°Æ¡ng dáº»o má»m, ngáº­p trÃ n topping: bÃ² khÃ´ miáº¿ng, khÃ´ gÃ  lÃ¡ chanh, tÃ©p sáº¥y, trá»©ng cÃºt lÃ²ng Ä‘Ã o, xoÃ i chua thÃ¡i sá»£i, rau rÄƒm vÃ  nÆ°á»›c sá»‘t bÃ² sa táº¿ thÆ¡m cay bÃ­ truyá»n.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            AllowSauceCustomization = true,
            DefaultSauce = "NÆ°á»›c sá»‘t bÃ² sa táº¿ Ä‘áº·c biá»‡t",
            PortionDescription = "1 pháº§n lá»›n ngáº­p topping"
        });
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "BÃ¡nh TrÃ¡ng NÆ°á»›ng ÄÃ  Láº¡t Trá»©ng Thá»‹t XÃºc XÃ­ch",
            Slug = "banh-trang-nuong-da-lat-trung-thit-xuc-xich",
            ProductType = "FOOD",
            Description = "'Pizza Viá»‡t Nam' nÆ°á»›ng than há»“ng giÃ²n rá»¥m, pháº¿t trá»©ng gÃ  bÃ©o ngáº­y, hÃ nh hoa, ruá»‘c thá»‹t, xÃºc xÃ­ch vÃ  sá»‘t mayonnaise bÆ¡ trá»©ng.",
            BasePrice = 30000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Mayonnaise & tÆ°Æ¡ng á»›t nÆ°á»›ng",
            PortionDescription = "1 chiáº¿c bÃ¡nh lá»›n"
        });
        products.Add(new Product
        {
            CategoryId = catBanhTrang.Id,
            Name = "BÃ¡nh TrÃ¡ng Cuá»‘n BÆ¡ Sá»‘t Me Táº¯c",
            Slug = "banh-trang-cuon-bo-sot-me-tac",
            ProductType = "FOOD",
            Description = "Tá»«ng cuá»™n bÃ¡nh trÃ¡ng dáº»o thÆ¡m cuá»‘n trá»©ng cÃºt, khÃ´ bÃ², tÃ©p sáº¥y, hÃ nh phi giÃ²n tan ngáº­p trong sá»‘t bÆ¡ bÃ©o ngáº­y vÃ  sá»‘t me chua cay.",
            BasePrice = 35000,
            ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            AllowSauceCustomization = true,
            DefaultSauce = "Sá»‘t bÆ¡ trá»©ng & sá»‘t me cay",
            PortionDescription = "1 pháº§n (8 cuá»‘n cáº¯t khÃºc)"
        });

        // --- 5.7 Category 2.6: Trá»©ng chÃ©n & mÃ³n trá»©ng ---
        products.Add(new Product
        {
            CategoryId = catTrungChen.Id,
            Name = "Trá»©ng ChÃ©n NÆ°á»›ng PhÃ´ Mai XÃºc XÃ­ch Pate",
            Slug = "trung-chen-nuong-pho-mai-xuc-xich-pate",
            ProductType = "FOOD",
            Description = "ChÃ©n sá»© nÆ°á»›ng than hoa nÃ³ng bá»ng, 4 quáº£ trá»©ng cÃºt tÆ°Æ¡i phá»§ Ä‘áº§y phÃ´ mai Mozzarella kÃ©o sá»£i, xÃºc xÃ­ch xÃ´ng khÃ³i, pate thÆ¡m lá»«ng vÃ  má»¡ hÃ nh bÃ©o ngáº­y.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "NÆ°á»›c sá»‘t chua ngá»t & muá»‘i tiÃªu táº¯c",
            PortionDescription = "1 chÃ©n nÆ°á»›ng nÃ³ng há»•i"
        });
        products.Add(new Product
        {
            CategoryId = catTrungChen.Id,
            Name = "Trá»©ng CÃºt Lá»™n XÃ o Me Chua Ngá»t",
            Slug = "trung-cut-lon-xao-me-chua-ngot",
            ProductType = "FOOD",
            Description = "Trá»©ng cÃºt lá»™n bÃ¹i bÃ©o Ä‘áº£o trong nÆ°á»›c sá»‘t me káº¹o chua ngá»t sÃ¡nh má»‹n, ráº¯c thÃªm Ä‘áº­u phá»™ng rang thÆ¡m ná»©c vÃ  rau rÄƒm.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            DefaultSauce = "Sá»‘t me chua ngá»t sÃ¡nh Ä‘áº·c",
            PortionDescription = "1 Ä‘Ä©a (15 quáº£)"
        });

        // --- 5.8 Category 2.7: MÃ¬ & mÃ³n cay 7 cáº¥p Ä‘á»™ ---
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "MÃ¬ Cay HÃ n Quá»‘c 7 Cáº¥p Äá»™ Háº£i Sáº£n Tháº­p Cáº©m",
            Slug = "mi-cay-han-quoc-7-cap-do-hai-san-thap-cam",
            ProductType = "FOOD",
            Description = "Thá»‘ Ä‘Ã¡ sÃ´i sá»¥c nghi ngÃºt khÃ³i vá»›i mÃ¬ sá»£i HÃ n Quá»‘c dai giÃ²n, tÃ´m sÃº tÆ°Æ¡i, má»±c khoanh giÃ²n ngá»t, bÃ² Má»¹ cuá»™n, náº¥m kim chÃ¢m vÃ  báº¯p cáº£i tÃ­m trong nÆ°á»›c sÃºp kim chi cay tÃª tÃ¡i chuáº©n 7 cáº¥p Ä‘á»™.",
            BasePrice = 69000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            PortionDescription = "1 thá»‘ Ä‘Ã¡ lá»›n Ä‘áº§y Ä‘á»§ tÃ´m, má»±c, bÃ² Má»¹, xÃºc xÃ­ch"
        });
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "MÃ¬ Cay BÃ² Má»¹ Cuá»™n Náº¥m Kim ChÃ¢m",
            Slug = "mi-cay-bo-my-cuon-nam-kim-cham",
            ProductType = "FOOD",
            Description = "NÆ°á»›c dÃ¹ng cay chua ngá»t kim chi cay xÃ© lÆ°á»¡i hÃ²a quyá»‡n cÃ¹ng tá»«ng dáº£i thá»‹t bÃ² Má»¹ má»m tan vÃ  náº¥m kim chÃ¢m giÃ²n sáº§n sáº­t.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            PortionDescription = "1 thá»‘ Ä‘Ã¡ bÃ² Má»¹ háº£o háº¡ng"
        });
        products.Add(new Product
        {
            CategoryId = catMiCay.Id,
            Name = "MÃ¬ Trá»™n XÃºc XÃ­ch PhÃ´ Mai Trá»©ng LÃ²ng ÄÃ o",
            Slug = "mi-tron-xuc-xich-pho-mai-trung-long-dao",
            ProductType = "FOOD",
            Description = "MÃ¬ trá»™n Indomie sá»‘t cay Ä‘áº­m Ä‘Ã , phá»§ trá»©ng lÃ²ng Ä‘Ã o bÃ©o ngáº­y, xÃºc xÃ­ch rÃ¡n vÃ  phÃ´ mai mozzarella kÃ©o sá»£i thÆ¡m phá»©c.",
            BasePrice = 48000,
            ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            PortionDescription = "1 tÃ´ mÃ¬ trá»™n lá»›n"
        });

        // --- 5.9 Category 2.8: CÆ¡m & mÃ³n no ---
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "CÆ¡m ChiÃªn BÃ² Kim Chi Trá»©ng á»p La",
            Slug = "com-chien-bo-kim-chi-trung-op-la",
            ProductType = "FOOD",
            Description = "CÆ¡m chiÃªn háº¡t tÆ¡i vÃ ng Ã³ng Ä‘áº£o cÃ¹ng thá»‹t bÃ² Má»¹ thÃ¡i má»ng vÃ  kim chi cáº£i tháº£o chua cay giÃ²n tan, kÃ¨m trá»©ng á»‘p la lÃ²ng Ä‘Ã o vÃ  canh rong biá»ƒn thanh mÃ¡t.",
            BasePrice = 59000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 1,
            PortionDescription = "1 Ä‘Ä©a cÆ¡m Ä‘áº§y Ä‘áº·n kÃ¨m sÃºp rong biá»ƒn"
        });
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "CÆ¡m ChiÃªn Háº£i Sáº£n HoÃ ng Kim",
            Slug = "com-chien-hai-san-hoang-kim",
            ProductType = "FOOD",
            Description = "Háº¡t cÆ¡m Ä‘Æ°á»£c bao bá»c lá»›p lÃ²ng Ä‘á» trá»©ng muá»‘i bÃ©o ngáº­y, chiÃªn giÃ²n cÃ¹ng tÃ´m tÆ°Æ¡i, má»±c giÃ²n vÃ  Ä‘áº­u HÃ  Lan ngá»t bÃ¹i.",
            BasePrice = 62000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "1 Ä‘Ä©a lá»›n"
        });
        products.Add(new Product
        {
            CategoryId = catComNo.Id,
            Name = "Kimbap ChiÃªn XÃ¹ GiÃ²n Rá»¥m",
            Slug = "kimbap-chien-xu-gion-rum",
            ProductType = "FOOD",
            Description = "CÆ¡m cuá»™n rong biá»ƒn HÃ n Quá»‘c nhÃ¢n thanh cua, trá»©ng, xÃºc xÃ­ch vÃ  dÆ°a chuá»™t lÄƒn bá»™t xÃ¹ chiÃªn vÃ ng rá»¥m cháº¥m mayonnaise tÆ°Æ¡ng cÃ .",
            BasePrice = 39000,
            ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Mayonnaise & tÆ°Æ¡ng á»›t tÆ°Æ¡ng cÃ ",
            PortionDescription = "1 cuá»™n lá»›n (10 khoanh)"
        });

        // --- 5.10 Category 2.9: Ná»™i táº¡ng nÆ°á»›ng & mÃ³n nÆ°á»›ng ---
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "LÃ²ng Non NÆ°á»›ng Sa Táº¿ Cay GiÃ²n Than Hoa",
            Slug = "long-non-nuong-sa-te-cay-gion-than-hoa",
            ProductType = "FOOD",
            Description = "LÃ²ng non tÆ°Æ¡i lÃ m sáº¡ch ká»¹ cÃ ng, táº©m Æ°á»›p sa táº¿ cay cay Ä‘Æ°á»£m vá»‹ máº­t ong, nÆ°á»›ng trÃªn than hoa xÃ¬ xÃ¨o vÃ ng Æ°Æ¡m giÃ²n sáº§n sáº­t.",
            BasePrice = 68000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 3,
            AllowSauceCustomization = true,
            DefaultSauce = "Muá»‘i á»›t xanh chua cay & máº¯m nÃªm",
            PortionDescription = "1 Ä‘Ä©a nÆ°á»›ng nÃ³ng (300g kÃ¨m dÆ°a leo, rau rÄƒm)"
        });
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "Dá»“i Sá»¥n NÆ°á»›ng Than Hoa GiÃ²n Rá»¥m",
            Slug = "doi-sun-nuong-than-hoa-gion-rum",
            ProductType = "FOOD",
            Description = "Dá»“i sá»¥n thá»‹t heo thÆ¡m ná»©c mÃ¹i hÃºng quáº¿ vÃ  hÃ nh hoa, sá»¥n giÃ²n sáº§n sáº­t bÃªn trong nÆ°á»›ng vÃ ng Æ°Æ¡m lá»›p vá» ngoÃ i.",
            BasePrice = 55000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t cay & sá»‘t me",
            PortionDescription = "1 pháº§n (6 khoanh dÃ i)"
        });
        products.Add(new Product
        {
            CategoryId = catNoiTang.Id,
            Name = "Sá»¥n GÃ  ChiÃªn NÆ°á»›c Máº¯m / Rang Muá»‘i Há»“ng KÃ´ng",
            Slug = "sun-ga-chien-nuoc-mam-rang-muoi-hong-kong",
            ProductType = "FOOD",
            Description = "Sá»¥n á»©c gÃ  giÃ²n sáº§n sáº­t rang muá»‘i Há»“ng KÃ´ng thÆ¡m ngÃ¡t mÃ¹i sáº£ gá»«ng lÃ¡ chanh giÃ²n rá»¥m khÃ³ quÃªn.",
            BasePrice = 59000,
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "Muá»‘i tiÃªu chanh á»›t",
            PortionDescription = "1 Ä‘Ä©a (khoáº£ng 250g)"
        });

        // --- 5.11 Category 2.10: Ä‚n váº·t bá»• sung ---
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "Báº¯p XÃ o BÆ¡ TÃ©p Má»¡ HÃ nh PhÃ´ Mai",
            Slug = "bap-xao-bo-tep-mo-hanh-pho-mai",
            ProductType = "FOOD",
            Description = "Báº¯p má»¹ ngá»t giÃ²n xÃ o cÃ¹ng bÆ¡ TÆ°á»ng An thÆ¡m lá»«ng, tÃ©p sáº¥y Ä‘áº­m vá»‹, hÃ nh hoa tÆ°Æ¡i vÃ  ráº¯c phÃ´ mai kÃ©o sá»£i dáº»o dai bÃ©o ngáº­y.",
            BasePrice = 32000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            AllowSauceCustomization = true,
            DefaultSauce = "TÆ°Æ¡ng á»›t chua cay",
            PortionDescription = "1 pháº§n lá»›n nÃ³ng há»•i"
        });
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "BÃ¡nh Gáº¡o Tokbokki Cay Ngá»t Sá»‘t PhÃ´ Mai",
            Slug = "banh-gao-tokbokki-cay-ngot-sot-pho-mai",
            ProductType = "FOOD",
            Description = "BÃ¡nh gáº¡o dáº»o má»m HÃ n Quá»‘c Ä‘un trong sá»‘t Gochujang cay ngá»t Ä‘áº­m Ä‘Ã , ngáº­p trÃ n cháº£ cÃ¡, xÃºc xÃ­ch vÃ  phÃ´ mai bÃ©o ngáº­y cháº£y trÃ n.",
            BasePrice = 45000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            AllowSpicyCustomization = true,
            SpicyLevel = 2,
            PortionDescription = "1 pháº§n thá»‘ tokbokki Ä‘áº§y áº¯p"
        });
        products.Add(new Product
        {
            CategoryId = catAnVatBoSung.Id,
            Name = "KhÃ´ GÃ  LÃ¡ Chanh Cay XÃ© ThÆ¡m Lá»«ng",
            Slug = "kho-ga-la-chanh-cay-xe-thom-lung",
            ProductType = "FOOD",
            Description = "Tá»«ng sá»£i thá»‹t gÃ  xÃ© dai ngá»t Ä‘Æ°á»£m vá»‹ máº¯m tá»i á»›t cay ná»“ng, thÆ¡m phá»©c hÆ°Æ¡ng lÃ¡ chanh tÆ°Æ¡i sáº¥y giÃ²n.",
            BasePrice = 38000,
            ImageUrl = "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "HÅ© 150g"
        });

        // --- 5.12 Combos (Food + Drink) ---
                var combo1 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  sá»¯a vÃ  cÃ¡ viÃªn chiÃªn",
            Slug = "combo-tra-sua-va-ca-vien-chien",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  Sá»¯a Truyá»n Thá»‘ng trÃ¢n chÃ¢u Ä‘en size M + 1 Pháº§n CÃ¡ ViÃªn ChiÃªn sá»‘t tÆ°Æ¡ng Ä‘en sa táº¿ nÃ³ng há»•i giÃ²n rá»¥m.",
            BasePrice = 72000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  sá»¯a vÃ  cÃ¡ viÃªn chiÃªn tiáº¿t kiá»‡m"
        };
        var combo2 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  sá»¯a vÃ  khoai tÃ¢y chiÃªn",
            Slug = "combo-tra-sua-va-khoai-tay-chien",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  Sá»¯a ThÃ¡i Xanh size M + 1 Pháº§n Khoai TÃ¢y ChiÃªn láº¯c phÃ´ mai giÃ²n rá»¥m thÆ¡m lá»«ng.",
            BasePrice = 68000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  sá»¯a vÃ  khoai tÃ¢y chiÃªn tiáº¿t kiá»‡m"
        };
        var combo3 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  sá»¯a vÃ  bÃ¡nh trÃ¡ng trá»™n",
            Slug = "combo-tra-sua-va-banh-trang-tron",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  Sá»¯a Ã” Long NÆ°á»›ng size M + 1 Pháº§n BÃ¡nh TrÃ¡ng Trá»™n bÃ² khÃ´ tÃ©p sáº¥y sá»‘t sa táº¿ Ä‘áº·c biá»‡t.",
            BasePrice = 69000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  sá»¯a vÃ  bÃ¡nh trÃ¡ng trá»™n tiáº¿t kiá»‡m"
        };
        var combo4 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  trÃ¡i cÃ¢y vÃ  chÃ¢n gÃ  sáº£ táº¯c",
            Slug = "combo-tra-trai-cay-va-chan-ga-sa-tac",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  ÄÃ o Cam Sáº£ size L giáº£i nhiá»‡t + 1 Pháº§n ChÃ¢n GÃ  Sáº£ Táº¯c giÃ²n sáº§n sáº­t chua cay tÃª lÆ°á»¡i.",
            BasePrice = 89000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  trÃ¡i cÃ¢y vÃ  chÃ¢n gÃ  sáº£ táº¯c tiáº¿t kiá»‡m"
        };
        var combo5 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  sá»¯a vÃ  phÃ´ mai que",
            Slug = "combo-tra-sua-va-pho-mai-que",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  Sá»¯a Socola size M + 3 CÃ¢y PhÃ´ Mai Que kÃ©o sá»£i ngáº­p trÃ n cháº¥m tÆ°Æ¡ng cÃ  sá»‘t bÆ¡.",
            BasePrice = 72000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  sá»¯a vÃ  phÃ´ mai que tiáº¿t kiá»‡m"
        };
        var combo6 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo trÃ  sá»¯a vÃ  xÃºc xÃ­ch chiÃªn",
            Slug = "combo-tra-sua-va-xuc-xich-chien",
            ProductType = "COMBO",
            Description = "1 Ly TrÃ  Sá»¯a TrÃ¢n ChÃ¢u HoÃ ng Kim size M + 2 CÃ¢y XÃºc XÃ­ch Äá»©c chiÃªn khÃ­a lá»‘c xoÃ¡y nÃ³ng sá»‘t.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo trÃ  sá»¯a vÃ  xÃºc xÃ­ch chiÃªn tiáº¿t kiá»‡m"
        };
        var combo7 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo nÆ°á»›c Ã©p vÃ  Ä‘á»“ Äƒn váº·t",
            Slug = "combo-nuoc-ep-va-do-an-vat",
            ProductType = "COMBO",
            Description = "1 Ly NÆ°á»›c Ã‰p á»”i Há»“ng nguyÃªn cháº¥t giÃ u vitamin C + 1 Pháº§n Nem Chua RÃ¡n phá»‘ cá»• giÃ²n rá»¥m.",
            BasePrice = 75000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo nÆ°á»›c Ã©p vÃ  Ä‘á»“ Äƒn váº·t tiáº¿t kiá»‡m"
        };
        var combo8 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo Ä‘á»“ uá»‘ng vÃ  mÃ¬ cay",
            Slug = "combo-do-uong-va-mi-cay",
            ProductType = "COMBO",
            Description = "1 Thá»‘ MÃ¬ Cay HÃ n Quá»‘c háº£i sáº£n bÃ² Má»¹ cáº¥p Ä‘á»™ cay tÃ¹y chá»n + 1 Ly Há»“ng TrÃ  Macchiato mÃ¡t láº¡nh chá»¯a cay.",
            BasePrice = 99000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo Ä‘á»“ uá»‘ng vÃ  mÃ¬ cay tiáº¿t kiá»‡m"
        };
        var combo9 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo Ä‘á»“ uá»‘ng vÃ  cÆ¡m chiÃªn",
            Slug = "combo-do-uong-va-com-chien",
            ProductType = "COMBO",
            Description = "1 ÄÄ©a CÆ¡m ChiÃªn Kim Chi Trá»©ng LÃ²ng ÄÃ o thÆ¡m ná»©c + 1 Ly TrÃ  Táº¯c khá»•ng lá»“ mÃ¡t ruá»™t chá»‘ng ngáº¥y.",
            BasePrice = 95000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo Ä‘á»“ uá»‘ng vÃ  cÆ¡m chiÃªn tiáº¿t kiá»‡m"
        };
        var combo10 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo má»™t ngÆ°á»i",
            Slug = "combo-mot-nguoi-solo-treat",
            ProductType = "COMBO",
            Description = "Kháº©u pháº§n solo chuáº©n vá»‹: 1 Ly TrÃ  Sá»¯a MilkyVerse Signature size M + 1 Pháº§n Há»“ LÃ´ ChiÃªn giÃ²n thÆ¡m.",
            BasePrice = 65000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = false,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo má»™t ngÆ°á»i tiáº¿t kiá»‡m"
        };
        var combo11 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo hai ngÆ°á»i",
            Slug = "combo-hai-nguoi-doi-ban-than",
            ProductType = "COMBO",
            Description = "Bá»¯a tiá»‡c Ä‘Ã´i báº¡n: 2 Ly TrÃ  Sá»¯a Tá»± Chá»n size M + 1 Máº¹t ViÃªn ChiÃªn Tháº­p Cáº©m + 1 Pháº§n Khoai TÃ¢y Láº¯c PhÃ´ Mai.",
            BasePrice = 135000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo hai ngÆ°á»i tiáº¿t kiá»‡m"
        };
        var combo12 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo nhÃ³m báº¡n",
            Slug = "combo-nhom-ban-party-sieu-vui",
            ProductType = "COMBO",
            Description = "Party thá»‹nh soáº¡n 4 ngÆ°á»i: 4 Ly Äá»“ Uá»‘ng Size L + 1 Máº¹t ViÃªn ChiÃªn Lá»›n + 1 Há»™p ChÃ¢n GÃ  Sá»‘t ThÃ¡i + 2 BÃ¡nh TrÃ¡ng Trá»™n.",
            BasePrice = 260000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = true,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo nhÃ³m báº¡n tiáº¿t kiá»‡m"
        };
        var combo13 = new Product
        {
            CategoryId = catCombo.Id,
            Name = "Combo tá»± chá»n Ä‘á»“ uá»‘ng vÃ  Ä‘á»“ Äƒn",
            Slug = "combo-tu-chon-do-uong-va-do-an",
            ProductType = "COMBO",
            Description = "Tá»± do ghÃ©p Ä‘Ã´i 1 mÃ³n Ä‘á»“ uá»‘ng báº¥t ká»³ (size M) vÃ  1 mÃ³n Äƒn váº·t yÃªu thÃ­ch vá»›i Æ°u Ä‘Ã£i tiáº¿t kiá»‡m 15%.",
            BasePrice = 85000,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80",
            IsBestSeller = false,
            IsNew = true,
            IsActive = true,
            StockStatus = 1,
            PortionDescription = "Combo tá»± chá»n Ä‘á»“ uá»‘ng vÃ  Ä‘á»“ Äƒn tiáº¿t kiá»‡m"
        };
        products.AddRange(new[] { combo1, combo2, combo3, combo4, combo5, combo6, combo7, combo8, combo9, combo10, combo11, combo12, combo13 });

                context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // ==========================================
        // 5.13 LINK REAL COMBO ITEMS (RELATIONAL COMBO BOM)
        // ==========================================
        if (!context.ComboItems.Any())
        {
            var pDrinkTraSua = products.FirstOrDefault(p => p.Slug == "tra-sua-truyen-thong") ?? p1;
            var pDrinkTranChau = products.FirstOrDefault(p => p.Slug == "tra-sua-tran-chau-den") ?? p2;
            var pDrinkThaiXanh = products.FirstOrDefault(p => p.Slug == "tra-sua-thai-xanh") ?? p1;
            var pDrinkOolong = products.FirstOrDefault(p => p.Slug.Contains("oolong") || p.Slug.Contains("o-long")) ?? p1;
            var pDrinkDaoCamSa = products.FirstOrDefault(p => p.Slug.Contains("dao") || p.Slug.Contains("tra-trai-cay")) ?? p1;
            var pDrinkNuocEp = products.FirstOrDefault(p => p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda")) ?? p1;
            var pDrinkMatcha = products.FirstOrDefault(p => p.Slug.Contains("matcha")) ?? p1;

            var pFoodCaVien = products.FirstOrDefault(p => p.Slug == "ca-vien-chien-truyen-thong") ?? products.FirstOrDefault(p => p.Slug.Contains("ca-vien")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodKhoaiTay = products.FirstOrDefault(p => p.Slug.Contains("khoai-tay")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodBanhTrang = products.FirstOrDefault(p => p.Slug.Contains("banh-trang")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodChanGa = products.FirstOrDefault(p => p.Slug.Contains("chan-ga")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodPhoMai = products.FirstOrDefault(p => p.Slug.Contains("pho-mai")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodXucXich = products.FirstOrDefault(p => p.Slug.Contains("xuc-xich")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodMiCay = products.FirstOrDefault(p => p.Slug.Contains("mi-cay")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodComChien = products.FirstOrDefault(p => p.Slug.Contains("com")) ?? products.First(p => p.ProductType == "FOOD");
            var pFoodGa = products.FirstOrDefault(p => p.Slug.Contains("ga-ran") || p.Slug.Contains("ga")) ?? products.First(p => p.ProductType == "FOOD");

            var comboItems = new List<ComboItem>
            {
                // Combo 1: Trà sữa + Cá viên chiên
                new() { ComboProductId = combo1.Id, IncludedProductId = pDrinkTranChau.Id, Quantity = 1, CustomNotes = "Trà sữa trân châu đen ngọt ngào béo ngậy (Size M)" },
                new() { ComboProductId = combo1.Id, IncludedProductId = pFoodCaVien.Id, Quantity = 1, CustomNotes = "Cá viên chiên tương sa tế giòn nóng" },

                // Combo 2: Trà sữa + Khoai tây chiên
                new() { ComboProductId = combo2.Id, IncludedProductId = pDrinkThaiXanh.Id, Quantity = 1, CustomNotes = "Trà sữa Thái xanh thơm mát (Size M)" },
                new() { ComboProductId = combo2.Id, IncludedProductId = pFoodKhoaiTay.Id, Quantity = 1, CustomNotes = "Khoai tây chiên lắc phô mai giòn rụm" },

                // Combo 3: Trà sữa + Bánh tráng trộn
                new() { ComboProductId = combo3.Id, IncludedProductId = pDrinkOolong.Id, Quantity = 1, CustomNotes = "Trà sữa Ô Long nướng khói đậm đà (Size M)" },
                new() { ComboProductId = combo3.Id, IncludedProductId = pFoodBanhTrang.Id, Quantity = 1, CustomNotes = "Bánh tráng trộn bò khô tép sấy sốt sa tế đặc biệt" },

                // Combo 4: Trà trái cây + Chân gà sả tắc
                new() { ComboProductId = combo4.Id, IncludedProductId = pDrinkDaoCamSa.Id, Quantity = 1, CustomNotes = "Trà đào cam sả thơm lừng giải nhiệt (Size L)" },
                new() { ComboProductId = combo4.Id, IncludedProductId = pFoodChanGa.Id, Quantity = 1, CustomNotes = "Chân gà ngâm sả tắc chua cay tê lưỡi" },

                // Combo 5: Trà sữa + Phô mai que
                new() { ComboProductId = combo5.Id, IncludedProductId = pDrinkTraSua.Id, Quantity = 1, CustomNotes = "Trà sữa truyền thống thượng hạng (Size M)" },
                new() { ComboProductId = combo5.Id, IncludedProductId = pFoodPhoMai.Id, Quantity = 1, CustomNotes = "Phô mai que Mozzarella kéo sợi béo ngậy" },

                // Combo 6: Trà sữa + Xúc xích chiên
                new() { ComboProductId = combo6.Id, IncludedProductId = pDrinkTraSua.Id, Quantity = 1, CustomNotes = "Trà sữa truyền thống béo mộc (Size M)" },
                new() { ComboProductId = combo6.Id, IncludedProductId = pFoodXucXich.Id, Quantity = 1, CustomNotes = "Xúc xích Đức xông khói chiên hoa mai" },

                // Combo 7: Nước ép + Đồ ăn vặt
                new() { ComboProductId = combo7.Id, IncludedProductId = pDrinkNuocEp.Id, Quantity = 1, CustomNotes = "Nước ép nguyên chất tươi mát (Size L)" },
                new() { ComboProductId = combo7.Id, IncludedProductId = pFoodKhoaiTay.Id, Quantity = 1, CustomNotes = "Khoai tây chiên vàng ruộm" },

                // Combo 8: Đồ uống + Mì cay
                new() { ComboProductId = combo8.Id, IncludedProductId = pDrinkTraSua.Id, Quantity = 1, CustomNotes = "Trà sữa giải cay làm dịu vị giác (Size M)" },
                new() { ComboProductId = combo8.Id, IncludedProductId = pFoodMiCay.Id, Quantity = 1, CustomNotes = "Mì cay Hàn Quốc hải sản 7 cấp độ" },

                // Combo 9: Đồ uống + Cơm chiên
                new() { ComboProductId = combo9.Id, IncludedProductId = pDrinkDaoCamSa.Id, Quantity = 1, CustomNotes = "Trà thanh nhiệt mát lạnh (Size M)" },
                new() { ComboProductId = combo9.Id, IncludedProductId = pFoodComChien.Id, Quantity = 1, CustomNotes = "Cơm chiên Dương Châu nóng sốt thơm lừng" },

                // Combo 10: Solo Treat
                new() { ComboProductId = combo10.Id, IncludedProductId = pDrinkTranChau.Id, Quantity = 1, CustomNotes = "1 Ly trà sữa signature ngọt thanh" },
                new() { ComboProductId = combo10.Id, IncludedProductId = pFoodCaVien.Id, Quantity = 1, CustomNotes = "1 Đĩa đồ ăn vặt nóng giòn chuẩn vị" },

                // Combo 11: Đôi bạn
                new() { ComboProductId = combo11.Id, IncludedProductId = pDrinkTraSua.Id, Quantity = 2, CustomNotes = "2 Ly trà sữa tươi thanh mát" },
                new() { ComboProductId = combo11.Id, IncludedProductId = pFoodCaVien.Id, Quantity = 1, CustomNotes = "Mẹt cá viên chiên thập cẩm lớn" },

                // Combo 12: Nhóm bạn
                new() { ComboProductId = combo12.Id, IncludedProductId = pDrinkTranChau.Id, Quantity = 3, CustomNotes = "3 Ly trà sữa trân châu thơm béo" },
                new() { ComboProductId = combo12.Id, IncludedProductId = pFoodKhoaiTay.Id, Quantity = 2, CustomNotes = "2 Mẹt đồ chiên tổng hợp" },

                // Combo 13: Tự chọn đồ uống & đồ ăn
                new() { ComboProductId = combo13.Id, IncludedProductId = pDrinkMatcha.Id, Quantity = 1, CustomNotes = "1 Món nước tùy chọn trong thực đơn" },
                new() { ComboProductId = combo13.Id, IncludedProductId = pFoodGa.Id, Quantity = 1, CustomNotes = "1 Món ăn vặt tùy chọn theo sở thích" }
            };

            context.ComboItems.AddRange(comboItems);
            await context.SaveChangesAsync();
        }


        // ==========================================
        // 6. RECIPES FOR PRODUCTS (BOM / COSTING)
        // ==========================================
        var r1M = new Recipe { ProductId = p1.Id, Size = "M", Notes = "CÃ´ng thá»©c chuáº©n Size M" };
        var r1L = new Recipe { ProductId = p1.Id, Size = "L", Notes = "CÃ´ng thá»©c chuáº©n Size L" };
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
        // 8. PURCHASE RECEIPTS (NHáº¬P KHO) & AUDIT
        // ==========================================
        var receipt1 = new PurchaseReceipt
        {
            ReceiptCode = "PNK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-01",
            SupplierName = "CÃ´ng ty TNHH Thá»±c Pháº©m TÆ°Æ¡i Sáº¡ch HoÃ ng Gia",
            CreatedByUserId = warehouse.Id,
            TotalCost = 4500000,
            Status = "Completed",
            Notes = "Nháº­p nguyÃªn liá»‡u tÆ°Æ¡i cho thá»±c Ä‘Æ¡n Ä‘á»“ Äƒn vÃ  trÃ  sá»¯a tuáº§n má»›i",
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
            Notes = "Kiá»ƒm kÃª Ä‘á»‹nh ká»³ Ä‘áº§u tuáº§n - chÃªnh lá»‡ch trong má»©c hao há»¥t cháº¿ biáº¿n cho phÃ©p"
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
                Reason = "Hao há»¥t tá»± nhiÃªn khi Ä‘ong rÃ³t pha cháº¿"
            },
            new StockAuditItem
            {
                StockAuditId = audit1.Id,
                IngredientId = ingFishBall.Id,
                SystemQuantity = 1200,
                ActualQuantity = 1200,
                Variance = 0,
                Reason = "Sá»‘ lÆ°á»£ng khá»›p hoÃ n toÃ n"
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
                Description = "Giáº£m 20% cho khÃ¡ch hÃ ng má»›i tráº£i nghiá»‡m vÅ© trá»¥ Milkyverse",
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
                Description = "Miá»…n phÃ­ váº­n chuyá»ƒn 15.000Ä‘ cho Ä‘Æ¡n hÃ ng tá»« 99.000Ä‘",
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
                Description = "Giáº£m thÃªm 25.000Ä‘ cho hÃ³a Ä‘Æ¡n cÃ³ mÃ³n Äƒn váº·t vÃ  trÃ  sá»¯a tá»« 150.000Ä‘",
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
        // 10.1 Order 1: Completed order (ÄÃ£ giao thÃ nh cÃ´ng)
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
            Note = "Giao giá» hÃ nh chÃ­nh, nhiá»u Ä‘Ã¡ riÃªng, tÆ°Æ¡ng á»›t thÃªm",
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
            CustomNotes = "Ãt ngá»t thanh mÃ¡t"
        };
        var detail1_2 = new OrderDetail
        {
            OrderId = sampleOrder1.Id,
            ProductName = "CÃ¡ ViÃªn ChiÃªn Tháº­p Cáº©m Máº¹t Lá»›n",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay nháº¹ (Cáº¥p 1)",
            DippingSauce = "TÆ°Æ¡ng Ä‘en & TÆ°Æ¡ng á»›t xÃ o tá»i",
            PortionSize = "1 máº¹t lá»›n",
            UnitPrice = 65000,
            Quantity = 1,
            TotalPrice = 65000,
            CustomNotes = "Nhiá»u dÆ°a leo vÃ  tÆ°Æ¡ng Ä‘en xÃ o"
        };
        var detail1_3 = new OrderDetail
        {
            OrderId = sampleOrder1.Id,
            ProductName = "BÃ¡nh TrÃ¡ng Trá»™n Milkyverse Äáº·c Biá»‡t",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay vá»«a (Cáº¥p 2)",
            DippingSauce = "Sá»‘t bÃ² sa táº¿",
            PortionSize = "Pháº§n lá»›n",
            UnitPrice = 35000,
            Quantity = 1,
            TotalPrice = 35000,
            CustomNotes = "Nhiá»u xoÃ i vÃ  rau rÄƒm"
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
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Pending", Note = "KhÃ¡ch hÃ ng Ä‘áº·t Ä‘Æ¡n thanh toÃ¡n VietQR", CreatedAt = DateTime.UtcNow.AddHours(-4) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Confirmed", Note = "Quáº§y Ä‘Ã£ xÃ¡c nháº­n Ä‘Æ¡n hÃ ng", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(50) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Preparing", Note = "Báº¿p & quáº§y Ä‘ang lÃ m mÃ³n vÃ  pha cháº¿", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(40) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Delivering", Note = "Shipper nháº­n Ä‘Æ¡n vÃ  Ä‘ang giao", CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(20) },
            new OrderStatusHistory { OrderId = sampleOrder1.Id, Status = "Completed", Note = "Giao hÃ ng thÃ nh cÃ´ng tá»›i ngÆ°á»i nháº­n", CreatedAt = DateTime.UtcNow.AddHours(-3) }
        );

        // 10.2 Order 2: In Delivering (Äang giao hÃ ng - COD - phÃ¢n cÃ´ng cho Shipper)
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
            CustomerName = "Tráº§n Thá»‹ Mai PhÆ°Æ¡ng",
            CustomerPhone = "0933445566",
            ShippingAddress = "TÃ²a nhÃ  Bitexco, 2 Háº£i Triá»u, Báº¿n NghÃ©, Quáº­n 1, TP.HCM",
            Note = "Gá»i trÆ°á»›c khi Ä‘áº¿n 5 phÃºt, thanh toÃ¡n COD tiá»n máº·t",
            CreatedAt = DateTime.UtcNow.AddMinutes(-45)
        };
        context.Orders.Add(sampleOrder2);
        await context.SaveChangesAsync();

        var detail2_1 = new OrderDetail
        {
            OrderId = sampleOrder2.Id,
            ProductName = "ChÃ¢n GÃ  RÃºt XÆ°Æ¡ng Sá»‘t ThÃ¡i Cay GiÃ²n",
            ItemType = "FOOD",
            Size = "Standard",
            SpicyLevel = "Cay vá»«a (Cáº¥p 3)",
            DippingSauce = "NÆ°á»›c sá»‘t ThÃ¡i chua cay",
            PortionSize = "1 pháº§n lá»›n",
            UnitPrice = 69000,
            Quantity = 1,
            TotalPrice = 69000,
            CustomNotes = "Nhiá»u cÃ³c bao tá»­ giÃ²n"
        };
        var detail2_2 = new OrderDetail
        {
            OrderId = sampleOrder2.Id,
            ProductName = "TrÃ  Sá»¯a Milkyverse Signature",
            ItemType = "DRINK",
            Size = "M",
            SugarLevel = "70%",
            IceLevel = "100%",
            UnitPrice = 45000,
            Quantity = 1,
            TotalPrice = 45000,
            CustomNotes = "Chuáº©n vá»‹"
        };
        context.OrderDetails.AddRange(detail2_1, detail2_2);
        await context.SaveChangesAsync();

        context.OrderStatusHistories.AddRange(
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Pending", Note = "ÄÆ¡n má»›i COD", CreatedAt = DateTime.UtcNow.AddMinutes(-45) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Confirmed", Note = "Quáº§y Ä‘Ã£ duyá»‡t", CreatedAt = DateTime.UtcNow.AddMinutes(-40) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Preparing", Note = "Báº¿p chiÃªn chÃ¢n gÃ  vÃ  quáº§y láº¯c trÃ ", CreatedAt = DateTime.UtcNow.AddMinutes(-35) },
            new OrderStatusHistory { OrderId = sampleOrder2.Id, Status = "Delivering", Note = "BÃ n giao Shipper HoÃ ng Giao HÃ ng mang Ä‘i", CreatedAt = DateTime.UtcNow.AddMinutes(-15) }
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
            Notes = "KhÃ¡ch dáº·n Ä‘á»©ng sáº£nh chá» táº¡i Bitexco"
        });

        // 10.3 Order 3: Pending Order (Chá» báº¿p / quáº§y xÃ¡c nháº­n)
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
            CustomerName = "Nguyá»…n VÄƒn HÃ¹ng",
            CustomerPhone = "0912348899",
            ShippingAddress = "45 Nguyá»…n Thá»‹ Minh Khai, PhÆ°á»ng Báº¿n NghÃ©, Quáº­n 1, TP.HCM",
            Note = "Giao gáº¥p trÆ°á»›c 12h trÆ°a",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.Orders.Add(sampleOrder3);
        await context.SaveChangesAsync();

        var detail3_1 = new OrderDetail
        {
            OrderId = sampleOrder3.Id,
            ProductName = "Combo SiÃªu Cay: MÃ¬ Cay HÃ n Quá»‘c 7 Cáº¥p Äá»™ + TrÃ  Ã” Long NÆ°á»›ng KhÃ³i",
            ItemType = "COMBO",
            Size = "Standard",
            SpicyLevel = "Cáº¥p Ä‘á»™ 3 (Cay vá»«a)",
            DippingSauce = "Muá»‘i á»›t chanh",
            PortionSize = "1 pháº§n combo",
            UnitPrice = 109000,
            Quantity = 1,
            TotalPrice = 109000,
            CustomNotes = "MÃ¬ cay cáº¥p 3, trÃ  Ã­t ngá»t 50%"
        };
        context.OrderDetails.Add(detail3_1);

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = sampleOrder3.Id,
            Status = "Pending",
            Note = "ÄÆ¡n má»›i nháº­n trÃªn há»‡ thá»‘ng MilkyVerse",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        
        // ==========================================
        // 11. INITIAL PRODUCT REVIEWS IN DATABASE
        // ==========================================
        if (!context.ProductReviews.Any())
        {
            var sampleCustomer = await context.Users.FirstOrDefaultAsync(u => u.Role == "Customer") ?? admin;
            var revProduct1 = products.FirstOrDefault(p => p.Slug == "tra-sua-tran-chau-den") ?? p1;
            var revProduct2 = products.FirstOrDefault(p => p.Slug.Contains("mi-cay")) ?? p1;
            var revProduct3 = products.FirstOrDefault(p => p.Slug.Contains("oolong")) ?? p1;
            var revProduct4 = products.FirstOrDefault(p => p.Slug.Contains("ca-vien")) ?? p1;
            var revProduct5 = products.FirstOrDefault(p => p.Slug.Contains("combo")) ?? p1;

            var initialReviews = new List<ProductReview>
            {
                new()
                {
                    ProductId = revProduct1.Id,
                    UserId = sampleCustomer.Id,
                    UserName = "Trần Bảo Ngọc",
                    UserAvatar = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
                    Rating = 5,
                    Verdict = "Rất ngon / Chuẩn vị",
                    Comment = "Trà sữa nướng trân châu hoàng kim đỉnh chóp! Trân châu mềm dẻo thấm vị đường đen, vị trà đậm đà không bị ngọt gắt.",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new()
                {
                    ProductId = revProduct2.Id,
                    UserId = sampleCustomer.Id,
                    UserName = "Lê Minh Quân",
                    UserAvatar = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
                    Rating = 5,
                    Verdict = "Hương vị đậm đà",
                    Comment = "Mì cay 7 cấp độ ăn kèm trà dâu macchiato là combo chân ái luôn. Cửa hàng giao nhanh đúng 20 phút là có hàng nóng hổi.",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    ProductId = revProduct3.Id,
                    UserId = sampleCustomer.Id,
                    UserName = "Hoàng Yến Nhi",
                    UserAvatar = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80",
                    Rating = 5,
                    Verdict = "Cực kỳ hài lòng",
                    Comment = "Món signature hôm nay ngon xuất sắc, lớp kem cheese béo mặn sánh mịn, uống kèm trân châu 3Q giòn sần sật cực mê!",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    ProductId = revProduct4.Id,
                    UserId = sampleCustomer.Id,
                    UserName = "Đặng Tuấn Anh",
                    UserAvatar = "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80",
                    Rating = 5,
                    Verdict = "Đáng đồng tiền",
                    Comment = "Cá viên chiên mẹt thập cẩm sốt Thái đậm đà, nước sốt chấm thần thánh ăn cuốn miệng dã man. 5 sao cho chất lượng phục vụ!",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    ProductId = revProduct5.Id,
                    UserId = sampleCustomer.Id,
                    UserName = "Nguyễn Thảo Ly",
                    UserAvatar = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
                    Rating = 5,
                    Verdict = "Combo siêu tiết kiệm",
                    Comment = "Combo ăn kèm vừa no bụng vừa tiết kiệm được 25k, trà sữa đậm vị trà còn đồ ăn vặt thì giao đến vẫn nóng giòn!",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            };

            context.ProductReviews.AddRange(initialReviews);
            await context.SaveChangesAsync();
        }

        await context.SaveChangesAsync();
    }
}

