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
import Chip from "@mui/material/Chip";
import HowToVoteOutlinedIcon from "@mui/icons-material/HowToVoteOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import {
  listarVereadores,
  criarVereador,
  atualizarVereador,
  desativarVereador,
  ativarVereador,
} from "../api/vereadores";
import SeletorLocal from "../components/SeletorLocal";
import { usePodeEscrever } from "../auth/AuthContext";

function DialogVereador({ aberto, onFechar, onSalvar, salvando, erro, vereadorEditando }) {
  const ehEdicao = !!vereadorEditando;

  const [nome, setNome] = useState(vereadorEditando?.nome ?? "");
  const [partido, setPartido] = useState(vereadorEditando?.partido ?? "");
  const [contato, setContato] = useState(vereadorEditando?.contato ?? "");
  const [localId, setLocalId] = useState(vereadorEditando?.localId ?? null);

  const handleSalvar = () => {
    onSalvar({
      nome,
      partido: partido || null,
      contato: contato || null,
      localId,
    });
  };

  const valido = nome.trim().length > 0;

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar vereador" : "Novo vereador"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}

          <TextField label="Nome" value={nome} onChange={(e) => setNome(e.target.value)} autoFocus required />

          <Stack direction="row" spacing={2}>
            <TextField
              label="Partido"
              value={partido}
              onChange={(e) => setPartido(e.target.value)}
              sx={{ flex: 1 }}
            />
            <TextField
              label="Contato"
              value={contato}
              onChange={(e) => setContato(e.target.value)}
              sx={{ flex: 1 }}
            />
          </Stack>
          <SeletorLocal value={localId} onChange={setLocalId} label="Gabinete" fullWidth />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Cancelar</Button>
        <Button variant="contained" onClick={handleSalvar} disabled={!valido || salvando}>
          Salvar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function VereadoresPage() {
  const queryClient = useQueryClient();
  const podeEscrever = usePodeEscrever();

  const [nome, setNome] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogVereador, setDialogVereador] = useState(null); // null | "novo" | vereador
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["vereadores", { nome, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarVereadores({ nome, page: paginationModel.page + 1, pageSize: paginationModel.pageSize }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["vereadores"] });
    setDialogVereador(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarVereador,
    onSuccess: () => invalidarEFechar("Vereador criado com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarVereador(id, dados),
    onSuccess: () => invalidarEFechar("Vereador atualizado."),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, ativar }) => (ativar ? ativarVereador(id) : desativarVereador(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vereadores"] });
      setMensagem({ tipo: "success", texto: "Status atualizado." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const colunas = [
    { field: "nome", headerName: "Nome", flex: 1 },
    { field: "partido", headerName: "Partido", flex: 1 },
    { field: "localNome", headerName: "Gabinete", flex: 1 },
    { field: "contato", headerName: "Contato", flex: 1 },
    {
      field: "ativo",
      headerName: "Status",
      width: 110,
      renderCell: (params) => (
        <Chip
          size="small"
          label={params.value ? "Ativo" : "Inativo"}
          color={params.value ? "success" : "default"}
          variant="outlined"
        />
      ),
    },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 100,
      renderCell: (params) =>
        podeEscrever && (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="Editar">
              <IconButton size="small" onClick={() => setDialogVereador(params.row)}>
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={params.row.ativo ? "Desativar" : "Ativar"}>
              <IconButton
                size="small"
                onClick={() => statusMutation.mutate({ id: params.row.id, ativar: !params.row.ativo })}
              >
                {params.row.ativo ? (
                  <BlockOutlinedIcon fontSize="small" />
                ) : (
                  <CheckCircleOutlinedIcon fontSize="small" />
                )}
              </IconButton>
            </Tooltip>
          </Stack>
        ),
    },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Vereadores
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <TextField
          label="Buscar por nome"
          size="small"
          value={nome}
          onChange={(e) => setNome(e.target.value)}
          sx={{ minWidth: 280 }}
        />
        <Box sx={{ flexGrow: 1 }} />
        {podeEscrever && (
          <Button
            variant="contained"
            color="secondary"
            startIcon={<HowToVoteOutlinedIcon />}
            onClick={() => setDialogVereador("novo")}
          >
            Novo vereador
          </Button>
        )}
      </Stack>

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

      {dialogVereador && (
        <DialogVereador
          aberto
          vereadorEditando={dialogVereador === "novo" ? null : dialogVereador}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogVereador(null)}
          onSalvar={(dados) =>
            dialogVereador === "novo"
              ? criarMutation.mutate(dados)
              : atualizarMutation.mutate({ id: dialogVereador.id, dados })
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
