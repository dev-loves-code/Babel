import axios from "axios";
import type { BookQueryObject, GetBookDto } from "../types/book.types";

const API_BASE_URL = "http://localhost:5286/api";

const bookApi = axios.create({
  baseURL: `${API_BASE_URL}/book`,
  headers: {
    "Content-Type": "application/json",
  },
});

export const bookService = {
  getAllBooks: async (queryObject?: BookQueryObject): Promise<GetBookDto[]> => {
    try {
      const params = new URLSearchParams();

      if (queryObject?.Title) params.append("Title", queryObject.Title);
      if (queryObject?.GenreName)
        params.append("GenreName", queryObject.GenreName);
      if (queryObject?.AuthorName)
        params.append("AuthorName", queryObject.AuthorName);
      if (queryObject?.MinQuantity !== undefined)
        params.append("MinQuantity", queryObject.MinQuantity.toString());
      if (queryObject?.MaxQuantity !== undefined)
        params.append("MaxQuantity", queryObject.MaxQuantity.toString());

      const response = await bookApi.get(`?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error("Error fetching books:", error);
      throw error;
    }
  },

  getBookById: async (id: number): Promise<GetBookDto> => {
    try {
      const response = await bookApi.get(`/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching book with id ${id}:`, error);
      throw error;
    }
  },
};
