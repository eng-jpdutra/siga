import { Outlet } from "react-router-dom";
import Box from "@mui/material/Box";
import AppBar from "@mui/material/AppBar";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import Chip from "@mui/material/Chip";

// Layout base da aplicação. As cores vêm sempre de `theme.palette` (nunca de
// hexadecimais soltos) — ver identidade visual em src/theme/theme.js.
// Por enquanto só uma barra de topo fixa; menu lateral e navegação por
// módulo entram quando as telas existirem.
export default function AppLayout() {
  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        minHeight: "100vh",
        bgcolor: "background.default",
        color: "text.primary",
      }}
    >
      <AppBar position="static" elevation={0}>
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            SIGA — sistema integrado de gestão de ativos
          </Typography>
          {/* Exemplo de destaque em âmbar para o item de navegação ativo. */}
          <Chip
            label="Início"
            color="secondary"
            size="small"
            sx={{ fontWeight: 500 }}
          />
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flex: 1, p: 3 }}>
        <Outlet />
      </Box>
    </Box>
  );
}
