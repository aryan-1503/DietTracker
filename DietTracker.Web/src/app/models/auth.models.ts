export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface OnboardingRequest {
  firstName: string;
  middleName?: string;
  lastName: string;
  heightCm: number;
  weightKg: number;
  goalWeightKg: number;
  targetDurationWeeks: number;
}

export interface UpdateProfileRequest {
  firstName: string;
  middleName?: string;
  lastName: string;
  heightCm?: number | null;
  weightKg?: number | null;
  goalWeightKg?: number | null;
  targetDurationWeeks?: number | null;
}

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  userId: number;
  expiresAt: string;
  isEmailVerified: boolean;
  onboardingCompleted: boolean;
}

export interface UserProfile {
  id: number;
  email: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  fullName: string;
  isEmailVerified: boolean;
  onboardingCompleted: boolean;
  heightCm?: number;
  weightKg?: number;
  goalWeightKg?: number;
  targetDurationWeeks?: number;
  createdAt: string;
}
