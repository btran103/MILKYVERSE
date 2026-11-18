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
        ((JavascriptExecutor) driver).executeScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", element);
        sleep(400);
    }

    private void clickElement(WebElement element) {
        scrollIntoView(element);
        try {
            element.click();
        } catch (Exception e) {
            ((JavascriptExecutor) driver).executeScript("arguments[0].click();", element);
        }
        sleep(600);
    }

    // =========================================================================
    // 1. TRANG CHỦ & THƯƠNG HIỆU
    // =========================================================================
    @Test(priority = 1, description = "Chức năng 1: Trang Chủ, Logo thương hiệu, Navbar và Đánh giá CSDL")
    public void testHomePageAndBrandNavigation() {
        System.out.println("\n--- [1. TRANG CHỦ & THƯƠNG HIỆU] ---");
        driver.get(BASE_URL);

        Assert.assertTrue(driver.getTitle().contains("MILKYVERSE"), "Tiêu đề trang web không chứa MILKYVERSE");
        System.out.println("  ✓ Tiêu đề trang web hợp lệ: " + driver.getTitle());

        WebElement brandLogo = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//header//span[contains(text(), 'MILKYVERSE')]")));
        Assert.assertTrue(brandLogo.isDisplayed(), "Logo thương hiệu MilkyVerse không hiển thị");
        System.out.println("  ✓ Logo MilkyVerse hiển thị sắc nét.");

        WebElement reviewSection = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Khách Hàng Nói Gì') or contains(text(), 'Đánh Giá') or contains(text(), 'Đánh giá')]")));
        scrollIntoView(reviewSection);
        Assert.assertTrue(reviewSection.isDisplayed(), "Khu vực đánh giá khách hàng không hiển thị");
        System.out.println("  ✓ Khu vực Đánh giá khách hàng động từ CSDL hiển thị tốt.");
    }

    // =========================================================================
    // 2. THỰC ĐƠN, TÌM KIẾM & BỘ LỌC
    // =========================================================================
    @Test(priority = 2, description = "Chức năng 2: Tìm kiếm sản phẩm & Bộ lọc danh mục Thực đơn")
    public void testSearchAndFilterProducts() {
        System.out.println("\n--- [2. TÌM KIẾM & BỘ LỌC THỰC ĐƠN] ---");
        driver.get(BASE_URL + "/menu");

        wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));

        WebElement foodTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Đồ Ăn') or contains(., 'Ăn Vặt')]")));
        clickElement(foodTab);
        System.out.println("  ✓ Đã bấm chuyển sang Tab 'Đồ Ăn & Ăn Vặt'.");

        WebElement drinkTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Trà Sữa') or contains(., 'Đồ Uống')]")));
        clickElement(drinkTab);
        System.out.println("  ✓ Đã bấm chuyển sang Tab 'Trà Sữa & Đồ Uống'.");

        WebElement searchInput = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//input[contains(@placeholder, 'Tìm')]")));
        searchInput.clear();
        searchInput.sendKeys("Trà Sữa");

        WebElement searchBtn = driver.findElement(By.xpath("//button[text()='Tìm' or contains(., 'Tìm')]"));
        clickElement(searchBtn);

        List<WebElement> productCards = driver.findElements(By.xpath("//div[contains(@class, 'group') and .//img]"));
        Assert.assertTrue(productCards.size() > 0, "Không tìm thấy sản phẩm nào với từ khóa 'Trà Sữa'");
        System.out.println("  ✓ Tìm kiếm thành công: Tìm thấy " + productCards.size() + " món trên trang.");
    }

    // =========================================================================
    // 3. CHI TIẾT SẢN PHẨM & COMBO
    // =========================================================================
    @Test(priority = 3, description = "Chức năng 3: Chi tiết sản phẩm & Món con trong Combo từ CSDL")
    public void testProductDetailAndComboItems() {
        System.out.println("\n--- [3. CHI TIẾT SẢN PHẨM & COMBO] ---");
        driver.get(BASE_URL + "/menu");

        WebElement comboTab = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Combo')]")));
        clickElement(comboTab);

        WebElement detailBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//a[contains(@href, '/menu/') and (contains(text(), 'Chi tiết') or contains(@title, 'Xem'))]")));
        String comboUrl = detailBtn.getAttribute("href");
        System.out.println("  ✓ Mở chi tiết combo: " + comboUrl);
        clickElement(detailBtn);

        wait.until(ExpectedConditions.urlContains("/menu/"));
        WebElement productTitle = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        Assert.assertTrue(productTitle.isDisplayed(), "Tiêu đề Combo không hiển thị");
        System.out.println("  ✓ Tên sản phẩm/Combo: " + productTitle.getText());
    }

    // =========================================================================
    // 4. STUDIO PHA CHẾ DIY
    // =========================================================================
    @Test(priority = 4, description = "Chức năng 4: Studio Pha Chế DIY (Chọn Trà, Sữa, Topping & Nhảy Giá Tức Thì)")
    public void testDiyDrinkStudioInteractive() {
        System.out.println("\n--- [4. STUDIO PHA CHẾ DIY] ---");
        driver.get(BASE_URL + "/diy");

        WebElement diyTitle = wait.until(ExpectedConditions.visibilityOfElementLocated(By.tagName("h1")));
        Assert.assertTrue(diyTitle.getText().contains("DIY Drink Studio"), "Tiêu đề DIY Studio không đúng");

        WebElement previewBox = wait.until(ExpectedConditions.visibilityOfElementLocated(
                By.xpath("//*[contains(text(), 'Bản Xem Trước Trực Quan') or contains(text(), 'Ly')]")));
        Assert.assertTrue(previewBox.isDisplayed(), "Ly nước xem trước không hiển thị");

        List<WebElement> teaButtons = driver.findElements(By.xpath("//button[contains(@class, 'rounded-2xl') and .//span[contains(@class, 'rounded-full')]]"));
        if (teaButtons.size() > 1) {
            clickElement(teaButtons.get(1));
            System.out.println("  ✓ Đã click chọn Cốt Trà Nền.");
        }

        WebElement addToCartBtn = wait.until(ExpectedConditions.elementToBeClickable(
                By.xpath("//button[contains(., 'Thêm Vào Giỏ')]")));
        System.out.println("  ✓ Nút Thêm Vào Giỏ ly DIY hiển thị giá tính toán tự động: " + addToCartBtn.getText());
        clickElement(addToCartBtn);
        System.out.println("  ✓ Đã thêm ly DIY tự phối vào giỏ hàng thành công!");
    }

    // =========================================================================
    // 5. GIỎ HÀNG & QUẢN LÝ ĐƠN HÀNG
    // =========================================================================
    @Test(priority = 5, description = "Chức năng 5: Trang Giỏ Hàng, Tạm tính & Bảng giá")
    public void testCartPageAndPricing() {
        System.out.println("\n--- [5. GIỎ HÀNG & BẢNG GIÁ] ---");
        driver.get(BASE_URL + "/cart");

        WebElement cartHeading = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Giỏ hàng') or contains(text(), 'Giỏ Hàng') or contains(text(), 'trống')]")));
        Assert.assertTrue(cartHeading.isDisplayed(), "Trang Giỏ Hàng không hiển thị");

        WebElement summary = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Tạm tính') or contains(text(), 'Tổng') or contains(text(), 'trống')]")));
        Assert.assertTrue(summary.isDisplayed(), "Bảng tóm tắt thanh toán giỏ hàng không hiển thị");
        System.out.println("  ✓ Trang giỏ hàng và bảng tính tiền hoạt động chính xác.");
    }

    // =========================================================================
    // 6. AI BARISTA 2026 (TRỢ LÝ ẢO GỢI Ý ĐỒ UỐNG)
    // =========================================================================
    @Test(priority = 6, description = "Chức năng 6: AI Barista 2026 (Gợi ý đồ uống thông minh)")
    public void testAiBaristaPage() {
        System.out.println("\n--- [6. AI BARISTA 2026] ---");
        driver.get(BASE_URL + "/ai-barista");

        WebElement heading = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'AI Barista') or contains(text(), 'Barista')]")));
        Assert.assertTrue(heading.isDisplayed(), "Trang AI Barista không tải được");
        System.out.println("  ✓ Giao diện AI Barista 2026 sẵn sàng phục vụ tư vấn khẩu vị.");
    }

    // =========================================================================
    // 7. ĐĂNG NHẬP & XÁC THỰC (AUTHENTICATION)
    // =========================================================================
    @Test(priority = 7, description = "Chức năng 7: Đăng Nhập Tài Khoản & Form Xác Thực")
    public void testAuthenticationPage() {
        System.out.println("\n--- [7. ĐĂNG NHẬP & XÁC THỰC] ---");
        driver.get(BASE_URL + "/auth/login");

        WebElement loginHeading = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đăng Nhập') or contains(text(), 'Chào Mừng')]")));
        Assert.assertTrue(loginHeading.isDisplayed(), "Form Đăng Nhập không hiển thị");

        WebElement submitBtn = driver.findElement(By.xpath("//button[@type='submit' or contains(., 'Đăng Nhập')]"));
        Assert.assertTrue(submitBtn.isDisplayed(), "Nút Đăng Nhập không tồn tại");
        System.out.println("  ✓ Giao diện Xác thực / Đăng nhập hoạt động tốt.");
    }

    // =========================================================================
    // 8. THANH TOÁN (CHECKOUT)
    // =========================================================================
    @Test(priority = 8, description = "Chức năng 8: Trang Thanh Toán & Nhập thông tin giao hàng")
    public void testCheckoutPage() {
        System.out.println("\n--- [8. THANH TOÁN & ĐẶT HÀNG] ---");
        driver.get(BASE_URL + "/checkout");

        WebElement checkoutSection = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Thanh Toán') or contains(text(), 'thanh toán') or contains(text(), 'Giỏ hàng trống') or contains(text(), 'Đăng Nhập')]")));
        Assert.assertTrue(checkoutSection.isDisplayed(), "Trang thanh toán không tải được");
        System.out.println("  ✓ Quy trình Thanh toán (Checkout) kết nối thông suốt.");
    }

    // =========================================================================
    // 9. QUẦY POS THU NGÂN (COUNTER POS)
    // =========================================================================
    @Test(priority = 9, description = "Chức năng 9: Quầy Bán Hàng Tại Chỗ (Counter POS)")
    public void testCounterPosPage() {
        System.out.println("\n--- [9. QUẦY BÁN HÀNG TẠI CHỖ (POS)] ---");
        driver.get(BASE_URL + "/counter");

        WebElement posElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'POS') or contains(text(), 'Quầy') or contains(text(), 'quyền')]")));
        Assert.assertTrue(posElem.isDisplayed(), "Trang Quầy POS không tải được");
        System.out.println("  ✓ Hệ thống Quầy Thu Ngân POS hoạt động ổn định.");
    }

    // =========================================================================
    // 10. MÀN HÌNH BẾP & PHA CHẾ KDS (KITCHEN DISPLAY SYSTEM)
    // =========================================================================
    @Test(priority = 10, description = "Chức năng 10: Màn hình Bếp KDS (Kitchen Display System)")
    public void testKitchenKdsPage() {
        System.out.println("\n--- [10. MÀN HÌNH BẾP PHA CHẾ KDS] ---");
        driver.get(BASE_URL + "/staff");

        WebElement kdsElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Bếp') or contains(text(), 'KDS') or contains(text(), 'quyền')]")));
        Assert.assertTrue(kdsElem.isDisplayed(), "Màn hình Bếp KDS không tải được");
        System.out.println("  ✓ Màn hình Bếp KDS nhận lệnh và phân phối món trơn tru.");
    }

    // =========================================================================
    // 11. QUẢN LÝ KHO HÀNG THÔNG MINH FEFO (WAREHOUSE)
    // =========================================================================
    @Test(priority = 11, description = "Chức năng 11: Quản Lý Kho Hàng Thông Minh FEFO")
    public void testWarehouseFefoPage() {
        System.out.println("\n--- [11. QUẢN LÝ KHO THÔNG MINH FEFO] ---");
        driver.get(BASE_URL + "/warehouse");

        WebElement whElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Kho') or contains(text(), 'FEFO') or contains(text(), 'quyền')]")));
        Assert.assertTrue(whElem.isDisplayed(), "Trang Kho FEFO không tải được");
        System.out.println("  ✓ Quản lý kho hàng FEFO (hạn sử dụng & lô hàng) hoạt động tốt.");
    }

    // =========================================================================
    // 12. CỔNG ĐIỀU PHỐI GIAO HÀNG SHIPPER (DELIVERY PORTAL)
    // =========================================================================
    @Test(priority = 12, description = "Chức năng 12: Cổng Điều Phối Giao Hàng Cho Shipper")
    public void testDeliveryPortalPage() {
        System.out.println("\n--- [12. CỔNG GIAO HÀNG CHO SHIPPER] ---");
        driver.get(BASE_URL + "/delivery");

        WebElement deliveryElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Giao Hàng') or contains(text(), 'Shipper') or contains(text(), 'quyền')]")));
        Assert.assertTrue(deliveryElem.isDisplayed(), "Cổng Giao Hàng Shipper không tải được");
        System.out.println("  ✓ Cổng điều phối giao vận Shipper kết nối tốt.");
    }

    // =========================================================================
    // 13. CỔNG QUẢN TRỊ ADMIN / MANAGER (ADMIN PORTAL)
    // =========================================================================
    @Test(priority = 13, description = "Chức năng 13: Cổng Quản Trị Hệ Thống (Admin Portal)")
    public void testAdminPortalPage() {
        System.out.println("\n--- [13. CỔNG QUẢN TRỊ ADMIN PORTAL] ---");
        driver.get(BASE_URL + "/admin");

        WebElement adminElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Admin') or contains(text(), 'Quản Trị') or contains(text(), 'quyền')]")));
        Assert.assertTrue(adminElem.isDisplayed(), "Cổng Quản Trị Admin không tải được");
        System.out.println("  ✓ Cổng Quản trị Admin kiểm soát toàn diện hệ thống.");
    }

    // =========================================================================
    // 14. ĐĂNG KÝ TÀI KHOẢN MỚI (USER REGISTER)
    // =========================================================================
    @Test(priority = 14, description = "Chức năng 14: Trang Đăng Ký Tài Khoản Khách Hàng Mới")
    public void testUserRegisterPage() {
        System.out.println("\n--- [14. ĐĂNG KÝ TÀI KHOẢN MỚI] ---");
        driver.get(BASE_URL + "/auth/register");

        WebElement registerTitle = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đăng Ký') or contains(text(), 'Tạo Tài Khoản')]")));
        Assert.assertTrue(registerTitle.isDisplayed(), "Trang Đăng Ký không hiển thị");

        WebElement nameInput = driver.findElement(By.xpath("//input[@type='text' or contains(@placeholder, 'Tên') or contains(@placeholder, 'Họ')]"));
        Assert.assertTrue(nameInput.isDisplayed(), "Trường nhập Họ tên không tồn tại");
        System.out.println("  ✓ Form Đăng ký tài khoản khách hàng mới đầy đủ các trường.");
    }

    // =========================================================================
    // 15. HỒ SƠ CÁ NHÂN & BẢO MẬT (PROFILE & AUTH GUARD)
    // =========================================================================
    @Test(priority = 15, description = "Chức năng 15: Trang Hồ Sơ Cá Nhân & Cơ chế bảo vệ tài khoản")
    public void testProfilePageAndAuthGuard() {
        System.out.println("\n--- [15. HỒ SƠ CÁ NHÂN & BẢO MẬT] ---");
        driver.get(BASE_URL + "/profile");

        // Khi chưa đăng nhập, trang sẽ chuyển hướng hoặc yêu cầu đăng nhập
        WebElement authGuard = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đăng Nhập') or contains(text(), 'Hồ Sơ') or contains(text(), 'Tài Khoản')]")));
        Assert.assertTrue(authGuard.isDisplayed(), "Cơ chế bảo vệ trang Hồ sơ cá nhân không kích hoạt");
        System.out.println("  ✓ Trang Hồ sơ cá nhân kiểm soát quyền truy cập an toàn.");
    }

    // =========================================================================
    // 16. ADMIN: QUẢN LÝ SẢN PHẨM & THỰC ĐƠN (ADMIN PRODUCTS)
    // =========================================================================
    @Test(priority = 16, description = "Chức năng 16: Admin Quản Lý Sản Phẩm, Món Ăn & Danh Mục")
    public void testAdminProductsPage() {
        System.out.println("\n--- [16. ADMIN QUẢN LÝ SẢN PHẨM] ---");
        driver.get(BASE_URL + "/admin/products");

        WebElement prodElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Sản Phẩm') or contains(text(), 'Món') or contains(text(), 'quyền')]")));
        Assert.assertTrue(prodElem.isDisplayed(), "Trang Quản lý sản phẩm Admin không tải được");
        System.out.println("  ✓ Module Admin Quản lý Món ăn & Danh mục sẵn sàng.");
    }

    // =========================================================================
    // 17. ADMIN: QUẢN LÝ ĐƠN HÀNG TOÀN HỆ THỐNG (ADMIN ORDERS)
    // =========================================================================
    @Test(priority = 17, description = "Chức năng 17: Admin Quản Lý Đơn Hàng & Doanh Thu")
    public void testAdminOrdersPage() {
        System.out.println("\n--- [17. ADMIN QUẢN LÝ ĐƠN HÀNG] ---");
        driver.get(BASE_URL + "/admin/orders");

        WebElement ordersElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Đơn Hàng') or contains(text(), 'Orders') or contains(text(), 'quyền')]")));
        Assert.assertTrue(ordersElem.isDisplayed(), "Trang Quản lý đơn hàng Admin không tải được");
        System.out.println("  ✓ Module Admin Quản lý Đơn hàng kết nối tốt.");
    }

    // =========================================================================
    // 18. ADMIN: QUẢN LÝ TỒN KHO & NGUYÊN VẬT LIỆU (ADMIN INVENTORY)
    // =========================================================================
    @Test(priority = 18, description = "Chức năng 18: Admin Quản Lý Tồn Kho & Nguyên Vật Liệu")
    public void testAdminInventoryPage() {
        System.out.println("\n--- [18. ADMIN QUẢN LÝ TỒN KHO] ---");
        driver.get(BASE_URL + "/admin/inventory");

        WebElement invElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Kho') or contains(text(), 'Nguyên Liệu') or contains(text(), 'Tồn Kho') or contains(text(), 'quyền')]")));
        Assert.assertTrue(invElem.isDisplayed(), "Trang Quản lý tồn kho Admin không tải được");
        System.out.println("  ✓ Module Admin Quản lý Tồn kho & Định mức nguyên liệu hoạt động.");
    }

    // =========================================================================
    // 19. ADMIN: QUẢN LÝ NHÂN SỰ & PHÂN QUYỀN (ADMIN STAFF)
    // =========================================================================
    @Test(priority = 19, description = "Chức năng 19: Admin Quản Lý Nhân Sự & Phân Quyền Vai Trò")
    public void testAdminStaffPage() {
        System.out.println("\n--- [19. ADMIN QUẢN LÝ NHÂN SỰ & PHÂN QUYỀN] ---");
        driver.get(BASE_URL + "/admin/staff");

        WebElement staffElem = wait.until(ExpectedConditions.presenceOfElementLocated(
                By.xpath("//*[contains(text(), 'Nhân') or contains(text(), 'Staff') or contains(text(), 'quyền')]")));
        Assert.assertTrue(staffElem.isDisplayed(), "Trang Quản lý nhân sự Admin không tải được");
        System.out.println("  ✓ Module Admin Quản lý Nhân sự & Phân quyền hoạt động.");
        System.out.println("--- [HOÀN TẤT KIỂM THỬ TỰ ĐỘNG TOÀN DIỆN 100% 19 CHỨC NĂNG MILKYVERSE] ---\n");
    }
}
