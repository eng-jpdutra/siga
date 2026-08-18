import { apiFetch } from "./client";

export function listarLicencas({ equipamentoId, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (equipamentoId) params.set("equipamentoId", String(equipamentoId));

  return apiFetch(`/api/licencas?${params.toString()}`);
}

export function criarLicenca(dados) {
  return apiFetch("/api/licencas", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarLicenca(id, dados) {
  return apiFetch(`/api/licencas/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function removerLicenca(id) {
  return apiFetch(`/api/licencas/${id}`, { method: "DELETE" });
}
