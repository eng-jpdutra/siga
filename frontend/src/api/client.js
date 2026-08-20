const BASE_URL = import.meta.env.VITE_API_URL;
const URL_PORTAL = import.meta.env.VITE_PORTAL_URL;
const CHAVE_TOKEN = "siga_token";

export function salvarToken(token) {
  localStorage.setItem(CHAVE_TOKEN, token);
}

export function limparToken() {
  localStorage.removeItem(CHAVE_TOKEN);
}

export function obterToken() {
  return localStorage.getItem(CHAVE_TOKEN);
}

// Wrapper fino sobre fetch: monta a URL a partir do VITE_API_URL, anexa o
// token (se tiver um salvo), já manda/lê JSON e transforma respostas de
// erro (400/404/409) em exceções com a mensagem que a API devolveu.
export async function apiFetch(path, options = {}) {
  const token = obterToken();

  // Upload de arquivo manda FormData — nesse caso o navegador precisa
  // definir o Content-Type sozinho (com o boundary do multipart);
  // se a gente forçar "application/json" aqui, o backend não consegue
  // ler o corpo da requisição.
  const ehFormData = options.body instanceof FormData;

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      ...(!ehFormData && { "Content-Type": "application/json" }),
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    },
  });

  // Token ausente/expirado/inválido: o SIGA não tem login próprio, então
  // não tem pra onde mandar a pessoa a não ser de volta pro Portal.
  if (response.status === 401) {
    localStorage.removeItem(CHAVE_TOKEN);
    window.location.href = URL_PORTAL;
    throw new Error("Sessão expirada. Faça login novamente.");
  }

  if (!response.ok) {
    const mensagem = await extrairMensagemDeErro(response);
    throw new Error(mensagem);
  }

  if (response.status === 204) return null;
  return response.json();
}

async function extrairMensagemDeErro(response) {
  try {
    const corpo = await response.json();
    if (typeof corpo === "string") return corpo;
    if (corpo?.errors) {
      return Object.values(corpo.errors).flat().join(" ");
    }
    if (corpo?.title) return corpo.title;
  } catch {
    // corpo não era JSON — usa a mensagem genérica abaixo.
  }
  return `Erro ${response.status} ao comunicar com a API.`;
}
