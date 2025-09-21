import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import BlockedUserPage from "../pages/BlockedUserPage/BlockedUserPage";

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string; // Optional role requirement
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRole,
}) => {
  const { user } = useAuth();

  console.log("ProtectedRoute - User:", user);

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  // Handle different possible formats of isBlocked
  const isBlocked = user.isBlocked === true;

  console.log("ProtectedRoute - User is blocked:", isBlocked);

  if (isBlocked) {
    return <BlockedUserPage />;
  }

  // Check role requirement if specified
  if (requiredRole && user.role !== requiredRole) {
    console.log(
      `ProtectedRoute - User role ${user.role} doesn't match required role ${requiredRole}`
    );
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
