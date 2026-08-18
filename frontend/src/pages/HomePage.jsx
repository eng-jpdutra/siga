import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";

// Placeholder da página inicial — entra o dashboard/listagem quando o
// módulo de inventário de TI for implementado. O Paper já usa
// theme.palette.background.paper (branco) sobre o fundo creme do layout.
export default function HomePage() {
  return (
    <Paper sx={{ p: 3, maxWidth: 480 }}>
      <Typography variant="body1" color="text.primary">
        Bem-vindo ao SIGA. As telas de inventário ainda serão implementadas.
      </Typography>
    </Paper>
  );
}
