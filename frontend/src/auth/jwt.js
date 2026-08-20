// O SIGA não emite mais o próprio token — ele chega pronto do Portal (ver
// pages/SsoPage.jsx). Isso só lê os claims do JWT no navegador pra montar
// o objeto de usuário local; não valida assinatura (quem faz isso de
// verdade é o backend, em toda chamada de API).
const CLAIM_NOME = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const CLAIM_SISTEMA_PAPEL = "sistemaPapel";
const PREFIXO_SIGA = "SIGA:";

export function decodificarUsuarioDoToken(token) {
  const payload = JSON.parse(
    atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/"))
  );

  const sistemaPapelBruto = payload[CLAIM_SISTEMA_PAPEL];
  const sistemaPapeis = Array.isArray(sistemaPapelBruto)
    ? sistemaPapelBruto
    : sistemaPapelBruto
      ? [sistemaPapelBruto]
      : [];

  const papelNoSiga = sistemaPapeis
    .find((sp) => sp.startsWith(PREFIXO_SIGA))
    ?.slice(PREFIXO_SIGA.length);

  return {
    nome: payload[CLAIM_NOME],
    papeis: papelNoSiga ? [papelNoSiga] : [],
  };
}
