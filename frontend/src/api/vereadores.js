import { apiFetch } from "./client";

export function listarVereadores({ nome, ativo, localId, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (nome) params.set("nome", nome);
  if (ativo !== undefined && ativo !== null) params.set("ativo", String(ativo));
  if (localId) params.set("localId", String(localId));

  return apiFetch(`/api/vereadores?${params.toString()}`);
}

export function criarVereador(dados) {
  return apiFetch("/api/vereadores", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarVereador(id, dados) {
  return apiFetch(`/api/vereadores/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function desativarVereador(id) {
  return apiFetch(`/api/vereadores/${id}/desativar`, { method: "POST" });
}

export function ativarVereador(id) {
  return apiFetch(`/api/vereadores/${id}/ativar`, { method: "POST" });
}
