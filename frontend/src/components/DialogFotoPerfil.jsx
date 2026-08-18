import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Stack from "@mui/material/Stack";
import Button from "@mui/material/Button";
import Avatar from "@mui/material/Avatar";
import Alert from "@mui/material/Alert";
import Typography from "@mui/material/Typography";
import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import { enviarMinhaFoto } from "../api/auth";

export default function DialogFotoPerfil({ aberto, onFechar, fotoAtualUrl }) {
  const queryClient = useQueryClient();
  const [arquivo, setArquivo] = useState(null);
  const [previaUrl, setPreviaUrl] = useState(null);

  const mutation = useMutation({
    mutationFn: () => enviarMinhaFoto(arquivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["minha-foto"] });
      handleFechar();
    },
  });

  function handleEscolherArquivo(e) {
    const arquivoEscolhido = e.target.files?.[0] ?? null;
    setArquivo(arquivoEscolhido);
    setPreviaUrl(arquivoEscolhido ? URL.createObjectURL(arquivoEscolhido) : null);
  }

  function handleFechar() {
    mutation.reset();
    setArquivo(null);
    setPreviaUrl(null);
    onFechar();
  }

  return (
    <Dialog open={aberto} onClose={handleFechar} fullWidth maxWidth="xs">
      <DialogTitle>Foto de perfil</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1, alignItems: "center" }}>
          {mutation.isError && <Alert severity="error" sx={{ width: "100%" }}>{mutation.error.message}</Alert>}

          <Avatar src={previaUrl ?? fotoAtualUrl ?? undefined} sx={{ width: 96, height: 96 }} />

          <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
            Escolher foto
            <input type="file" hidden accept=".jpg,.jpeg,.png" onChange={handleEscolherArquivo} />
          </Button>

          <Typography variant="caption" color="text.secondary">
            JPG ou PNG, até 5 MB.
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleFechar}>Cancelar</Button>
        <Button variant="contained" onClick={() => mutation.mutate()} disabled={!arquivo || mutation.isPending}>
          Salvar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
