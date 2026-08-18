import { apiFetch } from "./client";

export function listarResponsaveis({ nome, status, localId, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (nome) params.set("nome", nome);
  if (status) params.set("status", status);
  if (localId) params.set("localId", String(localId));

  return apiFetch(`/api/responsaveis?${params.toString()}`);
}

export function criarResponsavel(dados) {
  return apiFetch("/api/responsaveis", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarResponsavel(id, dados) {
  return apiFetch(`/api/responsaveis/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function desativarResponsavel(id) {
  return apiFetch(`/api/responsaveis/${id}/desativar`, { method: "POST" });
}

export function ativarResponsavel(id) {
  return apiFetch(`/api/responsaveis/${id}/ativar`, { method: "POST" });
}
