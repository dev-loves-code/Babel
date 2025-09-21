// App.tsx
import { useLocation } from "react-router-dom";
import "./App.css";
import Home from "./pages/Home/Home";
import Navbar from "./components/Navbar/Navbar";
import Footer from "./components/Footer/Footer";
import BookListPage from "./pages/BookListPage/BookListPage";
import { Navigate, Route, Routes } from "react-router-dom";
import BookDetailsPage from "./pages/BookDetailsPage/BookDetailsPage";
import RegisterForm from "./pages/Register/Register";
import LoginForm from "./pages/Login/Login";
import ProfilePage from "./pages/Profile/Profile";
import FavoritesPage from "./pages/FavoritesPage/FavoritesPage";
import MyBorrowsPage from "./pages/MyBorrowsPage/MyBorrowsPage";
import ProtectedRoute from "./components/ProtectedRoute";

import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminLayout from "./pages/admin/AdminLayout";
import UsersPage from "./pages/admin/Users";
import GenresPage from "./pages/admin/Genres";
import AuthorsPage from "./pages/admin/Authors";
import BooksPage from "./pages/admin/Books";
import BorrowsPage from "./pages/admin/Borrows";

function App() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith("/admin");

  return (
    <div>
      {!isAdminRoute && <Navbar />}
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/signup" element={<RegisterForm />} />
        <Route path="/login" element={<LoginForm />} />
        <Route path="/books" element={<BookListPage />} />
        <Route path="/books/:id" element={<BookDetailsPage />} />

        {/* Protected Routes */}
        <Route
          path="/profile"
          element={
            <ProtectedRoute>
              <ProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/favorites"
          element={
            <ProtectedRoute>
              <FavoritesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/borrows"
          element={
            <ProtectedRoute>
              <MyBorrowsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/admin"
          element={
            <ProtectedRoute requiredRole="Admin">
              <AdminLayout />
            </ProtectedRoute>
          }
        >
          <Route path="dashboard" element={<AdminDashboard />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="genres" element={<GenresPage />} />
          <Route path="authors" element={<AuthorsPage />} />
          <Route path="books" element={<BooksPage />} />
          <Route path="borrows" element={<BorrowsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      {!isAdminRoute && <Footer />}
    </div>
  );
}

export default App;
