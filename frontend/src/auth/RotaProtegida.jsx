import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

// Guarda de rota do lado do React (a proteção de verdade é o 401 da API —
// isso aqui só evita mostrar a tela e já mandar embora antes da chamada falhar).
export default function RotaProtegida() {
  const { autenticado } = useAuth();
  const location = useLocation();

  if (!autenticado) {
    return <Navigate to="/login" replace state={{ de: location.pathname }} />;
  }

  return <Outlet />;
}
