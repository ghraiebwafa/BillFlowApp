export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

type ProblemBody = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export async function parseApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as ProblemBody;
    if (body.detail) return new ApiError(body.detail, response.status);
    if (body.title) return new ApiError(body.title, response.status);
    const firstFieldError = body.errors
      ? Object.values(body.errors).flat()[0]
      : undefined;
    if (firstFieldError) return new ApiError(firstFieldError, response.status);
  } catch {
    // ignore JSON parse errors
  }

  return new ApiError(response.statusText || "Request failed", response.status);
}
