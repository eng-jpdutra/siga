import { apiFetch } from "./client";

export function listarUsuarios({ nome, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (nome) params.set("nome", nome);

  return apiFetch(`/api/usuarios?${params.toString()}`);
}

export function criarUsuario(dados) {
  return apiFetch("/api/usuarios", {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarUsuario(id, dados) {
  return apiFetch(`/api/usuarios/${id}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function desativarUsuario(id) {
  return apiFetch(`/api/usuarios/${id}/desativar`, { method: "POST" });
}

export function ativarUsuario(id) {
  return apiFetch(`/api/usuarios/${id}/ativar`, { method: "POST" });
}

export function redefinirSenha(id, novaSenha) {
  return apiFetch(`/api/usuarios/${id}/redefinir-senha`, {
    method: "POST",
    body: JSON.stringify({ novaSenha }),
  });
}
