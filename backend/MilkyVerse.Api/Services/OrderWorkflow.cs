namespace MilkyVerse.Api.Services;

/// <summary>
/// Central state machine & rules for order lifecycle, COD vs Online payment separation, and delivery transitions.
/// </summary>
public static class OrderWorkflow
{
    public const string StatusPendingConfirmation = "PendingConfirmation";
    public const string StatusPending = "Pending";
    public const string StatusConfirmed = "Confirmed";
    public const string StatusPreparing = "Preparing";
    public const string StatusReadyForPickup = "ReadyForPickup";
    public const string StatusDelivering = "Delivering";
    public const string StatusDelivered = "Delivered";
    public const string StatusCompleted = "Completed"; // Legacy alias compatible with Delivered
    public const string StatusDeliveryFailed = "DeliveryFailed";
    public const string StatusRejected = "Rejected";
    public const string StatusCancelled = "Cancelled";

    public const string PaymentUnpaid = "Unpaid";
    public const string PaymentPending = "Pending";
    public const string PaymentPaid = "Paid";
    public const string PaymentSuccess = "Success";
    public const string PaymentExpired = "Expired";
    public const string PaymentFailed = "Failed";
    public const string PaymentCancelled = "Cancelled";

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [StatusPendingConfirmation] = new(StringComparer.OrdinalIgnoreCase) { StatusConfirmed, StatusCancelled },
        [StatusPending] = new(StringComparer.OrdinalIgnoreCase) { StatusConfirmed, StatusCancelled },
        [StatusConfirmed] = new(StringComparer.OrdinalIgnoreCase) { StatusPreparing, StatusReadyForPickup, StatusDelivering, StatusCancelled },
        [StatusPreparing] = new(StringComparer.OrdinalIgnoreCase) { StatusReadyForPickup, StatusDelivering, StatusCancelled },
        [StatusReadyForPickup] = new(StringComparer.OrdinalIgnoreCase) { StatusDelivering, StatusCancelled },
        [StatusDelivering] = new(StringComparer.OrdinalIgnoreCase) { StatusDelivered, StatusCompleted, StatusDeliveryFailed, StatusRejected, StatusCancelled },
        [StatusDeliveryFailed] = new(StringComparer.OrdinalIgnoreCase) { StatusReadyForPickup, StatusDelivering, StatusCancelled },
    };

    public static bool IsCod(string? paymentMethod)
        => string.Equals(paymentMethod?.Trim(), "COD", StringComparison.OrdinalIgnoreCase);

    public static bool IsOnlinePayment(string? paymentMethod)
        => !IsCod(paymentMethod);

    public static bool IsPaid(string? paymentStatus)
        => string.Equals(paymentStatus, PaymentPaid, StringComparison.OrdinalIgnoreCase)
        || string.Equals(paymentStatus, PaymentSuccess, StringComparison.OrdinalIgnoreCase);

    public static bool IsTerminalStatus(string? status)
        => string.Equals(status, StatusDelivered, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, StatusCompleted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, StatusRejected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, StatusCancelled, StringComparison.OrdinalIgnoreCase);

    public static bool IsCustomerCancellableStatus(string? status)
        => string.Equals(status, StatusPendingConfirmation, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, StatusPending, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, StatusConfirmed, StringComparison.OrdinalIgnoreCase);

    public static bool CanTransition(string? currentStatus, string? targetStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(targetStatus))
            return false;

        if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase))
            return true;

        return AllowedTransitions.TryGetValue(currentStatus, out var nextStates) && nextStates.Contains(targetStatus);
    }

    public static string NormalizeDeliveryTargetStatus(string? requestedStatus)
    {
        return requestedStatus?.Trim() switch
        {
            "PickedUp" => "PickedUp",
            "InTransit" => "InTransit",
            "Delivered" or "Completed" => "Delivered",
            "Failed" or "DeliveryFailed" => "DeliveryFailed",
            "Rejected" => "Rejected",
            _ => requestedStatus?.Trim() ?? string.Empty
        };
    }
}
