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
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import AddIcon from "@mui/icons-material/Add";
import {
  listarNotasFiscais,
  criarNotaFiscal,
  atualizarNotaFiscal,
  removerNotaFiscal,
  baixarArquivoNotaFiscal,
} from "../api/notasFiscais";
import { usePodeEscrever } from "../auth/AuthContext";

const formatoMoeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const formatoData = new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" });

function DialogNotaFiscal({ aberto, onFechar, onSalvar, salvando, erro, notaEditando }) {
  const ehEdicao = !!notaEditando;

  const [numero, setNumero] = useState(notaEditando?.numero ?? "");
  const [fornecedor, setFornecedor] = useState(notaEditando?.fornecedor ?? "");
  const [dataEmissao, setDataEmissao] = useState(notaEditando?.dataEmissao ?? "");
  const [valor, setValor] = useState(notaEditando?.valor ?? "");
  const [observacao, setObservacao] = useState(notaEditando?.observacao ?? "");
  const [arquivo, setArquivo] = useState(null);

  const handleSalvar = () => {
    onSalvar({ numero, fornecedor, dataEmissao, valor, observacao, arquivo });
  };

  const valido = numero.trim().length > 0;

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar nota fiscal" : "Nova nota fiscal"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}

          <TextField label="Número" value={numero} onChange={(e) => setNumero(e.target.value)} autoFocus required />
          <TextField label="Fornecedor" value={fornecedor} onChange={(e) => setFornecedor(e.target.value)} />

          <Stack direction="row" spacing={2}>
            <TextField
              label="Data de emissão"
              type="date"
              value={dataEmissao}
              onChange={(e) => setDataEmissao(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              fullWidth
            />
            <TextField
              label="Valor (R$)"
              type="number"
              inputProps={{ step: "0.01", min: "0" }}
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              fullWidth
            />
          </Stack>

          <TextField
            label="Observação"
            value={observacao}
            onChange={(e) => setObservacao(e.target.value)}
            multiline
            minRows={2}
          />

          <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
            <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
              Anexar arquivo
              <input
                type="file"
                hidden
                accept=".pdf,.jpg,.jpeg,.png"
                onChange={(e) => setArquivo(e.target.files?.[0] ?? null)}
              />
            </Button>
            <Typography variant="body2" color="text.secondary">
              {arquivo?.name ??
                (ehEdicao && notaEditando.temArquivo ? "Já tem um arquivo anexado (mantido se não trocar)." : "Nenhum arquivo selecionado.")}
            </Typography>
          </Stack>
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

export default function NotasFiscaisPage() {
  const queryClient = useQueryClient();
  const podeEscrever = usePodeEscrever();

  const [numero, setNumero] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogNota, setDialogNota] = useState(null); // null | "novo" | nota
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["notas-fiscais", { numero, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarNotasFiscais({ numero, page: paginationModel.page + 1, pageSize: paginationModel.pageSize }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["notas-fiscais"] });
    setDialogNota(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarNotaFiscal,
    onSuccess: () => invalidarEFechar("Nota fiscal criada com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarNotaFiscal(id, dados),
    onSuccess: () => invalidarEFechar("Nota fiscal atualizada."),
  });

  const removerMutation = useMutation({
    mutationFn: removerNotaFiscal,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notas-fiscais"] });
      setMensagem({ tipo: "success", texto: "Nota fiscal removida." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  async function handleBaixar(nota) {
    try {
      await baixarArquivoNotaFiscal(nota.id, `nota-fiscal-${nota.numero}`);
    } catch (err) {
      setMensagem({ tipo: "error", texto: err.message });
    }
  }

  const colunas = [
    { field: "numero", headerName: "Número", width: 140 },
    { field: "fornecedor", headerName: "Fornecedor", flex: 1 },
    {
      field: "dataEmissao",
      headerName: "Emissão",
      width: 120,
      valueFormatter: (value) => (value ? formatoData.format(new Date(`${value}T00:00:00Z`)) : ""),
    },
    {
      field: "valor",
      headerName: "Valor",
      width: 130,
      valueFormatter: (value) => (value != null ? formatoMoeda.format(value) : ""),
    },
    {
      field: "temArquivo",
      headerName: "Arquivo",
      width: 110,
      renderCell: (params) =>
        params.value ? <Chip size="small" label="Anexado" color="success" variant="outlined" /> : null,
    },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 140,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          {params.row.temArquivo && (
            <Tooltip title="Baixar arquivo">
              <IconButton size="small" onClick={() => handleBaixar(params.row)}>
                <DownloadOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
          {podeEscrever && (
            <>
              <Tooltip title="Editar">
                <IconButton size="small" onClick={() => setDialogNota(params.row)}>
                  <EditOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              <Tooltip title="Remover">
                <IconButton size="small" onClick={() => removerMutation.mutate(params.row.id)}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </>
          )}
        </Stack>
      ),
    },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Notas fiscais
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <TextField
          label="Buscar por número ou fornecedor"
          size="small"
          value={numero}
          onChange={(e) => setNumero(e.target.value)}
          sx={{ minWidth: 280 }}
        />
        <Box sx={{ flexGrow: 1 }} />
        {podeEscrever && (
          <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={() => setDialogNota("novo")}>
            Nova nota fiscal
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

      {dialogNota && (
        <DialogNotaFiscal
          aberto
          notaEditando={dialogNota === "novo" ? null : dialogNota}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogNota(null)}
          onSalvar={(dados) =>
            dialogNota === "novo"
              ? criarMutation.mutate(dados)
              : atualizarMutation.mutate({ id: dialogNota.id, dados })
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
