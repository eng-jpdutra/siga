import { apiFetch } from "./client";

export function obterDashboard() {
  return apiFetch("/api/dashboard");
}
