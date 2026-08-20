import { createContext, useContext, useState } from "react";
import { salvarToken, limparToken, obterToken } from "../api/client";
import { decodificarUsuarioDoToken } from "./jwt";

const CHAVE_USUARIO = "siga_usuario";
const URL_PORTAL = import.meta.env.VITE_PORTAL_URL;

const AuthContext = createContext(null);

function usuarioSalvo() {
  const bruto = localStorage.getItem(CHAVE_USUARIO);
  return bruto ? JSON.parse(bruto) : null;
}

// O SIGA não tem mais login próprio — a identidade vem sempre de um token
// emitido pelo Portal (ver PORTAL/CLAUDE.md e pages/SsoPage.jsx). Aqui só
// guarda esse token e o que dá pra extrair dele, nada de senha/perfil.
export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(usuarioSalvo);

  function receberToken(token) {
    const dadosUsuario = decodificarUsuarioDoToken(token);
    salvarToken(token);
    localStorage.setItem(CHAVE_USUARIO, JSON.stringify(dadosUsuario));
    setUsuario(dadosUsuario);
  }

  function sair() {
    limparToken();
    localStorage.removeItem(CHAVE_USUARIO);
    setUsuario(null);
    window.location.href = URL_PORTAL;
  }

  const autenticado = !!usuario && !!obterToken();

  return (
    <AuthContext.Provider value={{ usuario, autenticado, receberToken, sair }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const contexto = useContext(AuthContext);
  if (!contexto) throw new Error("useAuth precisa estar dentro de um AuthProvider.");
  return contexto;
}

// Só existem dois papéis: Administrador (lê e escreve) e Consulta (só lê —
// ver CLAUDE.md). Usado nas telas de inventário pra esconder botões de
// criar/editar/excluir de quem não pode escrever — a API já bloqueia isso
// de qualquer forma, mas não faz sentido mostrar um botão que só dá erro.
export function usePodeEscrever() {
  const { usuario } = useAuth();
  return !!usuario?.papeis?.includes("Administrador");
}
