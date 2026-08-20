import { Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";

const URL_PORTAL = import.meta.env.VITE_PORTAL_URL;

// Guarda de rota do lado do React — a proteção de verdade é o 401 da API.
// Sem sessão local, não tem pra onde navegar dentro do próprio SIGA (não
// existe mais login aqui): manda direto pro Portal, que é quem autentica.
export default function RotaProtegida() {
  const { autenticado } = useAuth();

  if (!autenticado) {
    window.location.href = URL_PORTAL;
    return null;
  }

  return <Outlet />;
}
