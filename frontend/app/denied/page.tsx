"use client";

import Link from "next/link";
import { useLastResult } from "@/lib/lastResult";

export default function DeniedPage() {
  const result = useLastResult();
  const reasons = result?.status === "Denied" ? result.reasons : [];

  return (
    <div className="rounded-xl border border-red-200 bg-white p-8 text-center shadow-sm">
      <div aria-hidden className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 text-2xl text-red-700">
        ✕
      </div>
      <h1 className="text-2xl font-semibold">We couldn&apos;t approve your application</h1>
      {reasons.length > 0 ? (
        <ul className="mt-3 space-y-1 text-slate-600">
          {reasons.map((reason) => (
            <li key={reason.code}>{reason.message}</li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-slate-600">We are unable to approve this application right now.</p>
      )}
      <Link href="/" className="mt-6 inline-block rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-50">
        Back to the form
      </Link>
    </div>
  );
}
