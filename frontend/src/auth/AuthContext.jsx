import { createContext, useContext, useState } from "react";
import { login as loginApi } from "../api/auth";
import { salvarToken, limparToken, obterToken } from "../api/client";

const CHAVE_USUARIO = "siga_usuario";

const AuthContext = createContext(null);

function usuarioSalvo() {
  const bruto = localStorage.getItem(CHAVE_USUARIO);
  return bruto ? JSON.parse(bruto) : null;
}

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(usuarioSalvo);

  async function entrar(nomeUsuario, senha) {
    const resposta = await loginApi(nomeUsuario, senha);
    const dadosUsuario = { nome: resposta.nome, papeis: resposta.papeis };

    salvarToken(resposta.token);
    localStorage.setItem(CHAVE_USUARIO, JSON.stringify(dadosUsuario));
    setUsuario(dadosUsuario);
  }

  function sair() {
    limparToken();
    localStorage.removeItem(CHAVE_USUARIO);
    setUsuario(null);
  }

  const autenticado = !!usuario && !!obterToken();

  return (
    <AuthContext.Provider value={{ usuario, autenticado, entrar, sair }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const contexto = useContext(AuthContext);
  if (!contexto) throw new Error("useAuth precisa estar dentro de um AuthProvider.");
  return contexto;
}
