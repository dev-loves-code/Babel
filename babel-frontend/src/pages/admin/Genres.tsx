import { useState, useEffect } from "react";
import axios, { type AxiosResponse } from "axios";
import { useAuth } from "../../context/AuthContext";
import "./AdminPannel.css";

interface GenreDto {
  id: number;
  name: string;
  books?: { id: number; title: string; quantity: number; authorName: string }[];
}

const API_URL = "http://localhost:5286/api";

export default function GenresPage() {
  const { user } = useAuth();
  const [genres, setGenres] = useState<GenreDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [newGenre, setNewGenre] = useState("");
  const [editGenreId, setEditGenreId] = useState<number | null>(null);
  const [editGenreName, setEditGenreName] = useState("");
  const [deleteGenreId, setDeleteGenreId] = useState<number | null>(null);
  const [processingAction, setProcessingAction] = useState<string | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const fetchGenres = async () => {
    setLoading(true);
    try {
      const res: AxiosResponse<GenreDto[]> = await axios.get(
        `${API_URL}/genre`,
        { headers: getAuthHeader() }
      );
      setGenres(res.data);
    } catch (err) {
      console.error(err);
      alert("Error fetching genres. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGenres();
  }, []);

  const createGenre = async () => {
    if (!newGenre.trim()) return;

    setProcessingAction("create");
    try {
      const res = await axios.post(
        `${API_URL}/genre`,
        { name: newGenre.trim() },
        { headers: getAuthHeader() }
      );
      setGenres((prev) => [...prev, res.data]);
      setNewGenre("");
      setShowAddModal(false);
    } catch (err) {
      console.error(err);
      alert("Error creating genre. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const updateGenre = async (id: number) => {
    if (!editGenreName.trim()) return;

    setProcessingAction(`update-${id}`);
    try {
      const res = await axios.put(
        `${API_URL}/genre/${id}`,
        { name: editGenreName.trim() },
        { headers: getAuthHeader() }
      );
      setGenres((prev) => prev.map((g) => (g.id === id ? res.data : g)));
      setEditGenreId(null);
      setEditGenreName("");
    } catch (err) {
      console.error(err);
      alert("Error updating genre. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const confirmDeleteGenre = async () => {
    if (deleteGenreId === null) return;

    setProcessingAction(`delete-${deleteGenreId}`);
    try {
      await axios.delete(`${API_URL}/genre/${deleteGenreId}`, {
        headers: getAuthHeader(),
      });
      setGenres((prev) => prev.filter((g) => g.id !== deleteGenreId));
      setDeleteGenreId(null);
    } catch (err) {
      console.error(err);
      alert("Cannot delete genre because it has associated books.");
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
      <h1 className="authors-title">Manage Genres</h1>

      {/* Create new genre button */}
      <div className="authors-create">
        <button
          onClick={() => setShowAddModal(true)}
          className="authors-button add-button"
        >
          + Add New Genre
        </button>
      </div>

      {/* Genres table */}
      <table className="authors-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {genres.map((g) => (
            <tr key={g.id}>
              <td data-label="ID">{g.id}</td>
              <td data-label="Name">
                {editGenreId === g.id ? (
                  <input
                    value={editGenreName}
                    onChange={(e) => setEditGenreName(e.target.value)}
                    onKeyPress={(e) =>
                      handleKeyPress(e, () => updateGenre(g.id))
                    }
                    className="authors-input"
                    autoFocus
                    disabled={processingAction === `update-${g.id}`}
                  />
                ) : (
                  g.name
                )}
              </td>
              <td data-label="Actions" className="authors-actions">
                {editGenreId === g.id ? (
                  <>
                    <button
                      onClick={() => updateGenre(g.id)}
                      className="authors-button save-button"
                      disabled={
                        !editGenreName.trim() ||
                        processingAction === `update-${g.id}`
                      }
                    >
                      {processingAction === `update-${g.id}`
                        ? "Saving..."
                        : "Save"}
                    </button>
                    <button
                      onClick={() => {
                        setEditGenreId(null);
                        setEditGenreName("");
                      }}
                      className="authors-button cancel-button"
                      disabled={processingAction === `update-${g.id}`}
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <>
                    <button
                      onClick={() => {
                        setEditGenreId(g.id);
                        setEditGenreName(g.name);
                      }}
                      className="authors-button edit-button"
                      disabled={
                        editGenreId !== null || processingAction !== null
                      }
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setDeleteGenreId(g.id)}
                      className="authors-button delete-button"
                      disabled={
                        editGenreId !== null || processingAction !== null
                      }
                    >
                      Delete
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {genres.length === 0 && !loading && (
            <tr>
              <td colSpan={3}>No genres found.</td>
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
                  Loading genres...
                </div>
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Add Genre Modal */}
      {showAddModal && (
        <div className="add-author-modal">
          <div className="add-author-modal-content">
            <div className="add-author-modal-header">
              <h3>Add New Genre</h3>
            </div>
            <div className="add-author-modal-body">
              <input
                value={newGenre}
                onChange={(e) => setNewGenre(e.target.value)}
                onKeyPress={(e) => handleKeyPress(e, createGenre)}
                placeholder="Enter genre name..."
                className="add-author-input"
                autoFocus
                disabled={processingAction === "create"}
              />
            </div>
            <div className="add-author-modal-actions">
              <button
                onClick={createGenre}
                className="add-author-submit-btn"
                disabled={!newGenre.trim() || processingAction === "create"}
              >
                {processingAction === "create" ? "Adding..." : "Add Genre"}
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
      {deleteGenreId !== null && (
        <div className="authors-modal">
          <div className="authors-modal-content">
            <p>Are you sure you want to delete this genre?</p>
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
                onClick={confirmDeleteGenre}
                className="authors-button delete-button"
                disabled={processingAction === `delete-${deleteGenreId}`}
              >
                {processingAction === `delete-${deleteGenreId}`
                  ? "Deleting..."
                  : "Yes, Delete"}
              </button>
              <button
                onClick={() => setDeleteGenreId(null)}
                className="authors-button cancel-button"
                disabled={processingAction === `delete-${deleteGenreId}`}
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
