"use client";

// Simple reactive favorites manager using localStorage and CustomEvent
const STORAGE_KEY = "milky_favorites";
const EVENT_NAME = "milky_favorites_updated";

export function getFavoriteIds(): number[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

export function isFavorite(id: number): boolean {
  const ids = getFavoriteIds();
  return ids.includes(id);
}

export function toggleFavorite(id: number): boolean {
  if (typeof window === "undefined") return false;
  const ids = getFavoriteIds();
  const exists = ids.includes(id);
  const nextIds = exists ? ids.filter((item) => item !== id) : [...ids, id];

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(nextIds));
    window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: nextIds }));
  } catch (e) {
    console.error("Lỗi khi lưu yêu thích:", e);
  }

  return !exists;
}

export function subscribeFavorites(callback: (ids: number[]) => void): () => void {
  if (typeof window === "undefined") return () => {};

  const handler = (e: Event) => {
    const customEvent = e as CustomEvent<number[]>;
    callback(customEvent.detail || getFavoriteIds());
  };

  window.addEventListener(EVENT_NAME, handler);
  window.addEventListener("storage", handler);

  return () => {
    window.removeEventListener(EVENT_NAME, handler);
    window.removeEventListener("storage", handler);
  };
}
