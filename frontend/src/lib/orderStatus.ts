export function isCodPayment(paymentMethod?: string): boolean {
  return (paymentMethod || "").trim().toUpperCase() === "COD";
}

export function isPaymentPaid(paymentStatus?: string): boolean {
  const s = (paymentStatus || "").trim().toLowerCase();
  return s === "paid" || s === "success";
}

export function getOrderStatusMeta(status?: string): {
  label: string;
  badgeClass: string;
  isTerminal: boolean;
} {
  switch (status) {
    case "PendingConfirmation":
    case "Pending":
      return {
        label: "Chờ xác nhận",
        badgeClass: "bg-amber-100 text-amber-800 border border-amber-200",
        isTerminal: false,
      };
    case "Confirmed":
      return {
        label: "Đã xác nhận",
        badgeClass: "bg-blue-100 text-blue-800 border border-blue-200",
        isTerminal: false,
      };
    case "Preparing":
      return {
        label: "Đang chuẩn bị",
        badgeClass: "bg-orange-100 text-orange-800 border border-orange-200",
        isTerminal: false,
      };
    case "ReadyForPickup":
      return {
        label: "Sẵn sàng giao hàng",
        badgeClass: "bg-purple-100 text-purple-800 border border-purple-200",
        isTerminal: false,
      };
    case "Delivering":
      return {
        label: "Đang giao hàng",
        badgeClass: "bg-cyan-100 text-cyan-900 border border-cyan-300",
        isTerminal: false,
      };
    case "Delivered":
    case "Completed":
      return {
        label: "Giao hàng thành công",
        badgeClass: "bg-emerald-100 text-emerald-800 border border-emerald-200",
        isTerminal: true,
      };
    case "DeliveryFailed":
      return {
        label: "Giao hàng thất bại",
        badgeClass: "bg-rose-100 text-rose-800 border border-rose-200",
        isTerminal: false,
      };
    case "Rejected":
      return {
        label: "Từ chối nhận hàng",
        badgeClass: "bg-red-100 text-red-800 border border-red-200",
        isTerminal: true,
      };
    case "Cancelled":
      return {
        label: "Đã hủy đơn",
        badgeClass: "bg-red-100 text-red-800 border border-red-200",
        isTerminal: true,
      };
    default:
      return {
        label: status || "Không xác định",
        badgeClass: "bg-gray-100 text-gray-800 border border-gray-200",
        isTerminal: false,
      };
  }
}

export function getPaymentStatusMeta(paymentStatus?: string, paymentMethod?: string): {
  label: string;
  badgeClass: string;
  isPaid: boolean;
  isExpired: boolean;
} {
  const isCod = isCodPayment(paymentMethod);
  const paid = isPaymentPaid(paymentStatus);

  if (paid) {
    return {
      label: isCod ? "Đã thanh toán COD" : "Đã thanh toán",
      badgeClass: "bg-emerald-100 text-emerald-800",
      isPaid: true,
      isExpired: false,
    };
  }

  if (isCod) {
    return {
      label: "Chưa thanh toán",
      badgeClass: "bg-amber-100 text-amber-800 border border-amber-200",
      isPaid: false,
      isExpired: false,
    };
  }

  switch (paymentStatus) {
    case "Expired":
      return {
        label: "Hết hạn thanh toán online",
        badgeClass: "bg-red-100 text-red-800",
        isPaid: false,
        isExpired: true,
      };
    case "Failed":
      return {
        label: "Thanh toán thất bại",
        badgeClass: "bg-red-100 text-red-800",
        isPaid: false,
        isExpired: false,
      };
    case "Cancelled":
      return {
        label: "Đã hủy thanh toán",
        badgeClass: "bg-red-100 text-red-800",
        isPaid: false,
        isExpired: false,
      };
    case "Refunded":
      return {
        label: "Đã hoàn tiền",
        badgeClass: "bg-blue-100 text-blue-800",
        isPaid: false,
        isExpired: false,
      };
    default:
      return {
        label: "Đang chờ thanh toán online",
        badgeClass: "bg-amber-100 text-amber-800",
        isPaid: false,
        isExpired: false,
      };
  }
}
