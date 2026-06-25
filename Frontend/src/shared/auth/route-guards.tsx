import { useEffect } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSessionStore } from "./session-store";
import { homePathForRole, isAdminRole, isVisitorRole } from "./role-utils";
import { UserRole } from "../../domain/auth/types";
import { AppShell } from "../layout/AppShell";

function InactiveUserRedirect() {
  const clearSession = useSessionStore((s) => s.clearSession);

  useEffect(() => {
    clearSession();
  }, [clearSession]);

  return <Navigate to="/welcome" replace />;
}

export function GuestOnly() {
  const { isAuthenticated, user } = useSessionStore();
  if (isAuthenticated && user) {
    if (!user.isActive) return <InactiveUserRedirect />;
    return <Navigate to={homePathForRole(user.role)} replace />;
  }

  return <Outlet />;
}

export function RequireAuth() {
  const { isAuthenticated, user } = useSessionStore();
  if (!isAuthenticated) return <Navigate to="/welcome" replace />;
  if (user && !user.isActive) return <InactiveUserRedirect />;

  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}

export function RequireVisitor() {
  const { user } = useSessionStore();
  const location = useLocation();

  if (!user) return <Navigate to="/welcome" replace />;
  if (!user.isActive) return <InactiveUserRedirect />;
  if (!isVisitorRole(user.role)) {
    return <Navigate to="/admin/users" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

export function RequireAdmin() {
  const { user } = useSessionStore();
  const location = useLocation();

  if (!user) return <Navigate to="/welcome" replace />;
  if (!user.isActive) return <InactiveUserRedirect />;
  if (!isAdminRole(user.role)) {
    return <Navigate to="/dashboard" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

export function HomeRedirect() {
  const { isAuthenticated, user } = useSessionStore();
  if (!isAuthenticated || !user) return <Navigate to="/welcome" replace />;
  if (!user.isActive) return <InactiveUserRedirect />;
  return <Navigate to={homePathForRole(user.role)} replace />;
}

export function roleLabel(role: UserRole): string {
  switch (role) {
    case UserRole.SuperAdmin:
      return "SuperAdmin";
    case UserRole.Admin:
      return "Admin";
    default:
      return "Business Owner";
  }
}
