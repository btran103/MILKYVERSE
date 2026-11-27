"use client";

import React, { createContext, useContext, useEffect, useState } from "react";
import { CartItem } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { AuthRequiredModal } from "@/components/auth/AuthRequiredModal";

interface CartContextType {
  items: CartItem[];
  addItem: (item: Omit<CartItem, "id">, allowGuestForAgent?: boolean) => boolean;
  removeItem: (id: string) => void;
  updateQuantity: (id: string, delta: number) => void;
  clearCart: () => void;
  syncCartFromAgent: (agentCart: CartItem[]) => void;
  cartCount: number;
  subtotal: number;
  openAuthModal: (msg?: string) => void;
}

const CartContext = createContext<CartContextType>({
  items: [],
  addItem: () => false,
  removeItem: () => {},
  updateQuantity: () => {},
  clearCart: () => {},
  syncCartFromAgent: () => {},
  cartCount: 0,
  subtotal: 0,
  openAuthModal: () => {},
});

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user } = useAuth();
  const [items, setItems] = useState<CartItem[]>([]);
  const [isLoaded, setIsLoaded] = useState(false);
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [authModalMsg, setAuthModalMsg] = useState<string | undefined>(undefined);

  // User-scoped cart key so each user has isolated cart, and guest sessions don't leak into logged-in accounts
  const cartStorageKey = user ? `milky_cart_user_${user.id}` : "milky_cart_guest";

  useEffect(() => {
    // Clean up legacy shared key if present
    localStorage.removeItem("milky_cart");

    try {
      const saved = localStorage.getItem(cartStorageKey);
      if (saved) {
        setItems(JSON.parse(saved));
      } else {
        setItems([]);
      }
    } catch {
      setItems([]);
    }
    setIsLoaded(true);
  }, [cartStorageKey]);

  useEffect(() => {
    if (isLoaded) {
      if (items.length > 0) {
        localStorage.setItem(cartStorageKey, JSON.stringify(items));
      } else {
        localStorage.removeItem(cartStorageKey);
      }
    }
  }, [items, isLoaded, cartStorageKey]);

  // Tài khoản Shipper, Thủ kho, Quản lý/Admin không được giữ giỏ hàng đặt món
  useEffect(() => {
    if (user && user.role !== "Customer" && user.role !== "Staff") {
      setItems([]);
      localStorage.removeItem(cartStorageKey);
      localStorage.removeItem("milky_cart");
    }
  }, [user, cartStorageKey]);

  const openAuthModal = (msg?: string) => {
    setAuthModalMsg(msg);
    setShowAuthModal(true);
  };

  const getSignature = (i: Omit<CartItem, "id"> | CartItem) => {
    return `${i.productId || "diy"}-${i.itemType || "DRINK"}-${i.size || ""}-${i.sugarLevel || ""}-${i.iceLevel || ""}-${i.spicyLevel || ""}-${i.dippingSauce || ""}-${i.portionSize || ""}-${(i.toppings || [])
      .map((t) => `${t.toppingId}:${t.quantity}`)
      .sort()
      .join(",")}-${i.customNotes || ""}`;
  };

  const addItem = (item: Omit<CartItem, "id">, allowGuestForAgent: boolean = false): boolean => {
    if (!user && !allowGuestForAgent) {
      openAuthModal("Bạn cần đăng nhập hoặc đăng ký tài khoản để thêm món ăn hoặc trà sữa vào giỏ hàng!");
      return false;
    }

    if (user && user.role !== "Customer" && user.role !== "Staff" && user.role !== "Admin" && user.role !== "Manager") {
      alert(`Tài khoản vai trò ${user.role} không có quyền đặt món. Chỉ tài khoản Khách Hàng và Nhân Viên mới có thể đặt món.`);
      return false;
    }

    const signature = getSignature(item);

    setItems((prev) => {
      const existingIndex = prev.findIndex((i) => getSignature(i) === signature);

      if (existingIndex > -1) {
        const next = [...prev];
        const existing = next[existingIndex];
        const newQty = existing.quantity + item.quantity;
        next[existingIndex] = {
          ...existing,
          quantity: newQty,
          totalPrice: existing.unitPrice * newQty,
        };
        return next;
      } else {
        const newItem: CartItem = {
          ...item,
          id: "item_" + Date.now() + "_" + Math.random().toString(36).substring(2, 7),
        };
        return [...prev, newItem];
      }
    });

    return true;
  };

  const removeItem = (id: string) => {
    setItems((prev) => prev.filter((i) => i.id !== id));
  };

  const updateQuantity = (id: string, delta: number) => {
    setItems((prev) => {
      return prev
        .map((i) => {
          if (i.id === id) {
            const nextQty = i.quantity + delta;
            if (nextQty <= 0) return null;
            return {
              ...i,
              quantity: nextQty,
              totalPrice: i.unitPrice * nextQty,
            };
          }
          return i;
        })
        .filter(Boolean) as CartItem[];
    });
  };

  const clearCart = () => {
    setItems([]);
    localStorage.removeItem(cartStorageKey);
    localStorage.removeItem("milky_cart");
    localStorage.removeItem("milky_cart_guest");
  };

  const syncCartFromAgent = (agentCart: CartItem[]) => {
    const normalized: CartItem[] = (agentCart || []).map((c, idx) => ({
      ...c,
      id: c.id || `item_agent_${Date.now()}_${idx}`,
      itemType: (c.itemType as "DRINK" | "FOOD" | "COMBO") || "DRINK",
      size: c.size || "M",
      unitPrice: Number(c.unitPrice) || 0,
      quantity: Number(c.quantity) || 1,
      totalPrice: Number(c.totalPrice) || (Number(c.unitPrice) || 0) * (Number(c.quantity) || 1),
      isCustomDIY: Boolean(c.isCustomDIY),
      toppings: (c.toppings || []).map((t) => ({
        toppingId: Number(t.toppingId) || 1,
        name: t.name || "Topping",
        price: Number(t.price) || 0,
        quantity: Number(t.quantity) || 1,
      })),
    }));
    setItems(normalized);
  };

  const cartCount = items.reduce((acc, i) => acc + i.quantity, 0);
  const subtotal = items.reduce((acc, i) => acc + i.totalPrice, 0);

  return (
    <CartContext.Provider
      value={{
        items,
        addItem,
        removeItem,
        updateQuantity,
        clearCart,
        syncCartFromAgent,
        cartCount,
        subtotal,
        openAuthModal,
      }}
    >
      {children}

      <AuthRequiredModal
        isOpen={showAuthModal}
        message={authModalMsg}
        onClose={() => setShowAuthModal(false)}
      />
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);
