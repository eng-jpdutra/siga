import { apiFetch } from "./client";

export function listarConfiguracoes(equipamentoId) {
  return apiFetch(`/api/equipamentos/${equipamentoId}/configuracoes`);
}

export function criarConfiguracao(equipamentoId, dados) {
  return apiFetch(`/api/equipamentos/${equipamentoId}/configuracoes`, {
    method: "POST",
    body: JSON.stringify(dados),
  });
}

export function atualizarConfiguracao(equipamentoId, configuracaoId, dados) {
  return apiFetch(`/api/equipamentos/${equipamentoId}/configuracoes/${configuracaoId}`, {
    method: "PUT",
    body: JSON.stringify(dados),
  });
}

export function removerConfiguracao(equipamentoId, configuracaoId) {
  return apiFetch(`/api/equipamentos/${equipamentoId}/configuracoes/${configuracaoId}`, {
    method: "DELETE",
  });
}
