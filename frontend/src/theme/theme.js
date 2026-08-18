import { createTheme } from "@mui/material/styles";

// Paleta institucional da Câmara (ver "Identidade visual" no CLAUDE.md).
// Verde e âmbar são a identidade e não mudam entre os modos — só o fundo,
// as superfícies e o texto do corpo se adaptam ao claro/escuro.
const paletaInstitucional = {
  verde: "#17352E",
  ambar: "#C4862E",
  creme: "#ECEAE2",
  branco: "#FFFFFF",
  textoSobreEscuroPrimario: "#F3F1EA",
  textoSobreEscuroSecundario: "#7E9C8D",
  textoSobreClaro: "#1C2A25",
  // Fundo escuro é um verde bem apagado — não preto puro — pra manter a
  // mesma família de cor do resto da identidade em vez de um cinza genérico.
  fundoEscuroDefault: "#10201B",
  fundoEscuroSuperficie: "#17251F",
};

const paletasPorModo = {
  light: {
    mode: "light",
    background: {
      default: paletaInstitucional.creme,
      paper: paletaInstitucional.branco,
    },
    text: {
      primary: paletaInstitucional.textoSobreClaro,
      secondary: paletaInstitucional.textoSobreEscuroSecundario,
    },
  },
  dark: {
    mode: "dark",
    background: {
      default: paletaInstitucional.fundoEscuroDefault,
      paper: paletaInstitucional.fundoEscuroSuperficie,
    },
    text: {
      primary: paletaInstitucional.textoSobreEscuroPrimario,
      secondary: paletaInstitucional.textoSobreEscuroSecundario,
    },
  },
};

export function criarTema(modo) {
  return createTheme({
    palette: {
      ...paletasPorModo[modo],
      primary: {
        main: paletaInstitucional.verde,
        contrastText: paletaInstitucional.textoSobreEscuroPrimario,
      },
      secondary: {
        main: paletaInstitucional.ambar,
        contrastText: paletaInstitucional.textoSobreClaro,
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
}
