import { US_STATES } from "./states";

export type FormValues = {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  requestedAmount: string;
  ssn: string;
};

export type FormErrors = Partial<Record<keyof FormValues, string>>;

export const MAX_AMOUNT = 10_000_000;

/** Keeps digits only and formats them as ###-##-####. */
export function formatSsn(input: string): string {
  const digits = input.replace(/\D/g, "").slice(0, 9);
  if (digits.length <= 3) return digits;
  if (digits.length <= 5) return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}`;
}

export function parseAmount(input: string): number {
  return Number(input.replace(/[,$\s]/g, ""));
}

// Mirrors the backend rules so the user gets instant feedback. The API validates again.
export function validate(values: FormValues): FormErrors {
  const errors: FormErrors = {};

  const required: [keyof FormValues, string][] = [
    ["firstName", "First name is required."],
    ["lastName", "Last name is required."],
    ["companyName", "Company name is required."],
    ["street", "Street address is required."],
    ["city", "City is required."],
  ];
  for (const [field, message] of required) {
    if (!values[field].trim()) errors[field] = message;
  }

  if (!US_STATES.some((s) => s.code === values.state)) {
    errors.state = "Select a state.";
  }
  if (!/^\d{5}(-\d{4})?$/.test(values.zipCode.trim())) {
    errors.zipCode = "Enter a 5-digit ZIP code.";
  }
  if (!/^\d{3}-\d{2}-\d{4}$/.test(values.ssn)) {
    errors.ssn = "Enter all 9 digits of the SSN.";
  }

  const amount = parseAmount(values.requestedAmount);
  if (!values.requestedAmount.trim() || Number.isNaN(amount) || amount <= 0) {
    errors.requestedAmount = "Enter an amount greater than zero.";
  } else if (amount > MAX_AMOUNT) {
    errors.requestedAmount = `The maximum amount is $${MAX_AMOUNT.toLocaleString("en-US")}.`;
  }

  return errors;
}
