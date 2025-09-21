import React, { useState, useEffect } from "react";
import {
  BookOpen,
  Menu,
  X,
  User,
  LogIn,
  Settings,
  Heart,
  Library,
  Workflow,
} from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import "./Navbar.css";
import { useAuth } from "../../context/AuthContext";

const Navbar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const [isScrolled, setIsScrolled] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isProfileDropdownOpen, setIsProfileDropdownOpen] = useState(false);

  const { user, setUser } = useAuth();

  useEffect(() => {
    const handleScroll = () => setIsScrolled(window.scrollY > 50);
    window.addEventListener("scroll", handleScroll);
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Element;
      if (!target.closest(".profile-dropdown-container")) {
        setIsProfileDropdownOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const navLinks = [
    { name: "Home", to: "/" },
    { name: "Collection", to: "/books" },
    ...(user?.role === "Admin"
      ? [{ name: "Dashboard", to: "/admin/dashboard" }]
      : []),
  ];

  const isActive = (linkTo: string) => {
    if (linkTo === "/") return location.pathname === "/";
    return location.pathname === linkTo;
  };

  const handleNavigation = (path: string) => {
    navigate(path);
    setIsMobileMenuOpen(false);
    setIsProfileDropdownOpen(false);
  };

  const handleLogout = () => {
    setUser(null);
    localStorage.removeItem("user");
    setIsProfileDropdownOpen(false);
    navigate("/login");
  };

  return (
    <nav className={`navbar ${isScrolled ? "scrolled" : ""}`}>
      <div className="navbar-container">
        <div className="logo" onClick={() => handleNavigation("/")}>
          <BookOpen className="logo-icon" strokeWidth={1.5} />
          <div className="logo-text">
            <span>BABEL</span>
            <span>Literary Sanctuary</span>
          </div>
        </div>

        <div className="nav-links">
          {navLinks.map((link) => (
            <Link
              key={link.name}
              to={link.to}
              className={isActive(link.to) ? "active" : ""}
            >
              {link.name}
            </Link>
          ))}
        </div>

        <div className="profile-dropdown-container">
          {user ? (
            <div className="user-profile">
              <button
                className="profile-trigger"
                onClick={() => setIsProfileDropdownOpen(!isProfileDropdownOpen)}
              >
                <User size={24} />
                <span className="username">{user.userName}</span>
              </button>

              {isProfileDropdownOpen && (
                <div className="profile-dropdown">
                  <div className="dropdown-header">
                    <div className="user-avatar">
                      <User size={20} />
                    </div>
                    <div className="user-info">
                      <div className="dropdown-username">{user.userName}</div>
                      <div className="dropdown-email">{user.email}</div>
                    </div>
                  </div>

                  <div className="dropdown-divider"></div>

                  <button
                    className="dropdown-item"
                    onClick={() => handleNavigation("/profile")}
                  >
                    <Settings size={16} />
                    Profile Settings
                  </button>

                  <button
                    className="dropdown-item"
                    onClick={() => handleNavigation("/favorites")}
                  >
                    <Heart size={16} />
                    Favorites
                  </button>

                  <button
                    className="dropdown-item"
                    onClick={() => handleNavigation("/borrows")}
                  >
                    <Library size={16} />
                    My Borrows
                  </button>

                  <div className="dropdown-divider"></div>

                  <button
                    className="dropdown-item logout"
                    onClick={handleLogout}
                  >
                    <LogIn size={16} />
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          ) : (
            <button
              className="login-btn"
              onClick={() => handleNavigation("/login")}
            >
              <LogIn size={24} />
              <span>Login</span>
            </button>
          )}
        </div>

        <div className="mobile-menu-btn">
          <button onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}>
            {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </div>
      </div>

      {isMobileMenuOpen && (
        <div className="mobile-menu">
          {navLinks.map((link) => (
            <Link
              key={link.name}
              to={link.to}
              onClick={() => setIsMobileMenuOpen(false)}
              className={isActive(link.to) ? "active" : ""}
            >
              {link.name}
            </Link>
          ))}

          {user ? (
            <>
              <div className="mobile-user-info">
                <User size={20} />
                <span>{user.userName}</span>
              </div>

              <Link
                to="/profile"
                onClick={() => setIsMobileMenuOpen(false)}
                className="mobile-nav-link"
              >
                <Settings size={16} />
                Profile Settings
              </Link>

              {/* Added Favorites link for mobile */}
              <Link
                to="/favorites"
                onClick={() => setIsMobileMenuOpen(false)}
                className="mobile-nav-link"
              >
                <Heart size={16} />
                Favorites
              </Link>

              <Link
                to="/borrows"
                onClick={() => setIsMobileMenuOpen(false)}
                className="mobile-nav-link"
              >
                <Library size={16} />
                My Borrows
              </Link>

              {user?.role === "Admin" && (
                <Link
                  to="/admin/dashboard"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="mobile-nav-link"
                >
                  <Workflow size={16} />
                  Admin Dashboard
                </Link>
              )}

              <button onClick={handleLogout} className="mobile-nav-link logout">
                <LogIn size={16} />
                Sign Out
              </button>
            </>
          ) : (
            <Link
              to="/login"
              onClick={() => setIsMobileMenuOpen(false)}
              className="mobile-login-link"
            >
              <LogIn size={16} />
              Login
            </Link>
          )}
        </div>
      )}
    </nav>
  );
};

export default Navbar;
