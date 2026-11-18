export interface CommentItem {
  id: number;
  userName: string;
  avatarUrl?: string;
  rating: number;
  date: string;
  content: string;
  productId?: number;
}

export const INITIAL_COMMENTS: CommentItem[] = [
  {
    id: 1,
    userName: "Trần Bảo Ngọc",
    avatarUrl: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "Hôm nay",
    content: "Trà sữa nướng trân châu hoàng kim đỉnh chóp! Trân châu mềm dẻo thấm vị đường đen, vị trà đậm đà không bị ngọt gắt.",
  },
  {
    id: 2,
    userName: "Lê Minh Quân",
    avatarUrl: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "Hôm qua",
    content: "Mì cay 7 cấp độ ăn kèm trà dâu macchiato là combo chân ái luôn. Cửa hàng giao nhanh đúng 20 phút là có hàng nóng hổi.",
  },
  {
    id: 3,
    userName: "Hoàng Yến Nhi",
    avatarUrl: "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "2 ngày trước",
    content: "Món signature hôm nay ngon xuất sắc, lớp kem cheese béo mặn sánh mịn, uống kèm trân châu 3Q giòn sần sật cực mê!",
  },
  {
    id: 4,
    userName: "Đặng Tuấn Anh",
    avatarUrl: "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80",
    rating: 4,
    date: "3 ngày trước",
    content: "Cá viên chiên mẹt thập cẩm sốt Thái đậm đà, nước sốt chấm thần thánh ăn cuốn miệng dã man. 5 sao cho chất lượng phục vụ!",
  },
];
