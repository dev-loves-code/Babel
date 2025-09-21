import React, { useState, useEffect } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import { bookService } from "../../services/bookService";
import { useAuth } from "../../context/AuthContext";
import axios from "axios";
import "./BookDetailsPage.css";

const BookDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const [book, setBook] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [inWishlist, setInWishlist] = useState(false);

  const [isBorrowed, setIsBorrowed] = useState(false);

  // Check if book is already borrowed by current user
  useEffect(() => {
    const checkBorrowStatus = async () => {
      if (!user || !id) return;
      try {
        const res = await axios.get(`http://localhost:5286/api/borrow/${id}`, {
          headers: getAuthHeader(),
        });
        // If borrow exists => book is borrowed
        setIsBorrowed(res.data !== null);
      } catch (err) {
        console.error("Failed to check borrow status", err);
      }
    };
    checkBorrowStatus();
  }, [user, id]);

  const handleBorrowBook = async () => {
    if (!user || !id || isBorrowed) return;
    try {
      await axios.post(
        "http://localhost:5286/api/borrow",
        { BookId: Number(id) },
        { headers: getAuthHeader() }
      );
      setIsBorrowed(true);
    } catch (err) {
      console.error("Failed to borrow book", err);
      alert("Failed to borrow book. Try again.");
    }
  };

  useEffect(() => {
    const fetchBook = async () => {
      if (!id || isNaN(Number(id))) {
        setError("Invalid book ID");
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError("");

        const bookData = await bookService.getBookById(Number(id));

        // Fetch thumbnail from Google Books API
        const googleApiUrl = `https://www.googleapis.com/books/v1/volumes?q=intitle:${encodeURIComponent(bookData.title)}`;
        const response = await fetch(googleApiUrl);
        const data = await response.json();

        const thumbnail =
          data.items?.[0]?.volumeInfo?.imageLinks?.thumbnail ||
          "/default-book-placeholder.png";

        setBook({ ...bookData, thumbnail });
      } catch (err: any) {
        if (err.response?.status === 404) setError("Book not found");
        else setError("Failed to load book details. Please try again.");
        console.error("Error fetching book:", err);
      } finally {
        setLoading(false);
      }
    };

    fetchBook();
  }, [id]);

  useEffect(() => {
    const checkWishlist = async () => {
      if (!user || !id) return;
      try {
        const res = await axios.get(
          `http://localhost:5286/api/wishlist/is-in-wishlist/${id}`,
          { headers: getAuthHeader() }
        );
        setInWishlist(res.data === true);
      } catch (err) {
        console.error("Failed to check wishlist status", err);
      }
    };
    checkWishlist();
  }, [user, id]);

  const handleToggleWishlist = async () => {
    if (!user || !id) return;

    try {
      if (inWishlist) {
        // ❌ Remove from wishlist
        await axios.delete(`http://localhost:5286/api/wishlist/book/${id}`, {
          headers: getAuthHeader(),
        });
        setInWishlist(false);
      } else {
        // ➕ Add to wishlist
        await axios.post(
          `http://localhost:5286/api/wishlist`,
          { bookId: Number(id) },
          { headers: getAuthHeader() }
        );
        setInWishlist(true);
      }
    } catch (err) {
      console.error("Failed to toggle wishlist", err);
    }
  };
  const handleGoBack = () => {
    navigate(-1);
  };

  if (loading) {
    return (
      <div className="book-details-container">
        <div className="loading">Loading book details...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="book-details-container">
        <div className="error-container">
          <div className="error-message">{error}</div>
          <div className="error-actions">
            <button onClick={handleGoBack} className="btn btn-secondary">
              Go Back
            </button>
            <Link to="/books" className="btn btn-primary">
              Browse All Books
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (!book) {
    return (
      <div className="book-details-container">
        <div className="error-container">
          <div className="error-message">Book not found</div>
          <div className="error-actions">
            <button onClick={handleGoBack} className="btn btn-secondary">
              Go Back
            </button>
            <Link to="/books" className="btn btn-primary">
              Browse All Books
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="book-details-container">
      <div className="navigation-header">
        <button onClick={handleGoBack} className="btn btn-secondary back-btn">
          ← Back
        </button>
        <Link to="/books" className="btn btn-outline">
          Browse All Books
        </Link>
      </div>

      <div className="book-details-card">
        <div className="book-header">
          <div className="book-image">
            <img
              src={book.thumbnail}
              alt={book.title}
              onError={(e) => {
                (e.target as HTMLImageElement).src =
                  "/default-book-placeholder.png";
              }}
            />
          </div>
          <div className="book-title-section">
            <h1 className="book-title">{book.title}</h1>
            <div className="book-meta">
              <span className="book-id">ID: {book.id}</span>
            </div>
          </div>
          <div className="availability-section">
            <div
              className={`availability-badge ${book.quantity === 0 ? "out-of-stock" : "in-stock"}`}
            >
              {book.quantity === 0 ? "Out of Stock" : "In Stock"}
            </div>
            <div className="quantity-info">
              <span className="quantity-number">{book.quantity}</span>
              <span className="quantity-label">available</span>
            </div>
          </div>
        </div>
        <div className="book-content">
          <div className="book-details-grid">
            <div className="detail-section">
              <h2>Author Information</h2>
              <div className="detail-item">
                <label>Author</label>
                <span className="author-name">{book.authorName}</span>
              </div>
              <div className="detail-item">
                <label>Author ID</label>
                <span>{book.authorId}</span>
              </div>
            </div>
            <div className="detail-section">
              <h2>Genre Information</h2>
              <div className="detail-item">
                <label>Genre</label>
                <span className="genre-name">{book.genreName}</span>
              </div>
              <div className="detail-item">
                <label>Genre ID</label>
                <span>{book.genreId}</span>
              </div>
            </div>
            <div className="detail-section">
              <h2>Inventory</h2>
              <div className="detail-item">
                <label>Current Stock</label>
                <span
                  className={`stock-level ${book.quantity <= 5 ? "low-stock" : ""}`}
                >
                  {book.quantity} {book.quantity === 1 ? "copy" : "copies"}
                </span>
              </div>
              <div className="detail-item">
                <label>Status</label>
                <span
                  className={`status ${book.quantity === 0 ? "unavailable" : "available"}`}
                >
                  {book.quantity === 0 ? "Unavailable" : "Available"}
                </span>
              </div>
            </div>
          </div>
          <div className="actions-section">
            <h2>Actions</h2>
            <div className="action-buttons">
              <button
                className={`btn ${book.quantity === 0 || !user || isBorrowed ? "btn-disabled" : "btn-success"}`}
                disabled={
                  book.quantity === 0 || !user || isBorrowed || user.isBlocked
                }
                onClick={handleBorrowBook}
              >
                {isBorrowed ? "Borrowed" : "Borrow Book"}
              </button>

              <button
                className={`btn btn-outline ${!user ? "btn-disabled" : ""}`}
                disabled={!user || user.isBlocked}
                onClick={handleToggleWishlist}
              >
                {inWishlist ? "Remove from Wishlist" : "Add to Wishlist"}
              </button>
              <button className="btn btn-secondary">Share Book</button>
            </div>
          </div>
        </div>
      </div>
      <div className="related-actions">
        <Link
          to={`/books?authorName=${encodeURIComponent(book.authorName)}`}
          className="btn btn-outline"
        >
          More by {book.authorName}
        </Link>
        <Link
          to={`/books?genreName=${encodeURIComponent(book.genreName)}`}
          className="btn btn-outline"
        >
          More {book.genreName} Books
        </Link>
      </div>
    </div>
  );
};

export default BookDetailsPage;
