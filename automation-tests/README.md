# MilkyVerse - Automation Test Suite

Bộ kiểm thử tự động chính thức cho nền tảng Thương Mại Điện Tử MilkyVerse:
1. **Selenium Java + TestNG**: E2E Web UI Testing (Chrome tự động thao tác trực quan).
2. **Postman / Newman**: Backend REST API Integration Testing.

---

## 1. Selenium Java + TestNG (UI Automation Testing)
- **Thư mục:** `selenium-testng/`
- **Công nghệ:** Java 25, Selenium WebDriver 4.28.0, TestNG 7.10.2, WebDriverManager.
- **Tích hợp sẵn:** Apache Maven 3.9.9 portable (không cần cài thủ công Maven vào máy).

### Cách chạy:
- **Cách 1 (Nhanh nhất):** Click đúp file `run-selenium-tests.bat`
- **Cách 2 (Terminal):**
  ```powershell
  cd automation-tests\selenium-testng
  ..\maven\apache-maven-3.9.9\bin\mvn clean test
  ```

---

## 2. Postman / Newman (API Automation Testing)
- **Thư mục:** `postman/`
- **Tập tin:**
  - `MilkyVerse_API.postman_collection.json`
  - `MilkyVerse_Local.postman_environment.json`

### Cách chạy:
- **Cách 1:** Click đúp file `run-postman-api.bat`
- **Cách 2:** Mở app Postman Desktop -> Import 2 file trên -> Run Collection.
