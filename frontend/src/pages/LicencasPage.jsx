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
import Autocomplete from "@mui/material/Autocomplete";
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
import MenuItem from "@mui/material/MenuItem";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import AddIcon from "@mui/icons-material/Add";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import VisibilityOffOutlinedIcon from "@mui/icons-material/VisibilityOffOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import { listarLicencas, criarLicenca, atualizarLicenca, removerLicenca } from "../api/licencas";
import { listarEquipamentos } from "../api/equipamentos";
import { useDebounce } from "../hooks/useDebounce";
import SeletorNotaFiscal from "../components/SeletorNotaFiscal";

const TIPOS_LICENCA = ["OEM", "Volume", "Retail"];

// Resultado da busca (`listarEquipamentos`) traz marca/modelo/patrimônio
// separados; quando o valor vem de uma licença já existente, só temos a
// descrição pronta (`equipamentoDescricao`) — cobre os dois formatos.
function descricaoEquipamento(eq) {
  if (!eq) return "";
  if (eq.descricao) return eq.descricao;
  return `${eq.marca} ${eq.modelo}${eq.patrimonio ? ` (${eq.patrimonio})` : ""}`;
}

function SeletorEquipamento({ value, onChange, disabled }) {
  const [texto, setTexto] = useState("");
  const termo = useDebounce(texto);

  const { data, isFetching } = useQuery({
    queryKey: ["equipamentos-busca", termo],
    queryFn: () => listarEquipamentos({ termo, page: 1, pageSize: 20 }),
  });

  const opcoes = data?.items ?? [];
  // Garante que o equipamento já selecionado apareça na lista mesmo que a
  // busca atual não o traga de volta (ex.: acabou de abrir o diálogo de edição).
  const opcoesComSelecionado = value && !opcoes.some((o) => o.id === value.id) ? [value, ...opcoes] : opcoes;

  return (
    <Autocomplete
      options={opcoesComSelecionado}
      value={value}
      onChange={(_, novo) => onChange(novo)}
      onInputChange={(_, novoTexto) => setTexto(novoTexto)}
      getOptionLabel={descricaoEquipamento}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      loading={isFetching}
      disabled={disabled}
      renderInput={(params) => (
        <TextField {...params} label="Equipamento" required helperText="Busque por patrimônio, marca ou modelo." />
      )}
    />
  );
}

