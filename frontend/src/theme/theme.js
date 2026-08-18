import { createTheme } from "@mui/material/styles";

// Paleta institucional da Câmara (ver "Identidade visual" no CLAUDE.md).
// Fica centralizada aqui: os componentes devem ler `theme.palette.*`,
// nunca hexadecimais soltos — assim a paleta muda num único lugar.
const paletaInstitucional = {
  verde: "#17352E",
  ambar: "#C4862E",
  creme: "#ECEAE2",
  branco: "#FFFFFF",
  textoSobreEscuroPrimario: "#F3F1EA",
  textoSobreEscuroSecundario: "#7E9C8D",
  textoSobreClaro: "#1C2A25",
};

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: paletaInstitucional.verde,
      contrastText: paletaInstitucional.textoSobreEscuroPrimario,
    },
    secondary: {
      main: paletaInstitucional.ambar,
      contrastText: paletaInstitucional.textoSobreClaro,
    },
    background: {
      default: paletaInstitucional.creme,
      paper: paletaInstitucional.branco,
    },
    text: {
      primary: paletaInstitucional.textoSobreClaro,
      secondary: paletaInstitucional.textoSobreEscuroSecundario,
    },
  },
  typography: {
    // Sentence case em toda parte — nada de caixa-alta decorativa em botões/títulos.
    button: {
      textTransform: "none",
    },
  },
  components: {
    MuiAppBar: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          backgroundColor: t.palette.primary.main,
          color: t.palette.primary.contrastText,
        }),
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: "none",
        },
      },
    },
  },
});
