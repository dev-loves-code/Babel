import { useState, useEffect } from "react";
import axios, { type AxiosResponse } from "axios";
import { useAuth } from "../../context/AuthContext";
import "./AdminPannel.css";

interface AuthorDto {
  id: number;
  name: string;
  books?: { id: number; title: string; quantity: number; genreName: string }[];
}

const API_URL = "http://localhost:5286/api";

export default function AuthorsPage() {
  const { user } = useAuth();
  const [authors, setAuthors] = useState<AuthorDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [newAuthor, setNewAuthor] = useState("");
  const [editAuthorId, setEditAuthorId] = useState<number | null>(null);
  const [editAuthorName, setEditAuthorName] = useState("");
  const [deleteAuthorId, setDeleteAuthorId] = useState<number | null>(null);
  const [processingAction, setProcessingAction] = useState<string | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const fetchAuthors = async () => {
    setLoading(true);
    try {
      const res: AxiosResponse<AuthorDto[]> = await axios.get(
        `${API_URL}/author`,
        { headers: getAuthHeader() }
      );
      setAuthors(res.data);
    } catch (err) {
      console.error(err);
      alert("Error fetching authors. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAuthors();
  }, []);

  const createAuthor = async () => {
    if (!newAuthor.trim()) return;

    setProcessingAction("create");
    try {
      const res = await axios.post(
        `${API_URL}/author`,
        { name: newAuthor },
        { headers: getAuthHeader() }
      );
      setAuthors((prev) => [...prev, res.data]);
      setNewAuthor("");
      setShowAddModal(false);
    } catch (err) {
      console.error(err);
      alert("Error creating author. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const updateAuthor = async (id: number) => {
    if (!editAuthorName.trim()) return;

    setProcessingAction(`update-${id}`);
    try {
      const res = await axios.put(
        `${API_URL}/author/${id}`,
        { name: editAuthorName },
        { headers: getAuthHeader() }
      );
      setAuthors((prev) => prev.map((a) => (a.id === id ? res.data : a)));
      setEditAuthorId(null);
      setEditAuthorName("");
    } catch (err) {
      console.error(err);
      alert("Error updating author. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const confirmDeleteAuthor = async () => {
    if (deleteAuthorId === null) return;

    setProcessingAction(`delete-${deleteAuthorId}`);
    try {
      await axios.delete(`${API_URL}/author/${deleteAuthorId}`, {
        headers: getAuthHeader(),
      });
      setAuthors((prev) => prev.filter((a) => a.id !== deleteAuthorId));
      setDeleteAuthorId(null);
    } catch (err) {
      console.error(err);
      alert("Cannot delete author because they have associated books.");
    } finally {
      setProcessingAction(null);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent, action: () => void) => {
    if (e.key === "Enter") {
      action();
    }
  };

  return (
    <div className="authors-container">
      <h1 className="authors-title">Manage Authors</h1>

      {/* Create new author button */}
      <div className="authors-create">
        <button
          onClick={() => setShowAddModal(true)}
          className="authors-button add-button"
        >
          + Add New Author
        </button>
      </div>

      {/* Authors table */}
      <table className="authors-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {authors.map((a) => (
            <tr key={a.id}>
              <td data-label="ID">{a.id}</td>
              <td data-label="Name">
                {editAuthorId === a.id ? (
                  <input
                    value={editAuthorName}
                    onChange={(e) => setEditAuthorName(e.target.value)}
                    onKeyPress={(e) =>
                      handleKeyPress(e, () => updateAuthor(a.id))
                    }
                    className="authors-input"
                    autoFocus
                    disabled={processingAction === `update-${a.id}`}
                  />
                ) : (
                  a.name
                )}
              </td>
              <td data-label="Actions" className="authors-actions">
                {editAuthorId === a.id ? (
                  <>
                    <button
                      onClick={() => updateAuthor(a.id)}
                      className="authors-button save-button"
                      disabled={
                        !editAuthorName.trim() ||
                        processingAction === `update-${a.id}`
                      }
                    >
                      {processingAction === `update-${a.id}`
                        ? "Saving..."
                        : "Save"}
                    </button>
                    <button
                      onClick={() => {
                        setEditAuthorId(null);
                        setEditAuthorName("");
                      }}
                      className="authors-button cancel-button"
                      disabled={processingAction === `update-${a.id}`}
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <>
                    <button
                      onClick={() => {
                        setEditAuthorId(a.id);
                        setEditAuthorName(a.name);
                      }}
                      className="authors-button edit-button"
                      disabled={
                        editAuthorId !== null || processingAction !== null
                      }
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setDeleteAuthorId(a.id)}
                      className="authors-button delete-button"
                      disabled={
                        editAuthorId !== null || processingAction !== null
                      }
                    >
                      Delete
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {authors.length === 0 && !loading && (
            <tr>
              <td colSpan={3}>No authors found.</td>
            </tr>
          )}
          {loading && (
            <tr>
              <td colSpan={3}>
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    gap: "15px",
                  }}
                >
                  <div className="loading-spinner"></div>
                  Loading authors...
                </div>
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Add Author Modal */}
      {showAddModal && (
        <div className="add-author-modal">
          <div className="add-author-modal-content">
            <div className="add-author-modal-header">
              <h3>Add New Author</h3>
            </div>
            <div className="add-author-modal-body">
              <input
                value={newAuthor}
                onChange={(e) => setNewAuthor(e.target.value)}
                onKeyPress={(e) => handleKeyPress(e, createAuthor)}
                placeholder="Enter author name..."
                className="add-author-input"
                autoFocus
                disabled={processingAction === "create"}
              />
            </div>
            <div className="add-author-modal-actions">
              <button
                onClick={createAuthor}
                className="add-author-submit-btn"
                disabled={!newAuthor.trim() || processingAction === "create"}
              >
                {processingAction === "create" ? "Adding..." : "Add Author"}
              </button>
              <button
                onClick={() => setShowAddModal(false)}
                className="add-author-cancel-btn"
                disabled={processingAction === "create"}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirmation modal */}
      {deleteAuthorId !== null && (
        <div className="authors-modal">
          <div className="authors-modal-content">
            <p>Are you sure you want to delete this author?</p>
            <p
              style={{
                color: "#e74c3c",
                fontWeight: "600",
                margin: "10px 0 0 0",
              }}
            >
              This action cannot be undone.
            </p>
            <div className="authors-modal-actions">
              <button
                onClick={confirmDeleteAuthor}
                className="authors-button delete-button"
                disabled={processingAction === `delete-${deleteAuthorId}`}
              >
                {processingAction === `delete-${deleteAuthorId}`
                  ? "Deleting..."
                  : "Yes, Delete"}
              </button>
              <button
                onClick={() => setDeleteAuthorId(null)}
                className="authors-button cancel-button"
                disabled={processingAction === `delete-${deleteAuthorId}`}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
