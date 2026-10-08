import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Loan application",
  description: "Apply for a business loan",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className="min-h-screen bg-slate-50 text-slate-900 antialiased">
        <header className="border-b border-slate-200 bg-white">
          <div className="mx-auto max-w-2xl px-4 py-4 text-lg font-semibold text-blue-700">
            Loan application
          </div>
        </header>
        <main className="mx-auto max-w-2xl px-4 py-8">{children}</main>
      </body>
    </html>
  );
}
