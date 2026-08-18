import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import { alterarSenha } from "../api/auth";

export default function DialogAlterarMinhaSenha({ aberto, onFechar }) {
  const [senhaAtual, setSenhaAtual] = useState("");
  const [novaSenha, setNovaSenha] = useState("");

  const mutation = useMutation({
    mutationFn: () => alterarSenha(senhaAtual, novaSenha),
    onSuccess: () => {
      setSenhaAtual("");
      setNovaSenha("");
      onFechar();
    },
  });

  function handleFechar() {
    mutation.reset();
    setSenhaAtual("");
    setNovaSenha("");
    onFechar();
  }

  return (
    <Dialog open={aberto} onClose={handleFechar} fullWidth maxWidth="xs">
      <DialogTitle>Alterar minha senha</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {mutation.isError && <Alert severity="error">{mutation.error.message}</Alert>}
          <TextField
            label="Senha atual"
            type="password"
            value={senhaAtual}
            onChange={(e) => setSenhaAtual(e.target.value)}
            autoFocus
          />
          <TextField
            label="Nova senha"
            type="password"
            value={novaSenha}
            onChange={(e) => setNovaSenha(e.target.value)}
            helperText="Pelo menos 8 caracteres."
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleFechar}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={!senhaAtual || novaSenha.length < 8 || mutation.isPending}
        >
          Salvar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
