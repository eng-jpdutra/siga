import { apiFetch } from "./client";

export function listarEquipamentos({ termo, tipo, status, localId, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (termo) params.set("termo", termo);
  if (tipo) params.set("tipo", tipo);
  if (status) params.set("status", status);
  if (localId) params.set("localId", String(localId));

  return apiFetch(`/api/equipamentos?${params.toString()}`);
}

export function obterEquipamento(id) {
  return apiFetch(`/api/equipamentos/${id}`);
}

export function criarEquipamento(dados) {
  return apiFetch("/api/equipamentos", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarEquipamento(id, dados) {
  return apiFetch(`/api/equipamentos/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function baixarEquipamento(id) {
  return apiFetch(`/api/equipamentos/${id}/baixar`, { method: "POST" });
}

export function reativarEquipamento(id) {
  return apiFetch(`/api/equipamentos/${id}/reativar`, { method: "POST" });
}

export function listarHistorico(id) {
  return apiFetch(`/api/equipamentos/${id}/historico`);
}

export function criarHistorico(id, dados) {
  return apiFetch(`/api/equipamentos/${id}/historico`, {
    method: "POST",
    body: JSON.stringify(dados),
  });
}
