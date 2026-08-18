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
import PersonAddAltOutlinedIcon from "@mui/icons-material/PersonAddAltOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import {
  listarResponsaveis,
  criarResponsavel,
  atualizarResponsavel,
  desativarResponsavel,
  ativarResponsavel,
} from "../api/responsaveis";
import SeletorLocal from "../components/SeletorLocal";

function DialogResponsavel({ aberto, onFechar, onSalvar, salvando, erro, responsavelEditando }) {
  const ehEdicao = !!responsavelEditando;

  const [nome, setNome] = useState(responsavelEditando?.nome ?? "");
  const [cargo, setCargo] = useState(responsavelEditando?.cargo ?? "");
  const [localId, setLocalId] = useState(responsavelEditando?.localId ?? null);
  const [contato, setContato] = useState(responsavelEditando?.contato ?? "");
  const [observacao, setObservacao] = useState(responsavelEditando?.observacao ?? "");

  const handleSalvar = () => {
    onSalvar({
      nome,
      cargo: cargo || null,
      localId,
      contato: contato || null,
      observacao: observacao || null,
    });
  };

  const valido = nome.trim().length > 0;

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar responsável" : "Novo responsável"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}

          <TextField label="Nome" value={nome} onChange={(e) => setNome(e.target.value)} autoFocus required />
          <TextField label="Cargo" value={cargo} onChange={(e) => setCargo(e.target.value)} />

          <Stack direction="row" spacing={2}>
            <Box sx={{ flex: 1 }}>
              <SeletorLocal value={localId} onChange={setLocalId} fullWidth />
            </Box>
            <TextField
              label="Contato"
              value={contato}
              onChange={(e) => setContato(e.target.value)}
              sx={{ flex: 1 }}
            />
          </Stack>

          <TextField
            label="Observação"
            value={observacao}
            onChange={(e) => setObservacao(e.target.value)}
            multiline
            minRows={2}
          />
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

export default function ResponsaveisPage() {
  const queryClient = useQueryClient();

  const [nome, setNome] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogResponsavel, setDialogResponsavel] = useState(null); // null | "novo" | responsavel
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["responsaveis", { nome, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarResponsaveis({ nome, page: paginationModel.page + 1, pageSize: paginationModel.pageSize }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["responsaveis"] });
    setDialogResponsavel(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarResponsavel,
    onSuccess: () => invalidarEFechar("Responsável criado com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarResponsavel(id, dados),
    onSuccess: () => invalidarEFechar("Responsável atualizado."),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, ativar }) => (ativar ? ativarResponsavel(id) : desativarResponsavel(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["responsaveis"] });
      setMensagem({ tipo: "success", texto: "Status atualizado." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const colunas = [
    { field: "nome", headerName: "Nome", flex: 1 },
    { field: "cargo", headerName: "Cargo", flex: 1 },
    { field: "localNome", headerName: "Local", flex: 1 },
    { field: "contato", headerName: "Contato", flex: 1 },
    {
      field: "status",
      headerName: "Status",
      width: 110,
      renderCell: (params) => (
        <Chip
          size="small"
          label={params.value === "Ativo" ? "Ativo" : "Inativo"}
          color={params.value === "Ativo" ? "success" : "default"}
          variant="outlined"
        />
      ),
    },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 100,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Editar">
            <IconButton size="small" onClick={() => setDialogResponsavel(params.row)}>
              <EditOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title={params.row.status === "Ativo" ? "Desativar" : "Ativar"}>
            <IconButton
              size="small"
              onClick={() => statusMutation.mutate({ id: params.row.id, ativar: params.row.status !== "Ativo" })}
            >
              {params.row.status === "Ativo" ? (
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
        Responsáveis
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
        <Button
          variant="contained"
          color="secondary"
          startIcon={<PersonAddAltOutlinedIcon />}
          onClick={() => setDialogResponsavel("novo")}
        >
          Novo responsável
        </Button>
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

      {dialogResponsavel && (
        <DialogResponsavel
          aberto
          responsavelEditando={dialogResponsavel === "novo" ? null : dialogResponsavel}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogResponsavel(null)}
          onSalvar={(dados) =>
            dialogResponsavel === "novo"
              ? criarMutation.mutate(dados)
              : atualizarMutation.mutate({ id: dialogResponsavel.id, dados })
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
