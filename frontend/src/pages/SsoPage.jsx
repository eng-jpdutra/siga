import { useEffect, useRef } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";
import { useAuth } from "../auth/AuthContext";

const URL_PORTAL = import.meta.env.VITE_PORTAL_URL;

// Destino de quem clica no card do SIGA dentro do Portal — a URL chega com
// ?token=... (o mesmo JWT que o Portal já emitiu no login de lá). Só
// guarda o token e manda pra home; se não vier token nenhum (alguém abriu
// essa rota direto), volta pro Portal, que é onde o login de verdade mora.
export default function SsoPage() {
  const [searchParams] = useSearchParams();
  const { receberToken } = useAuth();
  const navigate = useNavigate();
  const jaProcessou = useRef(false);

  useEffect(() => {
    if (jaProcessou.current) return;
    jaProcessou.current = true;

    const token = searchParams.get("token");
    if (!token) {
      window.location.href = URL_PORTAL;
      return;
    }

    receberToken(token);
    navigate("/", { replace: true });
  }, [searchParams, receberToken, navigate]);

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <Stack spacing={2} sx={{ alignItems: "center" }}>
        <CircularProgress />
        <Typography variant="body2" color="text.secondary">Entrando...</Typography>
      </Stack>
    </Box>
  );
}
