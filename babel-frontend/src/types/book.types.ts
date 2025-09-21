// types/book.types.ts

export interface BookQueryObject {
  Title?: string;
  GenreName?: string;
  AuthorName?: string;
  MinQuantity?: number;
  MaxQuantity?: number;
}

export interface GetBookDto {
  id: number;
  title: string;
  quantity: number;
  genreId: number;
  genreName: string;
  authorId: number;
  authorName: string;
}

export interface BookFilters {
  title: string;
  genreName: string;
  authorName: string;
  minQuantity: string;
  maxQuantity: string;
}
