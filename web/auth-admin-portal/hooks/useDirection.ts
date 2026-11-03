"use client";

import { useSyncExternalStore } from "react";

const emptySubscribe = () => () => {};

export function useDirection(): "ltr" | "rtl" {
  return useSyncExternalStore(
    emptySubscribe,
    () => (typeof document !== "undefined" && (document.documentElement.getAttribute("dir") as "ltr" | "rtl")) || "ltr",
    () => "ltr"
  );
}
