"use client";

import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import { User, UserRole } from "@/types";
import { api } from "@/lib/api";

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  login: (email: string, pass: string) => Promise<{ success: boolean; message?: string }>;
  register: (payload: { fullName: string; email: string; password: string; verificationCode?: string; phoneNumber?: string; address?: string }) => Promise<{ success: boolean; message?: string }>;
  logout: () => void;
  isAdmin: boolean;
  isManager: boolean;
  isStaff: boolean;
  isWarehouse: boolean;
  isDelivery: boolean;
}

const AuthContext = createContext<AuthContextType>({
  user: null,
  token: null,
  isLoading: true,
  login: async () => ({ success: false }),
  register: async () => ({ success: false }),
  logout: () => {},
  isAdmin: false,
  isManager: false,
  isStaff: false,
  isWarehouse: false,
  isDelivery: false,
});

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    localStorage.removeItem("milky_token");
    localStorage.removeItem("milky_user");
  }, []);

  useEffect(() => {
    const savedToken = localStorage.getItem("milky_token");
    const savedUser = localStorage.getItem("milky_user");

    if (savedToken && savedUser) {
      try {
        setToken(savedToken);
        setUser(JSON.parse(savedUser));
        api.getProfile()
          .then((profile) => {
            setUser(profile);
            localStorage.setItem("milky_user", JSON.stringify(profile));
          })
          .catch(() => {
            logout();
          })
          .finally(() => setIsLoading(false));
      } catch {
        logout();
        setIsLoading(false);
      }
    } else {
      setIsLoading(false);
    }
  }, [logout]);

  const login = async (email: string, pass: string) => {
    try {
      const res = await api.login({ email, password: pass });
      setToken(res.token);
      const userObj: User = {
        id: res.userId,
        fullName: res.fullName,
        email: res.email,
        role: res.role as UserRole,
        phoneNumber: res.phoneNumber,
        address: res.address,
        createdAt: new Date().toISOString(),
      };
      setUser(userObj);
      localStorage.setItem("milky_token", res.token);
      localStorage.setItem("milky_user", JSON.stringify(userObj));
      return { success: true };
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đăng nhập thất bại.";
      return { success: false, message: msg };
    }
  };

  const register = async (payload: { fullName: string; email: string; password: string; verificationCode?: string; phoneNumber?: string; address?: string }) => {
    try {
      const res = await api.register(payload);
      setToken(res.token);
      const userObj: User = {
        id: res.userId,
        fullName: res.fullName,
        email: res.email,
        role: res.role as UserRole,
        phoneNumber: res.phoneNumber,
        address: res.address,
        createdAt: new Date().toISOString(),
      };
      setUser(userObj);
      localStorage.setItem("milky_token", res.token);
      localStorage.setItem("milky_user", JSON.stringify(userObj));
      return { success: true };
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đăng ký thất bại.";
      return { success: false, message: msg };
    }
  };

  const isAdmin = user?.role === "Admin";
  const isManager = user?.role === "Manager" || user?.role === "Admin";
  const isStaff = user?.role === "Staff" || user?.role === "Manager" || user?.role === "Admin";
  const isWarehouse = user?.role === "WarehouseStaff" || user?.role === "Manager" || user?.role === "Admin";
  const isDelivery = user?.role === "DeliveryStaff" || user?.role === "Manager" || user?.role === "Admin";

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        login,
        register,
        logout,
        isAdmin,
        isManager,
        isStaff,
        isWarehouse,
        isDelivery,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
