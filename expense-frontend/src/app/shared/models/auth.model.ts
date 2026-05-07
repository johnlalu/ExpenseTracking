/**
 * Authentication response with tokens.
 */
export interface AuthResponse {
  accessToken?: string;
  refreshToken?: string;
  email?: string;
  expiresIn: number;
}

/**
 * Login request payload.
 */
export interface LoginRequest {
  email?: string;
  password?: string;
}

/**
 * Register request payload.
 */
export interface RegisterRequest {
  email?: string;
  password?: string;
  confirmPassword?: string;
  fullName?: string;
}

/**
 * Decoded JWT payload.
 */
export interface JwtPayload {
  sub: string;
  email: string;
  iat: number;
  exp: number;
}
