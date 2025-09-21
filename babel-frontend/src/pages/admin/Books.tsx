import { useState, useEffect } from "react";
import axios, { type AxiosResponse } from "axios";
import { useAuth } from "../../context/AuthContext";
import "./AdminPannel.css";

interface BookDto {
  id: number;
  title: string;
  quantity: number;
  genreId: number;
  genreName: string;
  authorId: number;
  authorName: string;
}

interface GenreDto {
  id: number;
  name: string;
}

interface AuthorDto {
  id: number;
  name: string;
}

const API_URL = "http://localhost:5286/api";

export default function BooksPage() {
  const { user } = useAuth();
  const [books, setBooks] = useState<BookDto[]>([]);
  const [genres, setGenres] = useState<GenreDto[]>([]);
  const [authors, setAuthors] = useState<AuthorDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [newBook, setNewBook] = useState({
    title: "",
    quantity: 0,
    genreId: 0,
    authorId: 0,
  });
  const [editBookId, setEditBookId] = useState<number | null>(null);
  const [editBook, setEditBook] = useState({
    title: "",
    quantity: 0,
    genreId: 0,
    authorId: 0,
  });
  const [deleteBookId, setDeleteBookId] = useState<number | null>(null);
  const [processingAction, setProcessingAction] = useState<string | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const fetchBooks = async () => {
    setLoading(true);
    try {
      const res: AxiosResponse<BookDto[]> = await axios.get(`${API_URL}/book`, {
        headers: getAuthHeader(),
      });
      setBooks(res.data);
    } catch (err) {
      console.error(err);
      alert("Error fetching books. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const fetchGenres = async () => {
    try {
      const res: AxiosResponse<GenreDto[]> = await axios.get(
        `${API_URL}/genre`,
        { headers: getAuthHeader() }
      );
      setGenres(res.data);
    } catch (err) {
      console.error(err);
      alert("Error fetching genres. Please try again.");
    }
  };

  const fetchAuthors = async () => {
    try {
      const res: AxiosResponse<AuthorDto[]> = await axios.get(
        `${API_URL}/author`,
        { headers: getAuthHeader() }
      );
      setAuthors(res.data);
    } catch (err) {
      console.error(err);
      alert("Error fetching authors. Please try again.");
    }
  };

  useEffect(() => {
    fetchBooks();
    fetchGenres();
    fetchAuthors();
  }, []);

  const createBook = async () => {
    if (
      !newBook.title.trim() ||
      newBook.quantity <= 0 ||
      newBook.genreId <= 0 ||
      newBook.authorId <= 0
    ) {
      alert("Please fill all fields with valid values.");
      return;
    }

    setProcessingAction("create");
    try {
      const res = await axios.post(`${API_URL}/book`, newBook, {
        headers: getAuthHeader(),
      });
      setBooks((prev) => [...prev, res.data]);
      setNewBook({ title: "", quantity: 0, genreId: 0, authorId: 0 });
      setShowAddModal(false);
    } catch (err) {
      console.error(err);
      alert("Error creating book. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const updateBook = async (id: number) => {
    if (
      !editBook.title.trim() ||
      editBook.quantity <= 0 ||
      editBook.genreId <= 0 ||
      editBook.authorId <= 0
    ) {
      alert("Please fill all fields with valid values.");
      return;
    }

    setProcessingAction(`update-${id}`);
    try {
      const res = await axios.put(`${API_URL}/book/${id}`, editBook, {
        headers: getAuthHeader(),
      });
      setBooks((prev) => prev.map((b) => (b.id === id ? res.data : b)));
      setEditBookId(null);
    } catch (err) {
      console.error(err);
      alert("Error updating book. Please try again.");
    } finally {
      setProcessingAction(null);
    }
  };

  const confirmDeleteBook = async () => {
    if (deleteBookId === null) return;

    setProcessingAction(`delete-${deleteBookId}`);
    try {
      await axios.delete(`${API_URL}/book/${deleteBookId}`, {
        headers: getAuthHeader(),
      });
      setBooks((prev) => prev.filter((b) => b.id !== deleteBookId));
      setDeleteBookId(null);
    } catch (err) {
      console.error(err);
      alert(
        "Cannot delete book because it has active borrow or return requests."
      );
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
    <div className="books-container">
      <h1 className="books-title">Manage Books</h1>

      {/* Create new book button */}
      <div className="books-create">
        <button
          onClick={() => setShowAddModal(true)}
          className="books-button add-button"
        >
          + Add New Book
        </button>
      </div>

      {/* Books table */}
      <table className="books-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Title</th>
            <th>Quantity</th>
            <th>Genre</th>
            <th>Author</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {books.map((b) => (
            <tr key={b.id}>
              <td data-label="ID">{b.id}</td>
              <td data-label="Title">
                {editBookId === b.id ? (
                  <input
                    value={editBook.title}
                    onChange={(e) =>
                      setEditBook({ ...editBook, title: e.target.value })
                    }
                    className="books-input"
                    disabled={processingAction === `update-${b.id}`}
                  />
                ) : (
                  b.title
                )}
              </td>
              <td data-label="Quantity">
                {editBookId === b.id ? (
                  <input
                    type="number"
                    value={editBook.quantity}
                    onChange={(e) =>
                      setEditBook({
                        ...editBook,
                        quantity: Number(e.target.value),
                      })
                    }
                    className="books-input"
                    disabled={processingAction === `update-${b.id}`}
                  />
                ) : (
                  b.quantity
                )}
              </td>
              <td data-label="Genre">
                {editBookId === b.id ? (
                  <select
                    value={editBook.genreId}
                    onChange={(e) =>
                      setEditBook({
                        ...editBook,
                        genreId: Number(e.target.value),
                      })
                    }
                    className="books-select"
                    disabled={processingAction === `update-${b.id}`}
                  >
                    <option value={0}>Select Genre</option>
                    {genres.map((genre) => (
                      <option key={genre.id} value={genre.id}>
                        {genre.name}
                      </option>
                    ))}
                  </select>
                ) : (
                  b.genreName
                )}
              </td>
              <td data-label="Author">
                {editBookId === b.id ? (
                  <select
                    value={editBook.authorId}
                    onChange={(e) =>
                      setEditBook({
                        ...editBook,
                        authorId: Number(e.target.value),
                      })
                    }
                    className="books-select"
                    disabled={processingAction === `update-${b.id}`}
                  >
                    <option value={0}>Select Author</option>
                    {authors.map((author) => (
                      <option key={author.id} value={author.id}>
                        {author.name}
                      </option>
                    ))}
                  </select>
                ) : (
                  b.authorName
                )}
              </td>
              <td data-label="Actions" className="books-actions">
                {editBookId === b.id ? (
                  <>
                    <button
                      onClick={() => updateBook(b.id)}
                      className="books-button save-button"
                      disabled={processingAction === `update-${b.id}`}
                    >
                      {processingAction === `update-${b.id}`
                        ? "Saving..."
                        : "Save"}
                    </button>
                    <button
                      onClick={() => setEditBookId(null)}
                      className="books-button cancel-button"
                      disabled={processingAction === `update-${b.id}`}
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <>
                    <button
                      onClick={() => {
                        setEditBookId(b.id);
                        setEditBook({
                          title: b.title,
                          quantity: b.quantity,
                          genreId: b.genreId,
                          authorId: b.authorId,
                        });
                      }}
                      className="books-button edit-button"
                      disabled={
                        editBookId !== null || processingAction !== null
                      }
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setDeleteBookId(b.id)}
                      className="books-button delete-button"
                      disabled={
                        editBookId !== null || processingAction !== null
                      }
                    >
                      Delete
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {books.length === 0 && !loading && (
            <tr>
              <td colSpan={6}>No books found.</td>
            </tr>
          )}
          {loading && (
            <tr>
              <td colSpan={6} className="loading-data">
                <div className="loading-spinner"></div>
                Loading books...
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Add Book Modal */}
      {showAddModal && (
        <div className="add-book-modal">
          <div className="add-book-modal-content">
            <div className="add-book-modal-header">
              <h3>Add New Book</h3>
            </div>
            <div className="add-book-modal-body">
              <input
                type="text"
                placeholder="Title"
                value={newBook.title}
                onChange={(e) =>
                  setNewBook({ ...newBook, title: e.target.value })
                }
                onKeyPress={(e) => handleKeyPress(e, createBook)}
                className="add-book-input"
                autoFocus
                disabled={processingAction === "create"}
              />
              <input
                type="number"
                placeholder="Quantity"
                value={newBook.quantity || ""}
                onChange={(e) =>
                  setNewBook({ ...newBook, quantity: Number(e.target.value) })
                }
                onKeyPress={(e) => handleKeyPress(e, createBook)}
                className="add-book-input"
                disabled={processingAction === "create"}
              />
              <select
                value={newBook.genreId}
                onChange={(e) =>
                  setNewBook({ ...newBook, genreId: Number(e.target.value) })
                }
                className="add-book-select"
                disabled={processingAction === "create"}
              >
                <option value={0}>Select Genre</option>
                {genres.map((genre) => (
                  <option key={genre.id} value={genre.id}>
                    {genre.name}
                  </option>
                ))}
              </select>
              <select
                value={newBook.authorId}
                onChange={(e) =>
                  setNewBook({ ...newBook, authorId: Number(e.target.value) })
                }
                className="add-book-select"
                disabled={processingAction === "create"}
              >
                <option value={0}>Select Author</option>
                {authors.map((author) => (
                  <option key={author.id} value={author.id}>
                    {author.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="add-book-modal-actions">
              <button
                onClick={createBook}
                className="add-book-submit-btn"
                disabled={processingAction === "create"}
              >
                {processingAction === "create" ? "Adding..." : "Add Book"}
              </button>
              <button
                onClick={() => setShowAddModal(false)}
                className="add-book-cancel-btn"
                disabled={processingAction === "create"}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirmation modal */}
      {deleteBookId !== null && (
        <div className="books-modal">
          <div className="books-modal-content">
            <p>Are you sure you want to delete this book?</p>
            <p className="warning-text">This action cannot be undone.</p>
            <div className="books-modal-actions">
              <button
                onClick={confirmDeleteBook}
                className="books-button delete-button"
                disabled={processingAction === `delete-${deleteBookId}`}
              >
                {processingAction === `delete-${deleteBookId}`
                  ? "Deleting..."
                  : "Yes, Delete"}
              </button>
              <button
                onClick={() => setDeleteBookId(null)}
                className="books-button cancel-button"
                disabled={processingAction === `delete-${deleteBookId}`}
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
