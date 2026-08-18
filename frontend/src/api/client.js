const BASE_URL = import.meta.env.VITE_API_URL;
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
export async function apiFetch(path, { skipAuthRedirect = false, ...options } = {}) {
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

  // skipAuthRedirect: o próprio /api/auth/login também responde 401 quando a
  // senha está errada — isso não é "sessão expirada", é erro de formulário.
  if (response.status === 401 && !skipAuthRedirect) {
    // Token ausente/expirado/inválido: não tem como recuperar a sessão,
    // então já limpa e manda pro login em vez de deixar a tela travada.
    limparToken();
    if (window.location.pathname !== "/login") {
      window.location.href = "/login";
    }
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
