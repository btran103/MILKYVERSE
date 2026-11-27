using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;
using MilkyVerse.Api.Services;
using Moq;
using Xunit;

namespace MilkyVerse.Tests;

public class CodOrderDeliveryTests
{
    private static MilkyVerseDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MilkyVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new MilkyVerseDbContext(options);
    }

    private static async Task<(User Customer, User Staff, User Shipper1, User Shipper2, Product Product)> SeedActorsAndProductAsync(MilkyVerseDbContext db)
    {
        var customer = new User
        {
            FullName = "Nguyễn Khách Hàng",
            Email = "customer.cod@milkyverse.com",
            PasswordHash = "hash",
            PhoneNumber = "0901112222",
            Address = "123 Lê Lợi, Q1, TP.HCM",
            Role = "Customer",
            IsActive = true
        };
        var staff = new User
        {
            FullName = "Trần Nhân Viên Quầy",
            Email = "staff.cod@milkyverse.com",
            PasswordHash = "hash",
            PhoneNumber = "0903334444",
            Role = "Staff",
            IsActive = true
        };
        var shipper1 = new User
        {
            FullName = "Lê Shipper Một",
            Email = "shipper1@milkyverse.com",
            PasswordHash = "hash",
            PhoneNumber = "0905556666",
            Role = "DeliveryStaff",
            IsActive = true
        };
        var shipper2 = new User
        {
            FullName = "Phạm Shipper Hai",
            Email = "shipper2@milkyverse.com",
            PasswordHash = "hash",
            PhoneNumber = "0907778888",
            Role = "DeliveryStaff",
            IsActive = true
        };
        var product = new Product
        {
            Name = "Trà Sữa Ô Long Sữa Tươi",
            Slug = "tra-sua-o-long-sua-tuoi",
            BasePrice = 42000,
            ProductType = "DRINK",
            IsActive = true,
            StockStatus = 1
        };

        db.Users.AddRange(customer, staff, shipper1, shipper2);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (customer, staff, shipper1, shipper2, product);
    }

    private static CreateOrderRequest MakeOrderRequest(int productId, string productName, string paymentMethod = "COD")
        => new()
        {
            CustomerName = "Nguyễn Khách Hàng",
            CustomerPhone = "0901112222",
            ShippingAddress = "123 Lê Lợi, Q1, TP.HCM",
            Note = "Giao giờ hành chính",
            PaymentMethod = paymentMethod,
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = productId,
                    ProductName = productName,
                    ItemType = "DRINK",
                    Size = "M",
                    SugarLevel = "50%",
                    IceLevel = "70%",
                    Quantity = 2
                }
            }
        };

    // Test 1: Đặt hàng COD thành công, đơn không tự hủy (PendingConfirmation + Unpaid + không có PaymentExpiresAt)
    [Fact]
    public async Task T01_CreateCodOrder_StartsPendingConfirmationUnpaid_AndNeverSetsExpiry()
    {
        using var db = CreateDb();
        var (customer, _, _, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);

        var res = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));

        Assert.True(res.Success);
        Assert.NotNull(res.Data);
        Assert.Equal("COD", res.Data!.PaymentMethod);
        Assert.Equal(OrderWorkflow.StatusPendingConfirmation, res.Data.Status);
        Assert.Equal(OrderWorkflow.PaymentUnpaid, res.Data.PaymentStatus);
        Assert.Null(res.Data.PaymentExpiresAt);
        Assert.Equal(0, res.Data.RemainingPaymentSeconds);
        Assert.True(res.Data.CanCancel);
    }

    // Test 2: COD không bị ảnh hưởng bởi bộ đếm 2 phút MoMo hoặc job hết hạn online
    [Fact]
    public async Task T02_CodOrder_NotAffectedByMomo2MinuteExpirationOrBackgroundSweep()
    {
        using var db = CreateDb();
        var (customer, _, _, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        Assert.True(created.Success);

        // Simulate 10 minutes passing
        var dbOrder = await db.Orders.FindAsync(created.Data!.Id);
        dbOrder!.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        await db.SaveChangesAsync();

        int expiredCount = await orderService.ExpireOverdueOnlinePaymentsAsync();
        Assert.Equal(0, expiredCount);

        var fetched = await orderService.GetOrderByIdAsync(dbOrder.Id, customer.Id);
        Assert.True(fetched.Success);
        Assert.Equal(OrderWorkflow.StatusPendingConfirmation, fetched.Data!.Status);
        Assert.Equal(OrderWorkflow.PaymentUnpaid, fetched.Data.PaymentStatus);
        Assert.NotEqual(OrderWorkflow.StatusCancelled, fetched.Data.Status);
    }

    // Test 3: Nhân viên xác nhận đơn COD thành công nhưng PaymentStatus vẫn là Unpaid (chặn đánh dấu Paid sớm)
    [Fact]
    public async Task T03_StaffConfirmCodOrder_UpdatesOrderStatus_KeepsPaymentUnpaid()
    {
        using var db = CreateDb();
        var (customer, staff, _, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        // Premature Paid attempt on Confirmed must fail
        var prematurePaid = await orderService.UpdateOrderStatusAsync(
            orderId,
            new UpdateOrderStatusRequest { Status = "Confirmed", PaymentStatus = "Paid", Note = "Thử thu sớm" },
            staff.Id, "Staff", staff.FullName);
        Assert.False(prematurePaid.Success);

        // Valid staff confirmation keeps PaymentStatus = Unpaid
        var confirmRes = await orderService.UpdateOrderStatusAsync(
            orderId,
            new UpdateOrderStatusRequest { Status = "Confirmed", Note = "Quầy xác nhận đơn COD" },
            staff.Id, "Staff", staff.FullName);

        Assert.True(confirmRes.Success);
        Assert.Equal("Confirmed", confirmRes.Data!.Status);
        Assert.Equal("Unpaid", confirmRes.Data.PaymentStatus);
    }

    // Test 4 & 5: Chuyển trạng thái qua Preparing -> ReadyForPickup -> AssignShipper -> Delivering & Khách xem được Đang giao hàng
    [Fact]
    public async Task T04_T05_OrderMovesToDelivering_AndCustomerSeesDeliveringWithShipperInfo()
    {
        using var db = CreateDb();
        var (customer, staff, shipper1, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);
        var deliveryService = new DeliveryService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Preparing" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "ReadyForPickup" }, staff.Id, "Staff", staff.FullName);

        var assignRes = await deliveryService.AssignShipperAsync(new AssignShipperRequest { OrderId = orderId, ShipperId = shipper1.Id }, staff.Id, staff.FullName, "Staff");
        Assert.True(assignRes.Success);

        var transitRes = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "InTransit", Notes = "Đang di chuyển tới khách" });
        Assert.True(transitRes.Success);

        // Customer queries order details
        var customerView = await orderService.GetOrderByIdAsync(orderId, customer.Id);
        Assert.True(customerView.Success);
        Assert.Equal("Delivering", customerView.Data!.Status);
        Assert.Equal("Unpaid", customerView.Data.PaymentStatus);
        Assert.Equal(shipper1.Id, customerView.Data.ShipperId);
        Assert.Equal(shipper1.FullName, customerView.Data.ShipperName);
        Assert.Contains(customerView.Data.StatusHistories, h => h.Status == "Delivering");
    }

    // Test 6, 7, 8: Shipper xác nhận giao thành công + thu tiền COD -> Delivered & PaymentStatus Paid + audit
    [Fact]
    public async Task T06_T07_T08_ShipperConfirmsDeliveryAndCodCollection_UpdatesDeliveredAndPaidWithAudit()
    {
        using var db = CreateDb();
        var (customer, staff, shipper1, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);
        var deliveryService = new DeliveryService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Preparing" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "ReadyForPickup" }, staff.Id, "Staff", staff.FullName);
        await deliveryService.AssignShipperAsync(new AssignShipperRequest { OrderId = orderId, ShipperId = shipper1.Id }, staff.Id, staff.FullName, "Staff");
        await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "InTransit" });

        // Must fail if shipper tries to complete COD order WITHOUT confirming cash collected (IsCodCollected = false)
        var failNoCash = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest
        {
            Status = "Delivered",
            IsCodCollected = false
        });
        Assert.False(failNoCash.Success);

        // Succeeds only when IsCodCollected = true
        var okDeliver = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest
        {
            Status = "Delivered",
            IsCodCollected = true,
            Notes = "Đã giao và nhận đủ tiền mặt"
        });
        Assert.True(okDeliver.Success);

        var customerView = await orderService.GetOrderByIdAsync(orderId, customer.Id);
        Assert.Equal("Delivered", customerView.Data!.Status);
        Assert.Equal("Paid", customerView.Data.PaymentStatus);
        Assert.NotNull(customerView.Data.PaidAt);
        Assert.Equal(shipper1.Id, customerView.Data.PaymentConfirmedByUserId);
        Assert.Equal(shipper1.FullName, customerView.Data.PaymentConfirmedByName);
        Assert.True(customerView.Data.IsCodCollected);
        Assert.NotEmpty(customerView.Data.PaymentTransactions);
        Assert.Equal("COD", customerView.Data.PaymentTransactions[0].PaymentMethod);
        Assert.Equal("Success", customerView.Data.PaymentTransactions[0].PaymentStatus);
    }

    // Test 9: Giao hàng thất bại / Khách từ chối nhận không được ghi nhận thanh toán
    [Fact]
    public async Task T09_DeliveryFailed_DoesNotMarkPaid_AndRecordsReason()
    {
        using var db = CreateDb();
        var (customer, staff, shipper1, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);
        var deliveryService = new DeliveryService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Preparing" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "ReadyForPickup" }, staff.Id, "Staff", staff.FullName);
        await deliveryService.AssignShipperAsync(new AssignShipperRequest { OrderId = orderId, ShipperId = shipper1.Id }, staff.Id, staff.FullName, "Staff");
        await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "InTransit" });

        // Reject attempt to pass IsCodCollected=true on failure
        var invalidFail = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest
        {
            Status = "Failed",
            IsCodCollected = true,
            FailureReason = "Khách không nghe máy"
        });
        Assert.False(invalidFail.Success);

        // Proper delivery failure
        var failRes = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest
        {
            Status = "Failed",
            IsCodCollected = false,
            FailureReason = "Khách không nghe máy (Gọi 3 lần)"
        });
        Assert.True(failRes.Success);

        var orderView = await orderService.GetOrderByIdAsync(orderId, customer.Id);
        Assert.Equal("DeliveryFailed", orderView.Data!.Status);
        Assert.Equal("Unpaid", orderView.Data.PaymentStatus);
        Assert.False(orderView.Data.IsCodCollected);
        Assert.Equal("Khách không nghe máy (Gọi 3 lần)", orderView.Data.DeliveryFailureReason);

        // Allow redelivery flow back to Delivering
        var redeliverRes = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest
        {
            Status = "InTransit",
            Notes = "Hẹn giao lại lần 2"
        });
        Assert.True(redeliverRes.Success);
        var redeliverView = await orderService.GetOrderByIdAsync(orderId, customer.Id);
        Assert.Equal("Delivering", redeliverView.Data!.Status);
    }

    // Test 10: Shipper không được cập nhật đơn không thuộc quyền phụ trách
    [Fact]
    public async Task T10_UnassignedShipper_ForbiddenFromUpdatingAnotherShippersOrder()
    {
        using var db = CreateDb();
        var (customer, staff, shipper1, shipper2, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);
        var deliveryService = new DeliveryService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Preparing" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "ReadyForPickup" }, staff.Id, "Staff", staff.FullName);
        await deliveryService.AssignShipperAsync(new AssignShipperRequest { OrderId = orderId, ShipperId = shipper1.Id }, staff.Id, staff.FullName, "Staff");

        // Shipper2 tries to hijack delivery
        var hijackTransit = await deliveryService.UpdateDeliveryStatusAsync(shipper2.Id, orderId, new UpdateDeliveryStatusRequest { Status = "InTransit" });
        Assert.False(hijackTransit.Success);
        Assert.StartsWith("FORBIDDEN:", hijackTransit.Message);

        var hijackOrderApi = await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Delivering" }, shipper2.Id, "DeliveryStaff", shipper2.FullName);
        Assert.False(hijackOrderApi.Success);
        Assert.StartsWith("FORBIDDEN:", hijackOrderApi.Message);
    }

    // Test 11: Đơn MoMo hết hạn vẫn được xử lý theo cơ chế hết hạn riêng
    [Fact]
    public async Task T11_MomoOrder_ExpiresAfter2Minutes_IndependentlyFromCod()
    {
        using var db = CreateDb();
        var (customer, _, _, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);

        var momoOrderRes = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "MOMO"));
        var codOrderRes = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));

        Assert.NotNull(momoOrderRes.Data!.PaymentExpiresAt);
        Assert.Null(codOrderRes.Data!.PaymentExpiresAt);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Momo:PartnerCode"] = "MOMO",
                ["Momo:AccessKey"] = "test-access",
                ["Momo:SecretKey"] = "test-secret"
            })
            .Build();
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var momoService = new MomoPaymentService(db, config, httpFactoryMock.Object, NullLogger<MomoPaymentService>.Instance);
        var createTx = await momoService.CreateMomoPaymentAsync(customer.Id, new CreateMomoPaymentRequest { OrderId = momoOrderRes.Data.Id });
        Assert.True(createTx.Success);

        // Expire MoMo transaction past 120s AND backdate both orders > 3 minutes
        var momoEntity = await db.Orders.FindAsync(momoOrderRes.Data.Id);
        var txEntity = await db.PaymentTransactions.FirstAsync(pt => pt.OrderId == momoOrderRes.Data.Id);
        momoEntity!.PaymentExpiresAt = DateTime.UtcNow.AddSeconds(-10);
        txEntity.PaymentExpiresAt = DateTime.UtcNow.AddSeconds(-10);
        var codEntity = await db.Orders.FindAsync(codOrderRes.Data.Id);
        codEntity!.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        // Sweep online overdue payments
        await orderService.ExpireOverdueOnlinePaymentsAsync();

        var refreshedMomo = await momoService.GetPaymentStatusAsync(momoOrderRes.Data.Id, customer.Id);
        Assert.True(refreshedMomo.Data!.IsExpired);
        Assert.Equal("Expired", refreshedMomo.Data.PaymentStatus);

        var refreshedCod = await orderService.GetOrderByIdAsync(codOrderRes.Data.Id, customer.Id);
        Assert.Equal(OrderWorkflow.StatusPendingConfirmation, refreshedCod.Data!.Status);
        Assert.Equal(OrderWorkflow.PaymentUnpaid, refreshedCod.Data.PaymentStatus);
    }

    // Test 12: Không xảy ra cập nhật trạng thái sai quy trình hoặc xác nhận thanh toán trùng lặp
    [Fact]
    public async Task T12_PreventsInvalidTransitions_AndDuplicatePaymentConfirmation()
    {
        using var db = CreateDb();
        var (customer, staff, shipper1, _, product) = await SeedActorsAndProductAsync(db);
        var orderService = new OrderService(db);
        var deliveryService = new DeliveryService(db);

        var created = await orderService.CreateOrderAsync(customer.Id, MakeOrderRequest(product.Id, product.Name, "COD"));
        var orderId = created.Data!.Id;

        // Invalid jump: PendingConfirmation -> Delivering directly (skipping Confirmed/Preparing/Ready)
        var invalidJump = await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Delivering" }, staff.Id, "Staff", staff.FullName);
        Assert.False(invalidJump.Success);

        // Complete full valid flow
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Preparing" }, staff.Id, "Staff", staff.FullName);
        await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "ReadyForPickup" }, staff.Id, "Staff", staff.FullName);
        await deliveryService.AssignShipperAsync(new AssignShipperRequest { OrderId = orderId, ShipperId = shipper1.Id }, staff.Id, staff.FullName, "Staff");
        await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "InTransit" });

        var firstDeliver = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "Delivered", IsCodCollected = true });
        Assert.True(firstDeliver.Success);

        // Duplicate delivery / duplicate COD payment must fail
        var secondDeliver = await deliveryService.UpdateDeliveryStatusAsync(shipper1.Id, orderId, new UpdateDeliveryStatusRequest { Status = "Delivered", IsCodCollected = true });
        Assert.False(secondDeliver.Success);

        // Backward status change after Delivered must fail
        var backwardTransition = await orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "Confirmed" }, staff.Id, "Staff", staff.FullName);
        Assert.False(backwardTransition.Success);

        // Exactly 1 COD PaymentTransaction exists
        var txCount = await db.PaymentTransactions.CountAsync(pt => pt.OrderId == orderId);
        Assert.Equal(1, txCount);
    }
}
