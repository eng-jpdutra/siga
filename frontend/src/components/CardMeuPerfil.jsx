import { useState } from "react";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Box from "@mui/material/Box";
import Avatar from "@mui/material/Avatar";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import VpnKeyOutlinedIcon from "@mui/icons-material/VpnKeyOutlined";
import PhotoCameraOutlinedIcon from "@mui/icons-material/PhotoCameraOutlined";
import AccountCircleOutlinedIcon from "@mui/icons-material/AccountCircleOutlined";
import { useAuth } from "../auth/AuthContext";
import { useFotoPerfil } from "../hooks/useFotoPerfil";
import DialogAlterarMinhaSenha from "./DialogAlterarMinhaSenha";
import DialogFotoPerfil from "./DialogFotoPerfil";

// Autoatendimento: trocar senha e trocar foto — "sair" continua no
// cabeçalho, ao lado do avatar (não faz parte do que se edita aqui).
export default function CardMeuPerfil() {
  const { usuario } = useAuth();
  const { data: fotoUrl } = useFotoPerfil();
  const [dialogSenhaAberto, setDialogSenhaAberto] = useState(false);
  const [dialogFotoAberto, setDialogFotoAberto] = useState(false);

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>Meu perfil</Typography>

      <Stack direction="row" spacing={3} sx={{ alignItems: "center", flexWrap: "wrap" }}>
        <Avatar src={fotoUrl ?? undefined} sx={{ width: 80, height: 80, bgcolor: "secondary.main" }}>
          <AccountCircleOutlinedIcon sx={{ fontSize: 52 }} />
        </Avatar>

        <Box sx={{ flexGrow: 1, minWidth: 200 }}>
          <Typography variant="subtitle1">{usuario?.nome}</Typography>
          <Stack direction="row" spacing={0.5} sx={{ mt: 0.5 }}>
            {usuario?.papeis?.map((p) => <Chip key={p} size="small" label={p} />)}
          </Stack>
        </Box>

        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
          <Button variant="outlined" startIcon={<PhotoCameraOutlinedIcon />} onClick={() => setDialogFotoAberto(true)}>
            Trocar foto
          </Button>
          <Button variant="outlined" startIcon={<VpnKeyOutlinedIcon />} onClick={() => setDialogSenhaAberto(true)}>
            Alterar senha
          </Button>
        </Stack>
      </Stack>

      <DialogAlterarMinhaSenha aberto={dialogSenhaAberto} onFechar={() => setDialogSenhaAberto(false)} />
      <DialogFotoPerfil aberto={dialogFotoAberto} onFechar={() => setDialogFotoAberto(false)} fotoAtualUrl={fotoUrl} />
    </Paper>
  );
}
