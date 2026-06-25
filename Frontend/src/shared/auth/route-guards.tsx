import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSessionStore } from "../auth/session-store";
import { homePathForRole, isAdminRole, isVisitorRole } from "../auth/role-utils";
import { UserRole } from "../../domain/auth/types";
import { AppShell } from "../layout/AppShell";

export function GuestOnly() {
  const { isAuthenticated, user } = useSessionStore();
  if (isAuthenticated && user) {
    return <Navigate to={homePathForRole(user.role)} replace />;
  }

  return <Outlet />;
}

export function RequireAuth() {
  const { isAuthenticated } = useSessionStore();
  if (!isAuthenticated) return <Navigate to="/welcome" replace />;

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
  if (!isVisitorRole(user.role)) {
    return <Navigate to="/admin/users" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

export function RequireAdmin() {
  const { user } = useSessionStore();
  const location = useLocation();

  if (!user) return <Navigate to="/welcome" replace />;
  if (!isAdminRole(user.role)) {
    return <Navigate to="/dashboard" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

export function HomeRedirect() {
  const { isAuthenticated, user } = useSessionStore();
  if (!isAuthenticated || !user) return <Navigate to="/welcome" replace />;
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
