import { apiFetch, obterToken } from "./client";

const BASE_URL = import.meta.env.VITE_API_URL;

export function listarNotasFiscais({ numero, page, pageSize }) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (numero) params.set("numero", numero);

  return apiFetch(`/api/notas-fiscais?${params.toString()}`);
}

function paraFormData(dados) {
  const formData = new FormData();
  formData.append("numero", dados.numero);
  if (dados.fornecedor) formData.append("fornecedor", dados.fornecedor);
  if (dados.dataEmissao) formData.append("dataEmissao", dados.dataEmissao);
  if (dados.valor !== null && dados.valor !== "") formData.append("valor", dados.valor);
  if (dados.observacao) formData.append("observacao", dados.observacao);
  if (dados.arquivo) formData.append("arquivo", dados.arquivo);
  return formData;
}

export function criarNotaFiscal(dados) {
  return apiFetch("/api/notas-fiscais", {
    method: "POST",
    body: paraFormData(dados),
  });
}

export function atualizarNotaFiscal(id, dados) {
  return apiFetch(`/api/notas-fiscais/${id}`, {
    method: "PUT",
    body: paraFormData(dados),
  });
}

export function removerNotaFiscal(id) {
  return apiFetch(`/api/notas-fiscais/${id}`, { method: "DELETE" });
}

// Baixa o arquivo direto no navegador. Não passa pelo apiFetch porque a
// resposta é binária, não JSON — aqui a gente monta o fetch na mão, com o
// mesmo token, e dispara o download via um link temporário.
export async function baixarArquivoNotaFiscal(id, nomeSugerido) {
  const token = obterToken();
  const response = await fetch(`${BASE_URL}/api/notas-fiscais/${id}/arquivo`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) throw new Error("Não foi possível baixar o arquivo.");

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = nomeSugerido ?? "nota-fiscal";
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
