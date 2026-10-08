"use client";

import { useMemo, useSyncExternalStore } from "react";
import type { ApplicationResult } from "./api";

const KEY = "loan:lastResult";

export function saveLastResult(result: ApplicationResult) {
  sessionStorage.setItem(KEY, JSON.stringify(result));
}

const subscribe = () => () => {};
const getSnapshot = () => sessionStorage.getItem(KEY);
const getServerSnapshot = () => null;

export function useLastResult(): ApplicationResult | null {
  const raw = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
  return useMemo(() => (raw ? (JSON.parse(raw) as ApplicationResult) : null), [raw]);
}
