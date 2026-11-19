"use client";

export interface SavedAddress {
  id: string;
  label: "Nhà riêng" | "Văn phòng" | "Trường học" | "Khác";
  recipientName: string;
  phoneNumber: string;
  addressDetail: string;
  isDefault: boolean;
}

const STORAGE_PREFIX = "milky_addresses_";

export function getSavedAddresses(userId: number, initialAddress?: string, initialName?: string, initialPhone?: string): SavedAddress[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(`${STORAGE_PREFIX}${userId}`);
    if (raw) {
      const parsed: SavedAddress[] = JSON.parse(raw);
      if (parsed.length > 0) return parsed;
    }

    // If empty and initial address exists, seed default addresses
    const defaultAddresses: SavedAddress[] = [];
    if (initialAddress) {
      defaultAddresses.push({
        id: "addr_default_1",
        label: "Nhà riêng",
        recipientName: initialName || "Khách hàng MilkyVerse",
        phoneNumber: initialPhone || "0908888777",
        addressDetail: initialAddress,
        isDefault: true,
      });
    }

    if (defaultAddresses.length > 0) {
      localStorage.setItem(`${STORAGE_PREFIX}${userId}`, JSON.stringify(defaultAddresses));
    }

    return defaultAddresses;
  } catch {
    return [];
  }
}

export function saveAddress(userId: number, address: Omit<SavedAddress, "id">): SavedAddress[] {
  if (typeof window === "undefined") return [];
  const existing = getSavedAddresses(userId);

  const newAddr: SavedAddress = {
    ...address,
    id: "addr_" + Date.now(),
  };

  let updated: SavedAddress[];
  if (newAddr.isDefault) {
    updated = [newAddr, ...existing.map((a) => ({ ...a, isDefault: false }))];
  } else {
    // If it's the very first address, make it default automatically
    if (existing.length === 0) {
      newAddr.isDefault = true;
    }
    updated = [...existing, newAddr];
  }

  localStorage.setItem(`${STORAGE_PREFIX}${userId}`, JSON.stringify(updated));
  window.dispatchEvent(new CustomEvent("milky_addresses_updated", { detail: { userId, addresses: updated } }));
  return updated;
}

export function deleteAddress(userId: number, addressId: string): SavedAddress[] {
  if (typeof window === "undefined") return [];
  const existing = getSavedAddresses(userId);
  const updated = existing.filter((a) => a.id !== addressId);

  // If deleted address was default and other addresses exist, promote first one to default
  if (updated.length > 0 && !updated.some((a) => a.isDefault)) {
    updated[0].isDefault = true;
  }

  localStorage.setItem(`${STORAGE_PREFIX}${userId}`, JSON.stringify(updated));
  window.dispatchEvent(new CustomEvent("milky_addresses_updated", { detail: { userId, addresses: updated } }));
  return updated;
}

export function setDefaultAddress(userId: number, addressId: string): SavedAddress[] {
  if (typeof window === "undefined") return [];
  const existing = getSavedAddresses(userId);
  const updated = existing.map((a) => ({
    ...a,
    isDefault: a.id === addressId,
  }));

  localStorage.setItem(`${STORAGE_PREFIX}${userId}`, JSON.stringify(updated));
  window.dispatchEvent(new CustomEvent("milky_addresses_updated", { detail: { userId, addresses: updated } }));
  return updated;
}
