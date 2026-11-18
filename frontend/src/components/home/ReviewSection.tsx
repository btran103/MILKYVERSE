import React from "react";
import Link from "next/link";
import { Star, CheckCircle2, Send, Lock, LogIn } from "lucide-react";
import { CommentItem } from "./types";

interface ReviewSectionProps {
  comments: CommentItem[];
  user: any;
  newCommentText: string;
  setNewCommentText: (text: string) => void;
  newCommentRating: number;
  setNewCommentRating: (rating: number) => void;
  onAddReview: (e: React.FormEvent) => void;
}

export default function ReviewSection({
  comments,
  user,
  newCommentText,
  setNewCommentText,
  newCommentRating,
  setNewCommentRating,
  onAddReview,
}: ReviewSectionProps) {
  const avgRating = comments.length > 0 
    ? (comments.reduce((sum, c) => sum + (c.rating || 5), 0) / comments.length).toFixed(1) 
    : "5.0";
  const totalCount = comments.length;

  return (
    <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
      <div className="bg-white rounded-3xl p-6 sm:p-10 border border-[#EFE6DC] shadow-sm space-y-8">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6 border-b border-[#EFE6DC]">
          <div>
            <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
              <Star className="w-4 h-4 text-amber-500 fill-amber-500" />
              <span>Cộng Đồng Thưởng Thức MilkyVerse</span>
            </div>
            <h2 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
              Đánh Giá Số Sao & Bình Luận Toàn Bộ Món
            </h2>
            <p className="text-xs sm:text-sm text-[#786C65] mt-1">
              Nhận xét chân thực từ cơ sở dữ liệu khách hàng MilkyVerse
            </p>
          </div>

          {/* Overall Rating Summary Box */}
          <div className="flex items-center gap-4 bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] shrink-0">
            <div className="text-center">
              <span className="text-3xl font-black text-[#2D2522] block">{avgRating}</span>
              <div className="flex items-center justify-center gap-0.5 text-amber-500">
                {[1, 2, 3, 4, 5].map((s) => (
                  <Star key={s} className="w-3.5 h-3.5 fill-amber-500" />
                ))}
              </div>
            </div>
            <div className="text-xs text-[#786C65] border-l border-[#EFE6DC] pl-4">
              <p className="font-bold text-[#2D2522]">{totalCount} Đánh Giá Thực Tế</p>
              <p>100% Khách Đã Mua Hàng</p>
            </div>
          </div>
        </div>

        {/* Comments Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {comments.slice(0, 4).map((c) => (
            <div
              key={c.id}
              className="p-5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3 flex flex-col justify-between"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full overflow-hidden bg-white border border-[#EFE6DC] shrink-0">
                    {c.avatarUrl ? (
                      <img src={c.avatarUrl} alt={c.userName} className="w-full h-full object-cover" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center bg-[#6F4E37] text-white font-bold text-xs">
                        {c.userName.charAt(0)}
                      </div>
                    )}
                  </div>
                  <div>
                    <h4 className="font-extrabold text-xs text-[#2D2522]">{c.userName}</h4>
                    <div className="flex items-center gap-1 text-amber-500 mt-0.5">
                      {Array.from({ length: c.rating }).map((_, i) => (
                        <Star key={i} className="w-3 h-3 fill-amber-500" />
                      ))}
                    </div>
                  </div>
                </div>
                <span className="text-[10px] text-[#786C65] font-semibold">{c.date}</span>
              </div>

              <p className="text-xs text-[#786C65] leading-relaxed italic">
                &ldquo;{c.content}&rdquo;
              </p>
            </div>
          ))}
        </div>

        {/* Form Viết Đánh Giá & Chấm Sao */}
        {user ? (
          <form
            onSubmit={onAddReview}
            className="p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-[#FAF7F2] to-white border border-[#EFE6DC] space-y-4"
          >
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 rounded-full overflow-hidden bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs shrink-0">
                  {user.avatarUrl ? (
                    <img src={user.avatarUrl} alt={user.fullName} className="w-full h-full object-cover" />
                  ) : (
                    user.fullName?.charAt(0) || "U"
                  )}
                </div>
                <div>
                  <h4 className="font-extrabold text-xs text-[#2D2522]">{user.fullName}</h4>
                  <span className="text-[10px] text-emerald-700 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3 h-3" /> Đã xác thực tài khoản • Khách hàng MilkyVerse
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-[#786C65]">Chấm điểm sao:</span>
                <div className="flex items-center gap-1">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setNewCommentRating(s)}
                      className="p-1 hover:scale-125 transition-transform"
                    >
                      <Star
                        className={`w-4 h-4 ${
                          s <= newCommentRating
                            ? "text-amber-500 fill-amber-500"
                            : "text-gray-300"
                        }`}
                      />
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="text"
                required
                placeholder="Cảm nhận của bạn về hương vị trà sữa, độ ngọt béo, đồ ăn vặt..."
                value={newCommentText}
                onChange={(e) => setNewCommentText(e.target.value)}
                className="flex-1 px-4 py-2.5 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <button
                type="submit"
                className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all flex items-center gap-1.5 shrink-0"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Gửi</span>
              </button>
            </div>
          </form>
        ) : (
          <div className="p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-[#FAF7F2] to-white border border-[#EFE6DC] flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex items-start gap-3">
              <div className="w-10 h-10 rounded-2xl bg-amber-100 text-amber-800 flex items-center justify-center shrink-0">
                <Lock className="w-5 h-5" />
              </div>
              <div>
                <h4 className="font-extrabold text-sm text-[#2D2522]">Xác Thực Phân Quyền Bình Luận</h4>
                <p className="text-xs text-[#786C65]">Vui lòng đăng nhập tài khoản MilkyVerse để viết đánh giá và chấm điểm sao.</p>
              </div>
            </div>
            <Link
              href="/auth/login"
              className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-sm flex items-center gap-1.5 shrink-0"
            >
              <LogIn className="w-4 h-4" />
              <span>Đăng Nhập Để Bình Luận</span>
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
