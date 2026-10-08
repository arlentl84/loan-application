"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ApiError, submitApplication } from "@/lib/api";
import { saveLastResult } from "@/lib/lastResult";
import { US_STATES } from "@/lib/states";
import { formatSsn, parseAmount, validate, type FormErrors, type FormValues } from "@/lib/validation";
import { Field, inputClass } from "./Field";

const EMPTY: FormValues = {
  firstName: "",
  lastName: "",
  street: "",
  city: "",
  state: "",
  zipCode: "",
  companyName: "",
  requestedAmount: "",
  ssn: "",
};

export function ApplicationForm() {
  const router = useRouter();
  const [values, setValues] = useState<FormValues>(EMPTY);
  const [errors, setErrors] = useState<FormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [showSsn, setShowSsn] = useState(false);

  function set<K extends keyof FormValues>(field: K, value: FormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }));    
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  function checkField(field: keyof FormValues) {
    const message = validate(values)[field];
    setErrors((current) => ({ ...current, [field]: message }));
  }

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const found = validate(values);
    setErrors(found);
    const firstInvalid = Object.keys(found)[0];
    if (firstInvalid) {
      document.getElementById(firstInvalid)?.focus();
      return;
    }

    setSubmitting(true);
    try {
      const result = await submitApplication({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        street: values.street.trim(),
        city: values.city.trim(),
        state: values.state,
        zipCode: values.zipCode.trim(),
        companyName: values.companyName.trim(),
        requestedAmount: parseAmount(values.requestedAmount),
        ssn: values.ssn,
      });
      saveLastResult(result);
      router.push(result.status === "Approved" ? "/approved" : "/denied");
    } catch (error) {
      if (error instanceof ApiError) {
        setFormError(error.message);
        setErrors(error.fieldErrors as FormErrors);
      } else {
        setFormError("Something went wrong. Please try again.");
      }
      //setSubmitting(false);
    }
    finally{
      setSubmitting(false);
    }    
  }

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-8 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div>
        <h1 className="text-2xl font-semibold">Apply for a loan</h1>
        <p className="mt-1 text-sm text-slate-600">It takes about two minutes. You get an answer right away.</p>
      </div>

      {formError && (
        <div role="alert" className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {formError}
        </div>
      )}

      <fieldset className="space-y-4">
        <legend className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">About you</legend>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field id="firstName" label="First name" error={errors.firstName}>
            {(a) => (
              <input {...a} className={inputClass} autoComplete="given-name" value={values.firstName}
                onChange={(e) => set("firstName", e.target.value)} onBlur={() => checkField("firstName")} />
            )}
          </Field>
          <Field id="lastName" label="Last name" error={errors.lastName}>
            {(a) => (
              <input {...a} className={inputClass} autoComplete="family-name" value={values.lastName}
                onChange={(e) => set("lastName", e.target.value)} onBlur={() => checkField("lastName")} />
            )}
          </Field>
        </div>
        <Field id="ssn" label="Social Security Number" error={errors.ssn} hint="Used to identify you. Nine digits.">
          {(a) => (
            <div className="flex gap-2">
              <input {...a} className={inputClass} type={showSsn ? "text" : "password"} inputMode="numeric"
                autoComplete="off" placeholder="123-45-6789" value={values.ssn}
                onChange={(e) => set("ssn", formatSsn(e.target.value))} onBlur={() => checkField("ssn")} />
              <button type="button" onClick={() => setShowSsn((s) => !s)} aria-pressed={showSsn}
                className="shrink-0 rounded-md border border-slate-300 px-3 text-sm text-slate-700 hover:bg-slate-50">
                {showSsn ? "Hide" : "Show"}
              </button>
            </div>
          )}
        </Field>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">Your business</legend>
        <Field id="companyName" label="Company name" error={errors.companyName}>
          {(a) => (
            <input {...a} className={inputClass} autoComplete="organization" value={values.companyName}
              onChange={(e) => set("companyName", e.target.value)} onBlur={() => checkField("companyName")} />
          )}
        </Field>
        <Field id="street" label="Street address" error={errors.street}>
          {(a) => (
            <input {...a} className={inputClass} autoComplete="street-address" value={values.street}
              onChange={(e) => set("street", e.target.value)} onBlur={() => checkField("street")} />
          )}
        </Field>
        <div className="grid gap-4 sm:grid-cols-6">
          <div className="sm:col-span-3">
            <Field id="city" label="City" error={errors.city}>
              {(a) => (
                <input {...a} className={inputClass} autoComplete="address-level2" value={values.city}
                  onChange={(e) => set("city", e.target.value)} onBlur={() => checkField("city")} />
              )}
            </Field>
          </div>
          <div className="sm:col-span-2">
            <Field id="state" label="State" error={errors.state}>
              {(a) => (
                <select {...a} className={inputClass} autoComplete="address-level1" value={values.state}
                  onChange={(e) => set("state", e.target.value)} onBlur={() => checkField("state")}>
                  <option value="">Select…</option>
                  {US_STATES.map((s) => (
                    <option key={s.code} value={s.code}>
                      {s.code} · {s.name}
                    </option>
                  ))}
                </select>
              )}
            </Field>
          </div>
          <div className="sm:col-span-1">
            <Field id="zipCode" label="ZIP" error={errors.zipCode}>
              {(a) => (
                <input {...a} className={inputClass} inputMode="numeric" autoComplete="postal-code" maxLength={10}
                  value={values.zipCode} onChange={(e) => set("zipCode", e.target.value)}
                  onBlur={() => checkField("zipCode")} />
              )}
            </Field>
          </div>
        </div>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">The loan</legend>
        <Field id="requestedAmount" label="Requested amount" error={errors.requestedAmount}>
          {(a) => (
            <div className="relative">
              <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sm text-slate-500">$</span>
              <input {...a} className={`${inputClass} pl-7`} inputMode="decimal" placeholder="25,000"
                value={values.requestedAmount} onChange={(e) => set("requestedAmount", e.target.value)}
                onBlur={() => checkField("requestedAmount")} />
            </div>
          )}
        </Field>
      </fieldset>

      <button type="submit" disabled={submitting}
        className="w-full rounded-md bg-blue-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-300 disabled:cursor-not-allowed disabled:bg-blue-400">
        {submitting ? "Checking your application…" : "Submit application"}
      </button>
    </form>
  );
}
