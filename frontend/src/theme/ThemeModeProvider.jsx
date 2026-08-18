import { createContext, useContext, useMemo, useState } from "react";
import { ThemeProvider } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import { criarTema } from "./theme";

const CHAVE_TEMA = "siga_tema";

const ThemeModeContext = createContext(null);

function modoSalvo() {
  const salvo = localStorage.getItem(CHAVE_TEMA);
  if (salvo === "light" || salvo === "dark") return salvo;
  // Sem preferência salva, respeita o que o sistema operacional já usa.
  return window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeModeProvider({ children }) {
  const [modo, setModo] = useState(modoSalvo);

  function alternarModo() {
    setModo((atual) => {
      const novo = atual === "light" ? "dark" : "light";
      localStorage.setItem(CHAVE_TEMA, novo);
      return novo;
    });
  }

  const tema = useMemo(() => criarTema(modo), [modo]);

  return (
    <ThemeModeContext.Provider value={{ modo, alternarModo }}>
      <ThemeProvider theme={tema}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </ThemeModeContext.Provider>
  );
}

export function useThemeMode() {
  const contexto = useContext(ThemeModeContext);
  if (!contexto) throw new Error("useThemeMode precisa estar dentro de um ThemeModeProvider.");
  return contexto;
}
