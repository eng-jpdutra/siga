import { Routes, Route } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import RotaProtegida from "./auth/RotaProtegida";
import SsoPage from "./pages/SsoPage";
import HomePage from "./pages/HomePage";
import LocaisPage from "./pages/LocaisPage";
import NotasFiscaisPage from "./pages/NotasFiscaisPage";
import LicencasPage from "./pages/LicencasPage";
import EquipamentosPage from "./pages/EquipamentosPage";
import VereadoresPage from "./pages/VereadoresPage";

export default function App() {
  return (
    <Routes>
      <Route path="/sso" element={<SsoPage />} />

      <Route element={<RotaProtegida />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/locais" element={<LocaisPage />} />
          <Route path="/notas-fiscais" element={<NotasFiscaisPage />} />
          <Route path="/licencas" element={<LicencasPage />} />
          <Route path="/equipamentos" element={<EquipamentosPage />} />
          <Route path="/vereadores" element={<VereadoresPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