function DialogLicenca({ aberto, onFechar, onSalvar, salvando, erro, licencaEditando }) {
  const ehEdicao = !!licencaEditando;

  const [equipamento, setEquipamento] = useState(
    licencaEditando
      ? { id: licencaEditando.equipamentoId, descricao: licencaEditando.equipamentoDescricao }
      : null
  );
  const [produto, setProduto] = useState(licencaEditando?.produto ?? "");
  const [chave, setChave] = useState(licencaEditando?.chave ?? "");
  const [tipo, setTipo] = useState(licencaEditando?.tipo ?? "");
  const [observacao, setObservacao] = useState(licencaEditando?.observacao ?? "");
  const [notaFiscal, setNotaFiscal] = useState(
    licencaEditando?.notaFiscalId ? { id: licencaEditando.notaFiscalId, numero: licencaEditando.notaFiscalNumero } : null
  );

  const handleSalvar = () => {
    onSalvar({
      equipamentoId: equipamento?.id,
      produto,
      chave,
      tipo: tipo || null,
      observacao: observacao || null,
      notaFiscalId: notaFiscal?.id ?? null,
    });
  };

  const valido = equipamento?.id && produto.trim() && chave.trim();

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar licença" : "Nova licença"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}

          <SeletorEquipamento value={equipamento} onChange={setEquipamento} disabled={ehEdicao} />

          <TextField label="Produto" value={produto} onChange={(e) => setProduto(e.target.value)} autoFocus required />
          <TextField
            label="Chave de licença"
            value={chave}
            onChange={(e) => setChave(e.target.value)}
            required
            helperText="Gravada de forma criptografada no banco."
          />
          <TextField label="Tipo" select value={tipo} onChange={(e) => setTipo(e.target.value)}>
            <MenuItem value="">
              <em>Não informado</em>
            </MenuItem>
            {TIPOS_LICENCA.map((t) => (
              <MenuItem key={t} value={t}>
                {t}
              </MenuItem>
            ))}
          </TextField>

          <SeletorNotaFiscal value={notaFiscal} onChange={setNotaFiscal} />

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

// Chave só aparece em texto quando o usuário pede — evita deixar exposta na
// tela por padrão, mesmo que a API já a devolva descriptografada.
function CelulaChave({ valor }) {
  const [visivel, setVisivel] = useState(false);

  return (
    <Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
      <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
        {visivel ? valor : "•".repeat(Math.min(valor.length, 20))}
      </Typography>
      <IconButton size="small" onClick={() => setVisivel((v) => !v)} aria-label="mostrar/ocultar chave">
        {visivel ? <VisibilityOffOutlinedIcon fontSize="small" /> : <VisibilityOutlinedIcon fontSize="small" />}
      </IconButton>
      <IconButton size="small" onClick={() => navigator.clipboard.writeText(valor)} aria-label="copiar chave">
        <ContentCopyOutlinedIcon fontSize="small" />
      </IconButton>
    </Stack>
  );
}

export default function LicencasPage() {
  const queryClient = useQueryClient();

  const [equipamentoFiltro, setEquipamentoFiltro] = useState(null);
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogLicenca, setDialogLicenca] = useState(null); // null | "novo" | licenca
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["licencas", { equipamentoId: equipamentoFiltro?.id, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarLicencas({
        equipamentoId: equipamentoFiltro?.id,
        page: paginationModel.page + 1,
        pageSize: paginationModel.pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["licencas"] });
    setDialogLicenca(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarLicenca,
    onSuccess: () => invalidarEFechar("Licença criada com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarLicenca(id, dados),
    onSuccess: () => invalidarEFechar("Licença atualizada."),
  });

  const removerMutation = useMutation({
    mutationFn: removerLicenca,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["licencas"] });
      setMensagem({ tipo: "success", texto: "Licença removida." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const colunas = [
    { field: "produto", headerName: "Produto", flex: 1 },
    { field: "equipamentoDescricao", headerName: "Equipamento", flex: 1 },
    { field: "tipo", headerName: "Tipo", width: 100 },
    {
      field: "chave",
      headerName: "Chave",
      flex: 1.3,
      sortable: false,
      renderCell: (params) => <CelulaChave valor={params.value} />,
    },
    { field: "notaFiscalNumero", headerName: "Nota fiscal", width: 130 },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 100,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Editar">
            <IconButton size="small" onClick={() => setDialogLicenca(params.row)}>
              <EditOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Remover">
            <IconButton size="small" onClick={() => removerMutation.mutate(params.row.id)}>
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
        Licenças
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <Box sx={{ minWidth: 320 }}>
          <SeletorEquipamento value={equipamentoFiltro} onChange={setEquipamentoFiltro} />
        </Box>
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={() => setDialogLicenca("novo")}>
          Nova licença
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
          getRowHeight={() => "auto"}
          sx={{
            "& .MuiDataGrid-cell": { display: "flex", alignItems: "center", py: 1 },
            "& .MuiDataGrid-columnHeaderTitleContainer": { alignItems: "center" },
          }}
        />
      </Box>

      {dialogLicenca && (
        <DialogLicenca
          aberto
          licencaEditando={dialogLicenca === "novo" ? null : dialogLicenca}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogLicenca(null)}
          onSalvar={(dados) =>
            dialogLicenca === "novo"
              ? criarMutation.mutate(dados)
              : atualizarMutation.mutate({ id: dialogLicenca.id, dados })
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
