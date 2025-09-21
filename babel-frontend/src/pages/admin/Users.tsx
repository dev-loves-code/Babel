import { useState, useEffect } from "react";
import axios, { type AxiosResponse } from "axios";
import { useAuth } from "../../context/AuthContext";
import type { AdminUserDto } from "../../types/admin.types";
import "./AdminPannel.css";

const API_URL = "http://localhost:5286/api";

export default function UsersPage() {
  const { user } = useAuth();
  const [query, setQuery] = useState("");
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [processingUserId, setProcessingUserId] = useState<string | null>(null);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const searchUsers = async (searchTerm: string) => {
    if (!searchTerm.trim()) {
      setUsers([]);
      return;
    }

    setLoading(true);
    try {
      const res: AxiosResponse<AdminUserDto[]> = await axios.get(
        `${API_URL}/account/search?query=${encodeURIComponent(searchTerm)}`,
        { headers: getAuthHeader() }
      );
      setUsers(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const delayDebounce = setTimeout(() => {
      setLoading(true);
      if (query.trim() === "") {
        // fetch all users if query is empty
        axios
          .get(`${API_URL}/account/search`, {
            headers: getAuthHeader(),
            params: { cache: "no-cache" },
          })
          .then((res) => setUsers(res.data))
          .catch((err) => console.error(err))
          .finally(() => setLoading(false));
      } else {
        // fetch filtered users
        axios
          .get(`${API_URL}/account/search?query=${encodeURIComponent(query)}`, {
            headers: getAuthHeader(),
            params: { cache: "no-cache" },
          })
          .then((res) => setUsers(res.data))
          .catch((err) => console.error(err))
          .finally(() => setLoading(false));
      }
    }, 300); // 300ms debounce

    return () => clearTimeout(delayDebounce);
  }, [query]);

  const toggleBlock = async (userId: string, isBlocked: boolean) => {
    setProcessingUserId(userId);
    try {
      const endpoint = `${API_URL}/account/${isBlocked ? "unblock" : "block"}/${userId}`;
      await axios.post(endpoint, {}, { headers: getAuthHeader() });

      // Optimistic update
      setUsers((prev) =>
        prev.map((u) =>
          u.userId === userId ? { ...u, isBlocked: !isBlocked } : u
        )
      );
    } catch (err) {
      console.error(err);
      alert("Error processing request. Please try again.");
    } finally {
      setProcessingUserId(null);
    }
  };

  return (
    <div className="users-page">
      <h1 className="page-title">User Management</h1>

      <div className="search-container">
        <div className="search-input-wrapper">
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by username or email..."
            className="search-input"
            disabled={loading}
          />
          {loading && <div className="search-spinner"></div>}
        </div>
      </div>

      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Email</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.userId}>
                <td>
                  <div className="user-info">
                    <div className="username">{u.userName}</div>
                  </div>
                </td>
                <td>
                  <div className="email">{u.email}</div>
                </td>
                <td>
                  <span
                    className={`status-badge ${u.isBlocked ? "blocked" : "active"}`}
                  >
                    {u.isBlocked ? "Blocked" : "Active"}
                  </span>
                </td>
                <td>
                  <button
                    onClick={() => toggleBlock(u.userId, u.isBlocked)}
                    disabled={processingUserId === u.userId}
                    className={`action-btn ${u.isBlocked ? "unblock-btn" : "block-btn"}`}
                  >
                    {processingUserId === u.userId
                      ? "Processing..."
                      : u.isBlocked
                        ? "Unblock"
                        : "Block"}
                  </button>
                </td>
              </tr>
            ))}
            {users.length === 0 && !loading && (
              <tr>
                <td colSpan={4} className="no-data">
                  {query.trim() === ""
                    ? "Start typing to search for users"
                    : "No users found"}
                </td>
              </tr>
            )}
            {loading && (
              <tr>
                <td colSpan={4} className="loading-data">
                  <div className="loading-spinner"></div>
                  Searching users...
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
