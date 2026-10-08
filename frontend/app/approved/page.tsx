"use client";

import Link from "next/link";
import { useLastResult } from "@/lib/lastResult";

export default function ApprovedPage() {
  const result = useLastResult();

  if (!result || result.status !== "Approved") {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-6 text-center shadow-sm">
        <p className="text-slate-600">There is no application to show.</p>
        <Link href="/" className="mt-4 inline-block font-medium text-blue-700 hover:underline">
          Start an application
        </Link>
      </div>
    );
  }

  const amount = (result.requestedAmount ?? 0).toLocaleString("en-US", { style: "currency", currency: "USD" });

  return (
    <div className="rounded-xl border border-green-200 bg-white p-8 text-center shadow-sm">
      <div aria-hidden className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-green-100 text-2xl text-green-700">
        ✓
      </div>
      <h1 className="text-2xl font-semibold">Your application is approved</h1>
      <p className="mt-2 text-slate-600">
        {result.isReturningCustomer
          ? `We found your existing application and updated it. The requested amount is now ${amount}.`
          : `We received your request for ${amount}.`}
      </p>
      {result.applicationId && (
        <p className="mt-4 text-xs text-slate-500">
          Reference: <span className="font-mono">{result.applicationId}</span>
        </p>
      )}
      <Link href="/" className="mt-6 inline-block rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-50">
        Submit another application
      </Link>
    </div>
  );
}
