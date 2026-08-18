import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";
import { useAuth } from "../auth/AuthContext";

export default function LoginPage() {
  const { entrar } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [nomeUsuario, setNomeUsuario] = useState("");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState(null);
  const [carregando, setCarregando] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await entrar(nomeUsuario, senha);
      navigate(location.state?.de ?? "/", { replace: true });
    } catch {
      setErro("Usuário ou senha inválidos.");
    } finally {
      setCarregando(false);
    }
  }

  return (
    <Box
      sx={{
        minHeight: "100vh",
        bgcolor: "primary.main",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        p: 2,
      }}
    >
      <Paper component="form" onSubmit={handleSubmit} sx={{ p: 4, width: "100%", maxWidth: 380 }}>
        <Stack spacing={3}>
          <Box>
            <Typography variant="h5" component="h1">
              SIGA
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sistema Integrado de Gestão de Ativos
            </Typography>
          </Box>

          {erro && <Alert severity="error">{erro}</Alert>}

          <TextField
            label="Usuário"
            value={nomeUsuario}
            onChange={(e) => setNomeUsuario(e.target.value)}
            autoFocus
            required
          />
          <TextField
            label="Senha"
            type="password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />

          <Button type="submit" variant="contained" color="secondary" size="large" disabled={carregando}>
            Entrar
          </Button>
        </Stack>
      </Paper>
    </Box>
  );
}
