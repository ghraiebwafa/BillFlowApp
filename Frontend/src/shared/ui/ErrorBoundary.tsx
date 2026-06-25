import type { ComponentType, ErrorInfo, ReactNode } from "react";
import { Component } from "react";

type ErrorBoundaryProps = {
  children: ReactNode;
  fallback?: ReactNode;
};

type ErrorBoundaryState = {
  hasError: boolean;
};

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("Unhandled UI error:", error, info.componentStack);
  }

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div className="flex min-h-screen items-center justify-center p-6 text-center">
            <div className="card max-w-md space-y-2">
              <h1 className="text-lg font-semibold">Something went wrong</h1>
              <p className="text-sm text-secondary">Refresh the page or try again later.</p>
            </div>
          </div>
        )
      );
    }

    return this.props.children;
  }
}

export function withErrorBoundary<P extends object>(
  Wrapped: ComponentType<P>,
  fallback?: ReactNode,
): ComponentType<P> {
  return function WithErrorBoundary(props: P) {
    return (
      <ErrorBoundary fallback={fallback}>
        <Wrapped {...props} />
      </ErrorBoundary>
    );
  };
}
