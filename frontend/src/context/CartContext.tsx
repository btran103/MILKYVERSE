"use client";

import React, { createContext, useContext, useEffect, useState } from "react";
import { CartItem } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { AuthRequiredModal } from "@/components/auth/AuthRequiredModal";

interface CartContextType {
  items: CartItem[];
  addItem: (item: Omit<CartItem, "id">) => boolean;
  removeItem: (id: string) => void;
  updateQuantity: (id: string, delta: number) => void;
  clearCart: () => void;
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

  useEffect(() => {
    const saved = localStorage.getItem("milky_cart");
    if (saved) {
      try {
        setItems(JSON.parse(saved));
      } catch {
        setItems([]);
      }
    }
    setIsLoaded(true);
  }, []);

  useEffect(() => {
    if (isLoaded) {
      localStorage.setItem("milky_cart", JSON.stringify(items));
    }
  }, [items, isLoaded]);

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

  const addItem = (item: Omit<CartItem, "id">): boolean => {
    if (!user) {
      openAuthModal("Bạn cần đăng nhập hoặc đăng ký tài khoản để thêm món ăn hoặc trà sữa vào giỏ hàng!");
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
    localStorage.removeItem("milky_cart");
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
