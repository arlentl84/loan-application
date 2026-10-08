import type { ReactNode } from "react";

type FieldProps = {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  children: (props: { id: string; "aria-invalid": boolean; "aria-describedby"?: string }) => ReactNode;
};

export const inputClass =
  "block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm shadow-sm " +
  "focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-200 " +
  "aria-[invalid=true]:border-red-500 aria-[invalid=true]:focus:ring-red-200";

export function Field({ id, label, error, hint, children }: FieldProps) {
  const messageId = `${id}-message`;
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-700">
        {label}
      </label>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": error || hint ? messageId : undefined })}
      {error ? (
        <p id={messageId} className="mt-1 text-sm text-red-600">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="mt-1 text-xs text-slate-500">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
