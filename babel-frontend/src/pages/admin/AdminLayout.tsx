import { Outlet, NavLink } from "react-router-dom";
import React from "react";
import "./AdminLayout.css"; // optional external CSS

const AdminLayout: React.FC = () => {
  return (
    <div className="admin-layout">
      {/* Sidebar */}
      <aside className="admin-sidebar">
        <h2 className="admin-title">Admin</h2>
        <nav className="admin-nav">
          <NavLink
            to="dashboard"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Dashboard
          </NavLink>
          <NavLink
            to="users"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Users
          </NavLink>
          <NavLink
            to="genres"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Genres
          </NavLink>
          <NavLink
            to="authors"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Authors
          </NavLink>
          <NavLink
            to="books"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Books
          </NavLink>
          <NavLink
            to="borrows"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Borrows
          </NavLink>
          <NavLink
            to="/home"
            className={({ isActive }) =>
              "admin-link" + (isActive ? " active" : "")
            }
          >
            Home
          </NavLink>
        </nav>
      </aside>

      {/* Main content */}
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  );
};

export default AdminLayout;
