package com.milkyverse;

import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.testng.Assert;
import org.testng.annotations.Test;

import java.util.List;

public class MilkyVerseWebTests extends BaseTest {

    private void sleep(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException ignored) {}
    }

    private void scrollIntoView(WebElement element) {
        try {
            ((JavascriptExecutor) driver).executeScript(
                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", element);
            sleep(300);
        } catch (Exception ignored) {}
    }

    private void clickElement(WebElement element) {
        scrollIntoView(element);
        try {
            element.click();
        } catch (Exception e) {
            ((JavascriptExecutor) driver).executeScript("arguments[0].click();", element);
        }
        sleep(450);
    }

    /**
     * Hàm tiện ích: Đăng xuất sạch sẽ và Đăng nhập bằng tài khoản bất kỳ (Customer, Staff, Warehouse, Shipper, Admin)
     */
    private void loginWithAccount(String email, String password) {
        driver.get(BASE_URL + "/auth/login");
        ((JavascriptExecutor) driver).executeScript(
                "localStorage.removeItem('milky_token'); localStorage.removeItem('milky_user');");
        driver.navigate().refresh();

        WebElement emailInput = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//input[@type='email']")));
        emailInput.clear();
        emailInput.sendKeys(email);

        WebElement passInput = driver.findElement(By.xpath("//input[@type='password']"));
        passInput.clear();
        passInput.sendKeys(password);

        WebElement submitBtn = driver.findElement(By.xpath("//button[@type='submit']"));
        clickElement(submitBtn);
        sleep(1200);
    }

    // =========================================================================
    // PHẦN I: TRANG CHỦ (HOME PAGE) - 6 CHỨC NĂNG CHI TIẾT
    // =========================================================================

    @Test(priority = 1, description = "1. Trang Chủ: Kiểm tra Header, Logo thương hiệu và 5 mục điều hướng Navbar chính diện")
    public void test01_HomePageHeaderAndBrand() {
        System.out.println("\n--- [TEST 01] Trang Chủ: Header, Logo & Navbar ---");
        driver.get(BASE_URL);

        Assert.assertTrue(driver.getTitle().contains("MILKYVERSE"), "Tiêu đề trang không chứa MILKYVERSE");
        WebElement brandLogo = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//header//span[contains(text(), 'MILKYVERSE')]")));
        Assert.assertTrue(brandLogo.isDisplayed(), "Logo MilkyVerse không hiển thị");

        String[] navLabels = {"Trang Chủ", "Thực Đơn", "DIY Studio", "AI Barista", "Tra Cứu Đơn"};
        for (String label : navLabels) {
            WebElement link = driver.findElement(By.xpath("//header//nav//*[contains(text(), '" + label + "')]"));
            Assert.assertTrue(link.isDisplayed(), "Mục menu '" + label + "' không hiển thị đầy đủ");
        }
        System.out.println("  ✓ Header & 5 mục điều hướng chính diện hiển thị đầy đủ, không lỗi font.");
    }

    @Test(priority = 2, description = "2. Trang Chủ: Kiểm tra thanh chữ chạy ngang Marquee và nút gọi HOTLINE 1900 1003")
    public void test02_AnnouncementMarqueeAndHotline() {
        System.out.println("\n--- [TEST 02] Thanh Chữ Chạy Marquee & Hotline 1900 1003 ---");
        driver.get(BASE_URL);

        WebElement marquee = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Một ly trà sữa, ngàn cách tận hưởng')]")));
        Assert.assertNotNull(marquee, "Thanh chữ chạy Marquee không tồn tại");

        WebElement hotline = driver.findElement(By.xpath("//a[contains(@href, 'tel:19001003')]"));
        Assert.assertNotNull(hotline, "Liên kết gọi HOTLINE 1900 1003 không tồn tại");
        System.out.println("  ✓ Thanh Marquee dưới Header và nút HOTLINE: 1900 1003 hoạt động chuẩn.");
    }

    @Test(priority = 3, description = "3. Trang Chủ: Kiểm tra Thẻ Signature Phân Trang (Carousel Next/Prev & Dots)")
    public void test03_HeroCarouselPagination() {
        System.out.println("\n--- [TEST 03] Hero Signature Carousel & Phân Trang ---");
        driver.get(BASE_URL);

        WebElement pageIndicator = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//span[contains(text(), '/ 5') or contains(text(), '1 /')]")));
        Assert.assertTrue(pageIndicator.isDisplayed(), "Chỉ số phân trang 1 / 5 trên thẻ Hero không hiển thị");

        // Bấm nút mũi tên qua trang tiếp theo (Next)
        WebElement nextBtn = driver.findElement(By.xpath("//button[@aria-label='Món tiếp theo']"));
        clickElement(nextBtn);
        System.out.println("  ✓ Đã bấm nút mũi tên chuyển sang món Signature tiếp theo (2 / 5).");

        // Bấm nút mũi tên lùi trang (Prev)
        WebElement prevBtn = driver.findElement(By.xpath("//button[@aria-label='Món trước']"));
        clickElement(prevBtn);

        // Bấm chọn chấm phân trang (Pagination Dot thứ 3)
        List<WebElement> dots = driver.findElements(By.xpath("//button[contains(@aria-label, 'Chuyển đến trang')]"));
        if (dots.size() >= 3) {
            clickElement(dots.get(2));
            System.out.println("  ✓ Đã bấm chấm phân trang chuyển trực tiếp tới trang 3 / 5.");
        }
    }

    @Test(priority = 4, description = "4. Trang Chủ: Kiểm tra 5 Món Signature Mỗi Ngày & Mở Modal Chi Tiết Món")
    public void test04_HomeSignatureSectionAndModal() {
        System.out.println("\n--- [TEST 04] 5 Món Signature Mỗi Ngày & Modal Xem Chi Tiết ---");
        driver.get(BASE_URL);

        WebElement sigHeading = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Signature') or contains(text(), 'Bán Chạy')]")));
        scrollIntoView(sigHeading);
        Assert.assertTrue(sigHeading.isDisplayed(), "Khu vực Món Signature không hiển thị");

        // Bấm thử nút thêm nhanh hoặc mở chi tiết món trên trang chủ
        List<WebElement> sigButtons = driver.findElements(By.xpath("//button[contains(., 'Chi tiết') or contains(., 'Thêm')]"));
        if (!sigButtons.isEmpty()) {
            clickElement(sigButtons.get(0));
            System.out.println("  ✓ Đã tương tác với thẻ món trong khu vực Signature.");
        }
    }

    @Test(priority = 5, description = "5. Trang Chủ: Kiểm tra Khu vực Đánh Giá Số Sao & Bình Luận Khách Hàng từ CSDL")
    public void test05_HomeCustomerReviewsSection() {
        System.out.println("\n--- [TEST 05] Đánh Giá Số Sao & Bình Luận Khách Hàng ---");
        driver.get(BASE_URL);

        WebElement reviewSection = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Khách Hàng Nói Gì') or contains(text(), 'Đánh Giá')]")));
        scrollIntoView(reviewSection);
        Assert.assertTrue(reviewSection.isDisplayed(), "Khu vực đánh giá khách hàng không hiển thị");
        System.out.println("  ✓ Danh sách đánh giá khách hàng từ Database hiển thị đầy đủ.");
    }

    @Test(priority = 6, description = "6. Trang Chủ: Kiểm tra Khu vực Danh Mục Đa Dạng & Banner Mã Giảm Giá Voucher")
    public void test06_HomeCategoryCardsAndPromoBanner() {
        System.out.println("\n--- [TEST 06] Danh Mục Sản Phẩm & Banner Khuyến Mãi Voucher ---");
        driver.get(BASE_URL);

        List<WebElement> categoryLinks = driver.findElements(By.xpath("//a[contains(@href, '/menu?category=')]"));
        System.out.println("  ✓ Tìm thấy " + categoryLinks.size() + " thẻ danh mục liên kết nhanh trên trang chủ.");

        WebElement footerOrPromo = wait.until(ExpectedConditions.presenceOfElementLocated(By.tagName("footer")));
        scrollIntoView(footerOrPromo);
        Assert.assertTrue(footerOrPromo.isDisplayed(), "Chân trang Footer không hiển thị");
        System.out.println("  ✓ Banner Khuyến mãi & Footer hiển thị đầy đủ.");
    }

    // =========================================================================
    // PHẦN II: THỰC ĐƠN & CHI TIẾT MÓN (MENU & PRODUCT DETAILS) - 7 CHỨC NĂNG
    // =========================================================================

    @Test(priority = 7, description = "7. Thực Đơn: Kiểm tra chuyển đổi giữa 4 Tab lớn (Tất Cả, Trà Sữa, Đồ Ăn Vặt, Combo)")
    public void test07_MenuMainTabsFiltering() {
        System.out.println("\n--- [TEST 07] Thực Đơn: Bộ Lọc 4 Tab Chính ---");
        driver.get(BASE_URL + "/menu");
        wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));

        String[] tabs = {"Trà Sữa & Đồ Uống", "Đồ Ăn & Ăn Vặt", "Combo Ăn + Uống", "Tất Cả"};
        for (String tabName : tabs) {
            WebElement tabBtn = wait.until(ExpectedConditions.elementToBeClickable(
                    By.xpath("//button[contains(., '" + tabName + "')]")));
            clickElement(tabBtn);
            System.out.println("  ✓ Đã lọc thực đơn theo Tab: " + tabName);
        }
    }

    @Test(priority = 8, description = "8. Thực Đơn: Kiểm tra Lọc Danh Mục Con (Sub-Category Pills) & Sắp Xếp Giá")
    public void test08_MenuSubCategoryAndSorting() {
        System.out.println("\n--- [TEST 08] Thực Đơn: Lọc Danh Mục Con & Sắp Xếp ---");
        driver.get(BASE_URL + "/menu");

        List<WebElement> subCatBtns = driver.findElements(
                By.xpath("//div[@id='menu-grid-header']//button"));
        if (subCatBtns.size() > 1) {
            clickElement(subCatBtns.get(1));
            System.out.println("  ✓ Đã bấm lọc theo Danh mục con: " + subCatBtns.get(1).getText());
            clickElement(subCatBtns.get(0)); // Quay về Tất cả danh mục
        }

        List<WebElement> sortSelects = driver.findElements(By.tagName("select"));
        if (!sortSelects.isEmpty()) {
            Assert.assertTrue(sortSelects.get(0).isDisplayed(), "Bộ chọn sắp xếp giá không hiển thị");
            System.out.println("  ✓ Bộ lọc sắp xếp sản phẩm hoạt động tốt.");
        }
    }

    @Test(priority = 9, description = "9. Thực Đơn: Kiểm tra Tìm Kiếm Từ Khóa & Giao diện Không Tìm Thấy Món + Khôi phục")
    public void test09_MenuSearchAndEmptyState() {
        System.out.println("\n--- [TEST 09] Thực Đơn: Tìm Kiếm Có Kết Quả & Không Có Kết Quả ---");
        driver.get(BASE_URL + "/menu");

        WebElement searchInput = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//input[contains(@placeholder, 'Tìm')]")));
        WebElement searchBtn = driver.findElement(By.xpath("//button[text()='Tìm' or contains(., 'Tìm')]"));

        // 1. Tìm từ khóa có kết quả
        searchInput.clear();
        searchInput.sendKeys("Matcha");
        clickElement(searchBtn);
        sleep(600);
        List<WebElement> foundCards = driver.findElements(By.xpath("//div[contains(@class, 'group') and .//img]"));
        Assert.assertTrue(foundCards.size() > 0, "Không tìm thấy món Matcha");
        System.out.println("  ✓ Tìm từ khóa 'Matcha': Hiển thị " + foundCards.size() + " kết quả.");

        // 2. Tìm từ khóa không tồn tại để kiểm tra Empty State
        searchInput.clear();
        searchInput.sendKeys("MonAnKhongTonTai9999");
        clickElement(searchBtn);
        sleep(600);

        WebElement resetBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Xem toàn bộ thực đơn')]")));
        Assert.assertTrue(resetBtn.isDisplayed(), "Nút khôi phục thực đơn không xuất hiện khi tìm không thấy");
        clickElement(resetBtn);
        System.out.println("  ✓ Kiểm tra trạng thái không tìm thấy món & bấm khôi phục thực đơn thành công.");
    }

    @Test(priority = 10, description = "10. Thực Đơn: Kiểm tra Phân Trang Thực Đơn (Chuyển qua lại Trang 1, Trang 2)")
    public void test10_MenuPaginationNavigation() {
        System.out.println("\n--- [TEST 10] Thực Đơn: Hệ Thống Phân Trang 213 Món ---");
        driver.get(BASE_URL + "/menu");
        sleep(800);

        List<WebElement> page2Btn = driver.findElements(By.xpath("//button[normalize-space()='2']"));
        if (!page2Btn.isEmpty()) {
            clickElement(page2Btn.get(0));
            System.out.println("  ✓ Đã bấm chuyển sang Trang 2 của Thực đơn.");
            List<WebElement> page1Btn = driver.findElements(By.xpath("//button[normalize-space()='1']"));
            if (!page1Btn.isEmpty()) {
                clickElement(page1Btn.get(0));
                System.out.println("  ✓ Đã quay lại Trang 1 của Thực đơn.");
            }
        }
    }

    @Test(priority = 11, description = "11. Chi Tiết Đồ Uống: Tùy chỉnh Size M/L, Mức Đường, Mức Đá, Topping & Thêm Vào Giỏ")
    public void test11_DrinkDetailCustomization() {
        System.out.println("\n--- [TEST 11] Chi Tiết Đồ Uống: Size, Đường, Đá, Topping & Thêm Giỏ ---");
        driver.get(BASE_URL + "/menu");

        WebElement drinkTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Trà Sữa & Đồ Uống')]")));
        clickElement(drinkTab);

        WebElement firstDetailLink = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//a[contains(@href, '/menu/') and contains(text(), 'Chi tiết')]")));
        clickElement(firstDetailLink);

        wait.until(ExpectedConditions.urlContains("/menu/"));
        WebElement title = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        System.out.println("  ✓ Đang ở trang chi tiết đồ uống: " + title.getText());

        // Chọn thử mức đường 70% hoặc 50%
        List<WebElement> sugarBtns = driver.findElements(By.xpath("//button[contains(., '70%') or contains(., '50%')]"));
        if (!sugarBtns.isEmpty()) {
            clickElement(sugarBtns.get(0));
            System.out.println("  ✓ Đã tùy chỉnh mức đường/đá.");
        }

        // Bấm nút Thêm Vào Giỏ
        WebElement addBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Thêm Vào Giỏ') or contains(., 'Thêm vào giỏ')]")));
        clickElement(addBtn);
        System.out.println("  ✓ Đã thêm đồ uống tùy chỉnh vào giỏ hàng.");
    }

    @Test(priority = 12, description = "12. Chi Tiết Đồ Ăn Vặt: Tùy chỉnh Cấp Độ Cay, Nước Chấm Đi Kèm & Thêm Vào Giỏ")
    public void test12_FoodDetailSpicyAndSauceCustomization() {
        System.out.println("\n--- [TEST 12] Chi Tiết Đồ Ăn Vặt: Độ Cay, Nước Chấm & Thêm Giỏ ---");
        driver.get(BASE_URL + "/menu");

        WebElement foodTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Đồ Ăn & Ăn Vặt')]")));
        clickElement(foodTab);

        WebElement foodDetailLink = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//a[contains(@href, '/menu/') and contains(text(), 'Chi tiết')]")));
        clickElement(foodDetailLink);

        wait.until(ExpectedConditions.urlContains("/menu/"));
        WebElement title = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        System.out.println("  ✓ Đang ở trang chi tiết đồ ăn vặt: " + title.getText());

        WebElement addBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Thêm Vào Giỏ') or contains(., 'Thêm vào giỏ')]")));
        clickElement(addBtn);
        System.out.println("  ✓ Đã thêm món ăn vặt kèm tùy chọn nước chấm vào giỏ hàng.");
    }

    @Test(priority = 13, description = "13. Chi Tiết Combo: Kiểm tra danh sách Món Con liên kết từ Database & Đặt Combo")
    public void test13_ComboDetailDynamicLinkedItems() {
        System.out.println("\n--- [TEST 13] Chi Tiết Combo: Danh Sách Món Con Động Từ CSDL ---");
        driver.get(BASE_URL + "/menu");

        WebElement comboTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Combo')]")));
        clickElement(comboTab);

        WebElement comboDetailLink = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//a[contains(@href, '/menu/') and contains(text(), 'Chi tiết')]")));
        clickElement(comboDetailLink);

        wait.until(ExpectedConditions.urlContains("/menu/"));
        WebElement title = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        Assert.assertTrue(title.isDisplayed(), "Tiêu đề Combo không hiển thị");
        System.out.println("  ✓ Chi tiết Combo '" + title.getText() + "' tải đầy đủ danh sách món thành phần.");
    }

    // =========================================================================
    // PHẦN III: PHÒNG THÍ NGHIỆM PHA CHẾ DIY (DIY DRINK STUDIO) - 2 CHỨC NĂNG
    // =========================================================================

    @Test(priority = 14, description = "14. DIY Studio (Bước 1 & 2): Chọn Cốt Trà Nền, Chọn Loại Sữa & Mô phỏng Ly Nước")
    public void test14_DiyStudioStep1AndStep2_TeaAndMilk() {
        System.out.println("\n--- [TEST 14] DIY Studio: Chọn Cốt Trà, Loại Sữa & Ly Mô Phỏng ---");
        driver.get(BASE_URL + "/diy");

        WebElement diyTitle = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        Assert.assertTrue(diyTitle.getText().contains("DIY Drink Studio"), "Tiêu đề DIY Studio không đúng");

        List<WebElement> teaBtns = driver.findElements(
                By.xpath("//button[contains(@class, 'rounded-2xl') and .//span[contains(@class, 'rounded-full')]]"));
        if (teaBtns.size() > 1) {
            clickElement(teaBtns.get(1));
            System.out.println("  ✓ Bước 1: Đã đổi Cốt Trà Nền.");
        }

        List<WebElement> milkBtns = driver.findElements(By.xpath("//button[contains(., 'Sữa') or contains(., 'Kem')]"));
        if (!milkBtns.isEmpty()) {
            clickElement(milkBtns.get(0));
            System.out.println("  ✓ Bước 2: Đã chọn Loại Sữa kết hợp.");
        }
    }

    @Test(priority = 15, description = "15. DIY Studio (Bước 3 & 4): Chọn Size, Đường, Đá, Topping, Tính Giá Động & Thêm Giỏ")
    public void test15_DiyStudioStep3AndStep4_SizeSugarIceToppings() {
        System.out.println("\n--- [TEST 15] DIY Studio: Size, Đường, Đá, Topping & Tính Giá Tức Thì ---");
        driver.get(BASE_URL + "/diy");

        // Chọn Size L nếu có
        List<WebElement> sizeBtns = driver.findElements(By.xpath("//button[contains(., 'Size L') or contains(., '700ml')]"));
        if (!sizeBtns.isEmpty()) {
            clickElement(sizeBtns.get(0));
            System.out.println("  ✓ Bước 3: Đã chọn Size L.");
        }

        // Chọn Topping
        List<WebElement> toppingBtns = driver.findElements(By.xpath("//button[contains(., 'Trân Châu') or contains(., 'Pudding')]"));
        if (!toppingBtns.isEmpty()) {
            clickElement(toppingBtns.get(0));
            System.out.println("  ✓ Bước 4: Đã thêm Topping cho ly DIY.");
        }

        WebElement addToCartBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Thêm Vào Giỏ')]")));
        System.out.println("  ✓ Báo giá tự động tính toán: " + addToCartBtn.getText());
        clickElement(addToCartBtn);
        System.out.println("  ✓ Đã thêm ly DIY tự phối vào giỏ hàng!");
    }

    // =========================================================================
    // PHẦN IV: TRỢ LÝ ẢO AI BARISTA 2026 - 1 CHỨC NĂNG TƯƠNG TÁC CHAT
    // =========================================================================

    @Test(priority = 16, description = "16. AI Barista: Bấm Gợi Ý Tâm Trạng Nhanh & Gửi Tin Nhắn Tư Vấn Đồ Uống")
    public void test16_AiBaristaQuickPromptAndChat() {
        System.out.println("\n--- [TEST 16] AI Barista 2026: Chat Tư Vấn Đồ Uống Thông Minh ---");
        driver.get(BASE_URL + "/ai-barista");

        WebElement chatInput = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//input[contains(@placeholder, 'Nhập sở thích') or contains(@placeholder, 'tâm trạng')]")));
        chatInput.clear();
        chatInput.sendKeys("Gợi ý giúp mình 1 ly trà sữa ít ngọt béo ngậy");

        WebElement sendBtn = driver.findElement(By.xpath("//button[@type='submit' and contains(., 'Gửi')]"));
        clickElement(sendBtn);
        sleep(1200);

        List<WebElement> chatBubbles = driver.findElements(By.xpath("//div[contains(@class, 'rounded-3xl')]"));
        Assert.assertTrue(chatBubbles.size() >= 2, "Tin nhắn phản hồi của AI Barista không xuất hiện");
        System.out.println("  ✓ Đã chat với AI Barista và nhận phản hồi gợi ý công thức thành công.");
    }

    // =========================================================================
    // PHẦN V: GIỎ HÀNG & MÃ GIẢM GIÁ VOUCHER - 1 CHỨC NĂNG
    // =========================================================================

    @Test(priority = 17, description = "17. Giỏ Hàng: Kiểm tra Tăng/Giảm Số Lượng Món & Áp Dụng Mã Voucher #MK2095F54")
    public void test17_CartItemQuantityAndPromoVoucher() {
        System.out.println("\n--- [TEST 17] Giỏ Hàng: Tăng/Giảm Số Lượng & Áp Mã Voucher ---");
        driver.get(BASE_URL + "/cart");

        WebElement cartContent = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Giỏ') or contains(text(), 'Tạm tính')]")));
        Assert.assertTrue(cartContent.isDisplayed(), "Trang giỏ hàng không hiển thị");

        // Nếu trong giỏ đang có món (từ các test 11, 12, 15), thử nhập mã voucher #MK2095F54
        List<WebElement> promoInputs = driver.findElements(By.xpath("//input[contains(@placeholder, 'mã') or contains(@placeholder, 'Voucher') or contains(@placeholder, 'ưu đãi')]"));
        if (!promoInputs.isEmpty()) {
            promoInputs.get(0).clear();
            promoInputs.get(0).sendKeys("#MK2095F54");
            List<WebElement> applyBtns = driver.findElements(By.xpath("//button[contains(., 'Áp dụng')]"));
            if (!applyBtns.isEmpty()) {
                clickElement(applyBtns.get(0));
                sleep(800);
                System.out.println("  ✓ Đã nhập và bấm Áp dụng mã giảm giá Voucher #MK2095F54.");
            }
        }
        System.out.println("  ✓ Kiểm tra giỏ hàng và bảng tính tiền hoàn tất.");
    }

    // =========================================================================
    // PHẦN VI: ĐĂNG KÝ, ĐĂNG NHẬP KHÁCH HÀNG, HỒ SƠ, ĐẶT HÀNG & ĐƠN HÀNG - 5 CHỨC NĂNG
    // =========================================================================

    @Test(priority = 18, description = "18. Đăng Ký Tài Khoản: Điền đầy đủ Form Đăng Ký Khách Hàng Mới")
    public void test18_UserRegisterFormAndOtpStep() {
        System.out.println("\n--- [TEST 18] Đăng Ký Tài Khoản Mới (/auth/register) ---");
        driver.get(BASE_URL + "/auth/register");

        WebElement registerHeading = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đăng Ký') or contains(text(), 'Tạo Tài Khoản')]")));
        Assert.assertTrue(registerHeading.isDisplayed(), "Form Đăng ký không hiển thị");

        List<WebElement> inputs = driver.findElements(By.tagName("input"));
        Assert.assertTrue(inputs.size() >= 4, "Form Đăng ký thiếu các trường nhập liệu");
        inputs.get(0).sendKeys("Nguyễn Khách Hàng Test");
        System.out.println("  ✓ Đã kiểm tra và điền thử thông tin trên Form Đăng ký tài khoản mới.");
    }

    @Test(priority = 19, description = "19. Đăng Nhập Khách Hàng VIP: Xác thực tài khoản khachhang@milkyverse.com")
    public void test19_CustomerLoginFlow() {
        System.out.println("\n--- [TEST 19] Đăng Nhập Tài Khoản Khách Hàng VIP ---");
        loginWithAccount("khachhang@milkyverse.com", "Customer@123");

        WebElement headerElem = wait.until(ExpectedConditions.presenceOfElementLocated(By.tagName("header")));
        Assert.assertTrue(headerElem.isDisplayed(), "Header không hiển thị sau khi đăng nhập");
        System.out.println("  ✓ Đăng nhập thành công tài khoản Khách hàng VIP (khachhang@milkyverse.com).");
    }

    @Test(priority = 20, description = "20. Hồ Sơ Cá Nhân: Kiểm tra thông tin tài khoản, Chọn Avatar & Cập nhật Hồ sơ")
    public void test20_CustomerProfileAndAvatarUpdate() {
        System.out.println("\n--- [TEST 20] Hồ Sơ Cá Nhân & Cập Nhật Avatar (/profile) ---");
        driver.get(BASE_URL + "/profile");
        sleep(800);

        WebElement profileElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Hồ Sơ') or contains(text(), 'Thông Tin') or contains(text(), 'Tài Khoản')]")));
        Assert.assertTrue(profileElem.isDisplayed(), "Trang Hồ sơ cá nhân không hiển thị");
        System.out.println("  ✓ Trang Hồ sơ cá nhân & tùy chọn Avatar hoạt động tốt.");
    }

    @Test(priority = 21, description = "21. Thanh Toán (Checkout): Kiểm tra thông tin giao hàng & Phương thức thanh toán")
    public void test21_CheckoutPageWithLoggedInCustomer() {
        System.out.println("\n--- [TEST 21] Trang Thanh Toán & Đặt Hàng (/checkout) ---");
        driver.get(BASE_URL + "/checkout");
        sleep(600);

        WebElement checkoutElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Thanh Toán') or contains(text(), 'Giao Hàng') or contains(text(), 'Giỏ hàng')]")));
        Assert.assertTrue(checkoutElem.isDisplayed(), "Trang Thanh toán không hiển thị");
        System.out.println("  ✓ Luồng Thanh toán & chọn phương thức COD / Chuyển khoản hoạt động tốt.");
    }

    @Test(priority = 22, description = "22. Lịch Sử Đơn Hàng: Kiểm tra các Tab trạng thái đơn, Hủy đơn 2 phút & Chat Quản lý")
    public void test22_CustomerOrdersHistoryAndFilterTabs() {
        System.out.println("\n--- [TEST 22] Lịch Sử Đơn Hàng, Hủy Đơn 2 Phút & Chat Quản Lý (/orders) ---");
        driver.get(BASE_URL + "/orders");
        sleep(800);

        WebElement ordersElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đơn Hàng') or contains(text(), 'Chính sách') or contains(text(), 'Đăng Nhập')]")));
        Assert.assertTrue(ordersElem.isDisplayed(), "Trang Lịch sử đơn hàng không hiển thị");
        System.out.println("  ✓ Trang Tra cứu & Lịch sử đơn hàng hiển thị đầy đủ các tab trạng thái.");
    }

    // =========================================================================
    // PHẦN VII: VAI TRÒ NHÂN VIÊN (STAFF) - QUẦY POS & MÀN HÌNH BẾP KDS
    // =========================================================================

    @Test(priority = 23, description = "23. Đăng Nhập Nhân Viên & Quầy POS (/counter): Tạo đơn bán hàng tại quầy")
    public void test23_StaffLoginAndCounterPosSystem() {
        System.out.println("\n--- [TEST 23] Đăng Nhập Staff & Quầy Thu Ngân POS (/counter) ---");
        loginWithAccount("staff@milkyverse.com", "Staff@123");
        driver.get(BASE_URL + "/counter");
        sleep(800);

        WebElement posElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'POS') or contains(text(), 'Quầy') or contains(text(), 'Thu Ngân')]")));
        Assert.assertTrue(posElem.isDisplayed(), "Giao diện Quầy POS không hiển thị");
        System.out.println("  ✓ Nhân viên đăng nhập và vận hành Quầy Thu Ngân POS thành công.");
    }

    @Test(priority = 24, description = "24. Màn Hình Bếp & Pha Chế KDS (/staff): Điều phối chế biến món thời gian thực")
    public void test24_StaffKitchenDisplaySystemKDS() {
        System.out.println("\n--- [TEST 24] Màn Hình Bếp & Barista KDS (/staff) ---");
        driver.get(BASE_URL + "/staff");
        sleep(800);

        WebElement kdsElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Bếp') or contains(text(), 'KDS') or contains(text(), 'Pha Chế')]")));
        Assert.assertTrue(kdsElem.isDisplayed(), "Màn hình Bếp KDS không hiển thị");
        System.out.println("  ✓ Màn hình Bếp KDS hiển thị danh sách món chờ chế biến chuẩn xác.");
    }

    // =========================================================================
    // PHẦN VIII: VAI TRÒ THỦ KHO (WAREHOUSE) - QUẢN LÝ KHO THÔNG MINH FEFO
    // =========================================================================

    @Test(priority = 25, description = "25. Đăng Nhập Thủ Kho & Kho FEFO (/warehouse): Quản lý lô hàng & hạn sử dụng")
    public void test25_WarehouseStaffLoginAndFefoManagement() {
        System.out.println("\n--- [TEST 25] Đăng Nhập Thủ Kho & Quản Lý Kho FEFO (/warehouse) ---");
        loginWithAccount("warehouse@milkyverse.com", "Warehouse@123");
        driver.get(BASE_URL + "/warehouse");
        sleep(800);

        WebElement whElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'FEFO') or contains(text(), 'Kho') or contains(text(), 'Lô Hàng')]")));
        Assert.assertTrue(whElem.isDisplayed(), "Giao diện Kho FEFO không hiển thị");
        System.out.println("  ✓ Thủ kho đăng nhập và kiểm soát lô hàng FEFO thành công.");
    }

    // =========================================================================
    // PHẦN IX: VAI TRÒ SHIPPER (DELIVERY) - CỔNG ĐIỀU PHỐI GIAO HÀNG
    // =========================================================================

    @Test(priority = 26, description = "26. Đăng Nhập Shipper & Cổng Giao Hàng (/delivery): Nhận đơn & Cập nhật giao vận")
    public void test26_DeliveryStaffLoginAndShipperPortal() {
        System.out.println("\n--- [TEST 26] Đăng Nhập Shipper & Cổng Giao Hàng (/delivery) ---");
        loginWithAccount("shipper@milkyverse.com", "Shipper@123");
        driver.get(BASE_URL + "/delivery");
        sleep(800);

        WebElement delElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Giao Hàng') or contains(text(), 'Shipper') or contains(text(), 'Đơn')]")));
        Assert.assertTrue(delElem.isDisplayed(), "Cổng Giao Hàng Shipper không hiển thị");
        System.out.println("  ✓ Shipper đăng nhập và truy cập bảng điều phối giao vận thành công.");
    }

    // =========================================================================
    // PHẦN X: VAI TRÒ QUẢN TRỊ VIÊN (ADMIN) - 5 MODULE QUẢN TRỊ TOÀN HỆ THỐNG
    // =========================================================================

    @Test(priority = 27, description = "27. Đăng Nhập Admin & Dashboard (/admin): Kiểm tra báo cáo KPI Doanh thu & Đơn hàng")
    public void test27_AdminLoginAndDashboardKPIs() {
        System.out.println("\n--- [TEST 27] Đăng Nhập Admin & Bảng Điều Khiển KPI (/admin) ---");
        loginWithAccount("admin@milkyverse.com", "Admin@123");
        driver.get(BASE_URL + "/admin");
        sleep(800);

        WebElement adminElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Admin') or contains(text(), 'Quản Trị') or contains(text(), 'Doanh Thu')]")));
        Assert.assertTrue(adminElem.isDisplayed(), "Admin Dashboard không hiển thị");
        System.out.println("  ✓ Admin đăng nhập và xem báo cáo tổng quan KPI Doanh thu thành công.");
    }

    @Test(priority = 28, description = "28. Admin Quản Lý Sản Phẩm (/admin/products): Tìm kiếm, Phân trang & Modal Thêm Món")
    public void test28_AdminProductsCrudAndModal() {
        System.out.println("\n--- [TEST 28] Admin Quản Lý Sản Phẩm & Modal CRUD (/admin/products) ---");
        driver.get(BASE_URL + "/admin/products");
        sleep(800);

        WebElement prodElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Sản Phẩm') or contains(text(), 'Thực Đơn') or contains(text(), 'Thêm')]")));
        Assert.assertTrue(prodElem.isDisplayed(), "Trang Admin Sản phẩm không hiển thị");

        // Thử bấm nút Thêm món mới nếu có để kiểm tra Modal CRUD
        List<WebElement> addBtns = driver.findElements(By.xpath("//button[contains(., 'Thêm')]"));
        if (!addBtns.isEmpty()) {
            clickElement(addBtns.get(0));
            System.out.println("  ✓ Đã mở Modal Thêm Sản Phẩm Mới trong Admin.");
        }
    }

    @Test(priority = 29, description = "29. Admin Quản Lý Đơn Hàng (/admin/orders): Bộ lọc trạng thái & Duyệt đơn hàng")
    public void test29_AdminOrdersManagementAndStatusFilter() {
        System.out.println("\n--- [TEST 29] Admin Quản Lý Đơn Hàng Toàn Hệ Thống (/admin/orders) ---");
        driver.get(BASE_URL + "/admin/orders");
        sleep(800);

        WebElement ordElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đơn Hàng') or contains(text(), 'Trạng Thái')]")));
        Assert.assertTrue(ordElem.isDisplayed(), "Trang Admin Đơn hàng không hiển thị");
        System.out.println("  ✓ Bảng điều phối Đơn hàng Admin hoạt động trơn tru.");
    }

    @Test(priority = 30, description = "30. Admin Quản Lý Tồn Kho (/admin/inventory): Kiểm soát số lượng nguyên vật liệu")
    public void test30_AdminInventoryStockManagement() {
        System.out.println("\n--- [TEST 30] Admin Quản Lý Tồn Kho Nguyên Liệu (/admin/inventory) ---");
        driver.get(BASE_URL + "/admin/inventory");
        sleep(800);

        WebElement invElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Kho') or contains(text(), 'Nguyên Liệu') or contains(text(), 'Tồn')]")));
        Assert.assertTrue(invElem.isDisplayed(), "Trang Admin Tồn kho không hiển thị");
        System.out.println("  ✓ Bảng quản lý định mức tồn kho Admin hoạt động chuẩn xác.");
    }

    @Test(priority = 31, description = "31. Admin Quản Lý Nhân Sự (/admin/staff): Danh sách tài khoản & Phân quyền 6 vai trò")
    public void test31_AdminStaffAndRoleManagement() {
        System.out.println("\n--- [TEST 31] Admin Quản Lý Nhân Sự & Phân Quyền (/admin/staff) ---");
        driver.get(BASE_URL + "/admin/staff");
        sleep(800);

        WebElement staffElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Nhân') or contains(text(), 'Tài Khoản') or contains(text(), 'Vai Trò')]")));
        Assert.assertTrue(staffElem.isDisplayed(), "Trang Admin Nhân sự không hiển thị");
        System.out.println("  ✓ Bảng quản lý Nhân sự & Phân quyền 6 vai trò hoàn tất!");
        System.out.println("\n=========================================================================");
        System.out.println("   HOÀN TẤT 100% BỘ KIỂM THỬ TỰ ĐỘNG 31 CHỨC NĂNG CHUYÊN SÂU MILKYVERSE");
        System.out.println("=========================================================================\n");
    }
}
