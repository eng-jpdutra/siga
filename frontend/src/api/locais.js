import { apiFetch } from "./client";

export function listarLocais({ nome, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (nome) params.set("nome", nome);

  return apiFetch(`/api/locais?${params.toString()}`);
}

// Texto livre — não é uma lista fechada validada pelo backend, só ajuda a
// organizar (ex.: "Gabinete", "Almoxarifado", "Plenário", "Setor").
export const TIPOS_DE_LOCAL_SUGERIDOS = ["Gabinete", "Almoxarifado", "Plenário", "Setor"];

export function criarLocal(dados) {
  return apiFetch("/api/locais", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarLocal(id, dados) {
  return apiFetch(`/api/locais/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function removerLocal(id) {
  return apiFetch(`/api/locais/${id}`, { method: "DELETE" });
}
