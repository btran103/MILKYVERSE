using MilkyVerse.Api.DTOs;

namespace MilkyVerse.Api.Services;

public interface ISePayService
{
    Task<ApiResponse<SePayQrResponse>> GetOrCreateSePayQrAsync(int orderId, int? userId = null);
    Task<ApiResponse<bool>> ProcessSePayWebhookAsync(SePayWebhookRequest request, string? authHeader);
    Task<ApiResponse<SePayQrResponse>> GetSePayStatusAsync(int orderId, int? userId = null);
}
