import axios, { AxiosError } from "axios";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api";

export const apiClient = axios.create({
  baseURL: API_BASE_URL
});

let authToken: string | null = null;

/** Sets (or clears) the bearer token attached to every subsequent request. */
export function setAuthToken(token: string | null) {
  authToken = token;
  if (token) {
    apiClient.defaults.headers.common.Authorization = `Bearer ${token}`;
  } else {
    delete apiClient.defaults.headers.common.Authorization;
  }
}

export function getAuthToken() {
  return authToken;
}

/** Extracts a human-readable message from a failed API call, falling back to a generic string. */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ error?: string; title?: string; errors?: string[] }>;
    const data = axiosError.response?.data;
    if (data?.error) return data.error;
    if (data?.errors?.length) return data.errors.join(" ");
    if (data?.title) return data.title;
  }
  return "Something went wrong. Please try again.";
}
