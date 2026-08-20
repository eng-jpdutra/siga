import { useState } from "react";
import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from "@tanstack/react-query";
import { DataGrid } from "@mui/x-data-grid";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import AddIcon from "@mui/icons-material/Add";
import Autocomplete from "@mui/material/Autocomplete";
import { listarLocais, criarLocal, atualizarLocal, removerLocal, TIPOS_DE_LOCAL_SUGERIDOS } from "../api/locais";
import { usePodeEscrever } from "../auth/AuthContext";

// Barra de filtro fica fora do DataGrid: filtragem é sempre feita no
// servidor (page/pageSize/nome viram query string), nunca no cliente.
function BarraDeFiltro({ nome, onNomeChange, onNovoLocal, podeEscrever }) {
  return (
    <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
      <TextField
        label="Buscar por nome"
        size="small"
        value={nome}
        onChange={(e) => onNomeChange(e.target.value)}
        sx={{ minWidth: 280 }}
      />
      <Box sx={{ flexGrow: 1 }} />
      {podeEscrever && (
        <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={onNovoLocal}>
          Novo local
        </Button>
      )}
    </Stack>
  );
}

function DialogLocal({ aberto, onFechar, onSalvar, salvando, erro, localEditando }) {
  const ehEdicao = !!localEditando;

  const [nome, setNome] = useState(localEditando?.nome ?? "");
  const [descricao, setDescricao] = useState(localEditando?.descricao ?? "");
  const [tipo, setTipo] = useState(localEditando?.tipo ?? "");

  const handleSalvar = () => {
    onSalvar({ nome, descricao: descricao || null, tipo: tipo || null }, () => {
      setNome("");
      setDescricao("");
      setTipo("");
    });
  };

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar local" : "Novo local"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}
          <TextField
            label="Nome"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            autoFocus
            required
          />
          <Autocomplete
            freeSolo
            options={TIPOS_DE_LOCAL_SUGERIDOS}
            value={tipo}
            onInputChange={(_, valor) => setTipo(valor)}
            renderInput={(params) => <TextField {...params} label="Tipo" placeholder="Gabinete, Almoxarifado..." />}
          />
          <TextField
            label="Descrição"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            multiline
            minRows={2}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Cancelar</Button>
        <Button variant="contained" onClick={handleSalvar} disabled={!nome.trim() || salvando}>
          Salvar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function LocaisPage() {
  const queryClient = useQueryClient();
  const podeEscrever = usePodeEscrever();

  const [nome, setNome] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogLocal, setDialogLocal] = useState(null); // null | "novo" | local
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["locais", { nome, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarLocais({
        nome,
        page: paginationModel.page + 1, // API é 1-based; DataGrid é 0-based.
        pageSize: paginationModel.pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["locais"] });
    setDialogLocal(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarLocal,
    onSuccess: () => invalidarEFechar("Local criado com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarLocal(id, dados),
    onSuccess: () => invalidarEFechar("Local atualizado."),
  });

  const removerMutation = useMutation({
    mutationFn: removerLocal,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["locais"] });
      setMensagem({ tipo: "success", texto: "Local removido." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const colunas = [
    { field: "nome", headerName: "Nome", flex: 1 },
    { field: "tipo", headerName: "Tipo", flex: 1 },
    { field: "descricao", headerName: "Descrição", flex: 2 },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 100,
      renderCell: (params) =>
        podeEscrever && (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="Editar">
              <IconButton size="small" aria-label="editar" onClick={() => setDialogLocal(params.row)}>
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Remover">
              <IconButton
                size="small"
                aria-label="remover"
                onClick={() => removerMutation.mutate(params.row.id)}
              >
                <DeleteOutlineIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
    },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Locais
      </Typography>

      <BarraDeFiltro
        nome={nome}
        onNomeChange={setNome}
        onNovoLocal={() => setDialogLocal("novo")}
        podeEscrever={podeEscrever}
      />

      {isError && <Alert severity="error" sx={{ mb: 2 }}>{error.message}</Alert>}

      <Box sx={{ bgcolor: "background.paper" }}>
        <DataGrid
          autoHeight
          rows={data?.items ?? []}
          rowCount={data?.totalCount ?? 0}
          columns={colunas}
          loading={isLoading}
          paginationMode="server"
          disableColumnFilter
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[20, 50, 100]}
          disableRowSelectionOnClick
          sx={{
            "& .MuiDataGrid-cell": { display: "flex", alignItems: "center" },
            "& .MuiDataGrid-columnHeaderTitleContainer": { alignItems: "center" },
          }}
        />
      </Box>

      {dialogLocal && (
        <DialogLocal
          aberto
          localEditando={dialogLocal === "novo" ? null : dialogLocal}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogLocal(null)}
          onSalvar={(dados) =>
            dialogLocal === "novo"
              ? criarMutation.mutate(dados)
              : atualizarMutation.mutate({ id: dialogLocal.id, dados })
          }
        />
      )}

      <Snackbar
        open={!!mensagem}
        autoHideDuration={4000}
        onClose={() => setMensagem(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        {mensagem && <Alert severity={mensagem.tipo}>{mensagem.texto}</Alert>}
      </Snackbar>
    </Box>
  );
}
