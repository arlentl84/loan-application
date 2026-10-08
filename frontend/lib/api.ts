const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export type ApplicationRequest = {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  requestedAmount: number;
  ssn: string;
};

export type ApplicationResult = {
  status: "Approved" | "Denied";
  isReturningCustomer: boolean;
  customerId: string | null;
  applicationId: string | null;
  requestedAmount: number | null;
  reasons: { code: string; message: string }[];
};

/** Something the user can act on: a message for the banner and, when known, messages per field. */
export class ApiError extends Error {
  constructor(
    message: string,
    public fieldErrors: Record<string, string> = {},
  ) {
    super(message);
  }
}

export async function submitApplication(request: ApplicationRequest): Promise<ApplicationResult> {
  let response: Response;
  try {
    response = await fetch(`${API_URL}/api/applications`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
  } catch {
    throw new ApiError("We couldn't reach the server. Check your connection and try again.");
  }

  if (response.status === 400) {
    throw await toValidationError(response);
  }
  if (!response.ok) {
    throw new ApiError("Something went wrong on our side. Please try again in a moment.");
  }
  return (await response.json()) as ApplicationResult;
}

async function toValidationError(response: Response): Promise<ApiError> {
  const fallback = "Some fields are not valid. Please review the form.";
  try {
    const problem = await response.json();
    const fieldErrors: Record<string, string> = {};

    // Model validation: { errors: { State: ["..."] } } (property names arrive in PascalCase).
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
      const field = key.charAt(0).toLowerCase() + key.slice(1);
      fieldErrors[field] = (messages as string[])[0];
    }
    // Domain validation: { field: "ssn", detail: "..." }.
    if (problem.field && problem.detail) {
      fieldErrors[problem.field] = problem.detail;
    }
    return new ApiError(fallback, fieldErrors);
  } catch {
    return new ApiError(fallback);
  }
}
