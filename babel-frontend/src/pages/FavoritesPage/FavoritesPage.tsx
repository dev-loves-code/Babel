import React, { useState, useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import "./FavoritesPage.css";

interface GetWishlistDto {
  id: number;
  bookId: number;
  bookTitle: string;
  authorName: string;
  genreName: string;
  addedDate: string;
  isAvailable: boolean;
  thumbnail?: string;
}

interface WishlistFilters {
  title: string;
  authorName: string;
  genreName: string;
  availableOnly: boolean | "";
}

const FavoritesPage: React.FC = () => {
  const { user } = useAuth();
  const location = useLocation();

  const [books, setBooks] = useState<GetWishlistDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>("");
  const [filters, setFilters] = useState<WishlistFilters>({
    title: "",
    authorName: "",
    genreName: "",
    availableOnly: "",
  });

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const fetchBookThumbnail = async (title: string): Promise<string> => {
    try {
      const res = await fetch(
        `https://www.googleapis.com/books/v1/volumes?q=intitle:${encodeURIComponent(title)}`
      );
      const data = await res.json();
      let thumbnail = data.items?.[0]?.volumeInfo?.imageLinks?.thumbnail;
      if (thumbnail) thumbnail = thumbnail.replace("http://", "https://");
      return thumbnail || "/default-book-placeholder.png";
    } catch (err) {
      console.error("Failed to fetch book thumbnail", err);
      return "/default-book-placeholder.png";
    }
  };

  const fetchFavorites = async () => {
    try {
      setLoading(true);
      setError("");

      const queryObject: any = {
        ...(filters.title && { BookTitle: filters.title }),
        ...(filters.genreName && { GenreName: filters.genreName }),
        ...(filters.authorName && { AuthorName: filters.authorName }),
        ...(filters.availableOnly !== "" && {
          AvailableOnly: filters.availableOnly,
        }),
      };

      const queryParams = new URLSearchParams(queryObject).toString();

      const res = await fetch(
        `http://localhost:5286/api/wishlist?${queryParams}`,
        { headers: getAuthHeader() }
      );

      if (!res.ok) throw new Error("Failed to fetch favorites");

      const data: GetWishlistDto[] = await res.json();

      // Fetch thumbnails for each book
      const booksWithThumbnails = await Promise.all(
        data.map(async (b) => ({
          ...b,
          thumbnail: await fetchBookThumbnail(b.bookTitle),
        }))
      );

      setBooks(booksWithThumbnails);
    } catch (err) {
      setError("Failed to fetch favorites. Please try again.");
      console.error("Error fetching favorites:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    setFilters((prev) => ({
      ...prev,
      title: params.get("title") || "",
      genreName: params.get("genreName") || "",
      authorName: params.get("authorName") || "",
    }));
  }, [location.search]);

  useEffect(() => {
    fetchFavorites();
  }, [filters]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const target = e.target as HTMLInputElement;
    const { name, value, type, checked } = target;
    setFilters((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleApplyFilters = (e: React.FormEvent) => {
    e.preventDefault();
    fetchFavorites();
  };

  const handleClearFilters = () => {
    setFilters({
      title: "",
      authorName: "",
      genreName: "",
      availableOnly: "",
    });
  };

  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement>) => {
    const target = e.target as HTMLImageElement;
    target.src = "/default-book-placeholder.png";
  };

  if (loading) return <div className="loading">Loading favorites...</div>;

  return (
    <div className="book-list-container">
      <header className="page-header">
        <h1>Favorites</h1>
        <p>Browse and search through your favorite books</p>
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
            <div className="filter-group">
              <label htmlFor="availableOnly">Available Only</label>
              <input
                type="checkbox"
                id="availableOnly"
                name="availableOnly"
                checked={!!filters.availableOnly}
                onChange={handleFilterChange}
              />
            </div>
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
        </form>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="books-section">
        <div className="books-header">
          <h2>Favorites ({books.length})</h2>
        </div>

        {books.length === 0 ? (
          <div className="no-books">
            <p>No favorites found matching your criteria.</p>
          </div>
        ) : (
          <div className="books-grid">
            {books.map((book) => (
              <div key={`fav-${book.bookId}`} className="book-card">
                <div className="book-image">
                  <img
                    src={book.thumbnail}
                    alt={book.bookTitle}
                    onError={handleImageError}
                    loading="lazy"
                  />
                </div>
                <div className="book-content">
                  <div className="book-info">
                    <h3 className="book-title">
                      <Link to={`/books/${book.bookId}`}>{book.bookTitle}</Link>
                    </h3>
                    <p className="book-author">by {book.authorName}</p>
                    <p className="book-genre">{book.genreName}</p>
                    <div className="book-quantity">
                      <span
                        className={`quantity-badge ${
                          !book.isAvailable ? "out-of-stock" : ""
                        }`}
                      >
                        {book.isAvailable ? "Available" : "Out of Stock"}
                      </span>
                    </div>
                  </div>
                  <div className="book-actions">
                    <Link
                      to={`/books/${book.bookId}`}
                      className="btn btn-outline"
                    >
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

export default FavoritesPage;
