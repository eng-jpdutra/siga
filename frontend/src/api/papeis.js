import { apiFetch } from "./client";

export function listarPapeis() {
  return apiFetch("/api/papeis");
}
