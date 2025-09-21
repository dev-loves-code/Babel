// pages/BookListPage.tsx
import React, { useState, useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import { bookService } from "../../services/bookService";
import type { GetBookDto, BookFilters } from "../../types/book.types";
import "./BookListPage.css";

const BookListPage: React.FC = () => {
  const location = useLocation();

  interface BookWithThumbnail extends GetBookDto {
    thumbnail: string;
  }

  const [books, setBooks] = useState<BookWithThumbnail[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>("");
  const [filters, setFilters] = useState<BookFilters>({
    title: "",
    genreName: "",
    authorName: "",
    minQuantity: "",
    maxQuantity: "",
  });

  const fetchBookThumbnail = async (title: string): Promise<string> => {
    try {
      const res = await fetch(
        `https://www.googleapis.com/books/v1/volumes?q=intitle:${encodeURIComponent(title)}`
      );
      const data = await res.json();

      // Get the thumbnail and convert to HTTPS if needed
      let thumbnail = data.items?.[0]?.volumeInfo?.imageLinks?.thumbnail;

      if (thumbnail) {
        // Convert HTTP to HTTPS to avoid mixed content issues
        thumbnail = thumbnail.replace("http://", "https://");
        return thumbnail;
      }

      return "/default-book-placeholder.png";
    } catch (err) {
      console.error("Failed to fetch book thumbnail", err);
      return "/default-book-placeholder.png";
    }
  };

  const fetchBooks = async () => {
    try {
      setLoading(true);
      setError("");

      const queryObject = {
        ...(filters.title && { Title: filters.title }),
        ...(filters.genreName && { GenreName: filters.genreName }),
        ...(filters.authorName && { AuthorName: filters.authorName }),
        ...(filters.minQuantity && {
          MinQuantity: parseInt(filters.minQuantity),
        }),
        ...(filters.maxQuantity && {
          MaxQuantity: parseInt(filters.maxQuantity),
        }),
      };

      const booksData = await bookService.getAllBooks(queryObject);

      // fetch thumbnails for each book
      const booksWithImages = await Promise.all(
        booksData.map(async (b) => ({
          ...b,
          thumbnail: await fetchBookThumbnail(b.title),
        }))
      );

      setBooks(booksWithImages);
    } catch (err) {
      setError("Failed to fetch books. Please try again.");
      console.error("Error fetching books:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const authorName = params.get("authorName") || "";
    const genreName = params.get("genreName") || "";
    const title = params.get("title") || "";

    setFilters({
      title,
      genreName,
      authorName,
      minQuantity: "",
      maxQuantity: "",
    });
  }, [location.search]);

  useEffect(() => {
    fetchBooks();
  }, [filters]);

  const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFilters((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleApplyFilters = (e: React.FormEvent) => {
    e.preventDefault();
    fetchBooks();
  };

  const handleClearFilters = () => {
    setFilters({
      title: "",
      genreName: "",
      authorName: "",
      minQuantity: "",
      maxQuantity: "",
    });
  };

  // Handle image load errors
  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement>) => {
    const target = e.target as HTMLImageElement;
    target.src = "/default-book-placeholder.png";
  };

  if (loading) return <div className="loading">Loading books...</div>;

  return (
    <div className="book-list-container">
      <header className="page-header">
        <h1>Book Library</h1>
        <p>Browse and search through our collection of books</p>
      </header>

      <div className="filters-section">
        <form onSubmit={handleApplyFilters} className="filters-form">
          <div className="filter-row">
            <div className="filter-group">
              <label htmlFor="title">Title</label>
              <input
                type="text"
                id="title"
                name="title"
                value={filters.title}
                onChange={handleFilterChange}
                placeholder="Search by title..."
              />
            </div>

            <div className="filter-group">
              <label htmlFor="genreName">Genre</label>
              <input
                type="text"
                id="genreName"
                name="genreName"
                value={filters.genreName}
                onChange={handleFilterChange}
                placeholder="Search by genre..."
              />
            </div>

            <div className="filter-group">
              <label htmlFor="authorName">Author</label>
              <input
                type="text"
                id="authorName"
                name="authorName"
                value={filters.authorName}
                onChange={handleFilterChange}
                placeholder="Search by author..."
              />
            </div>
          </div>

          <div className="filter-row">
            <div className="filter-group">
              <label htmlFor="minQuantity">Min Quantity</label>
              <input
                type="number"
                id="minQuantity"
                name="minQuantity"
                value={filters.minQuantity}
                onChange={handleFilterChange}
                placeholder="Min qty..."
                min="0"
              />
            </div>

            <div className="filter-group">
              <label htmlFor="maxQuantity">Max Quantity</label>
              <input
                type="number"
                id="maxQuantity"
                name="maxQuantity"
                value={filters.maxQuantity}
                onChange={handleFilterChange}
                placeholder="Max qty..."
                min="0"
              />
            </div>

            <div className="filter-actions">
              <button type="submit" className="btn btn-primary">
                Apply Filters
              </button>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={handleClearFilters}
              >
                Clear
              </button>
            </div>
          </div>
        </form>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="books-section">
        <div className="books-header">
          <h2>Books ({books.length})</h2>
        </div>

        {books.length === 0 ? (
          <div className="no-books">
            <p>No books found matching your criteria.</p>
          </div>
        ) : (
          <div className="books-grid">
            {books.map((book) => (
              <div key={`book-${book.id}`} className="book-card">
                <div className="book-image">
                  <img
                    src={book.thumbnail}
                    alt={book.title}
                    onError={handleImageError}
                    loading="lazy"
                  />
                </div>
                <div className="book-content">
                  <div className="book-info">
                    <h3 className="book-title">
                      <Link to={`/books/${book.id}`}>{book.title}</Link>
                    </h3>
                    <p className="book-author">by {book.authorName}</p>
                    <p className="book-genre">{book.genreName}</p>
                    <div className="book-quantity">
                      <span
                        className={`quantity-badge ${book.quantity === 0 ? "out-of-stock" : ""}`}
                      >
                        {book.quantity === 0
                          ? "Out of Stock"
                          : `${book.quantity} available`}
                      </span>
                    </div>
                  </div>
                  <div className="book-actions">
                    <Link to={`/books/${book.id}`} className="btn btn-outline">
                      View Details
                    </Link>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default BookListPage;
