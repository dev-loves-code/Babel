export interface LoginDto {
  username: string;
  password: string;
}

export interface RegisterDto {
  username: string;
  email: string;
  password: string;
}

export interface UserDto {
  userName: string;
  email: string;
  token: string;
  isBlocked: boolean;
  role: string;
}
