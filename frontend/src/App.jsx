import { Routes, Route } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import RotaProtegida from "./auth/RotaProtegida";
import LoginPage from "./pages/LoginPage";
import HomePage from "./pages/HomePage";
import LocaisPage from "./pages/LocaisPage";
import ResponsaveisPage from "./pages/ResponsaveisPage";
import UsuariosPage from "./pages/UsuariosPage";
import NotasFiscaisPage from "./pages/NotasFiscaisPage";
import LicencasPage from "./pages/LicencasPage";
import EquipamentosPage from "./pages/EquipamentosPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<RotaProtegida />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/locais" element={<LocaisPage />} />
          <Route path="/responsaveis" element={<ResponsaveisPage />} />
          <Route path="/usuarios" element={<UsuariosPage />} />
          <Route path="/notas-fiscais" element={<NotasFiscaisPage />} />
          <Route path="/licencas" element={<LicencasPage />} />
          <Route path="/equipamentos" element={<EquipamentosPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
