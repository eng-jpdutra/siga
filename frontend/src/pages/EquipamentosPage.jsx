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
import MenuItem from "@mui/material/MenuItem";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import AddIcon from "@mui/icons-material/Add";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import {
  listarEquipamentos,
  obterEquipamento,
  criarEquipamento,
  atualizarEquipamento,
  baixarEquipamento,
  reativarEquipamento,
  listarHistorico,
  criarHistorico,
} from "../api/equipamentos";
import SeletorLocal from "../components/SeletorLocal";
import SeletorResponsavel from "../components/SeletorResponsavel";
import SeletorNotaFiscal from "../components/SeletorNotaFiscal";

const TIPOS = ["Computador", "Impressora", "DispositivoRede", "Outro"];
const RESUMO_TIPO = { Computador: "Computador", Impressora: "Impressora", DispositivoRede: "Disp. de rede", Outro: "Outro" };
const STATUS_OPCOES = ["Ativo", "Manutencao", "Baixado"];
const SUBTIPO_COMPUTADOR = ["Desktop", "Notebook"];
const TIPO_ARMAZENAMENTO = ["HDD", "SSD", "NVMe"];
const TIPO_IMPRESSAO = ["Laser", "JatoDeTinta", "Matricial"];
const CONEXAO_IMPRESSORA = ["USB", "Rede"];
const SUBTIPO_DISPOSITIVO_REDE = ["Switch", "Roteador", "AccessPoint", "Firewall"];
const TIPOS_HISTORICO_MANUAL = ["Manutencao", "Formatacao", "Outro"];

function valoresIniciais(eq) {
  return {
    tipo: eq?.tipo ?? "Computador",
    patrimonio: eq?.patrimonio ?? "",
    numeroSerie: eq?.numeroSerie ?? "",
    marca: eq?.marca ?? "",
    modelo: eq?.modelo ?? "",
    localId: eq?.localId ?? null,
    responsavelId: eq?.responsavelId ?? null,
    notaFiscal: eq?.notaFiscalId ? { id: eq.notaFiscalId, numero: eq.notaFiscalNumero } : null,
    anoAquisicao: eq?.anoAquisicao ?? "",
    garantiaAte: eq?.garantiaAte ?? "",
    observacao: eq?.observacao ?? "",
    subtipoComputador: eq?.subtipoComputador ?? "",
    sistemaOperacional: eq?.sistemaOperacional ?? "",
    ramGb: eq?.ramGb ?? "",
    armazenamentoGb: eq?.armazenamentoGb ?? "",
    tipoArmazenamento: eq?.tipoArmazenamento ?? "",
    processador: eq?.processador ?? "",
    tipoImpressao: eq?.tipoImpressao ?? "",
    colorida: eq?.colorida ?? false,
    conexao: eq?.conexao ?? "",
    contadorPaginas: eq?.contadorPaginas ?? "",
    subtipoDispositivoRede: eq?.subtipoDispositivoRede ?? "",
    enderecoIp: eq?.enderecoIp ?? "",
    enderecoMac: eq?.enderecoMac ?? "",
    numPortas: eq?.numPortas ?? "",
    versaoFirmware: eq?.versaoFirmware ?? "",
  };
}

function paraNumeroOuNulo(v) {
  if (v === "" || v === null || v === undefined) return null;
  const n = Number(v);
  return Number.isNaN(n) ? null : n;
}

function paraPayload(form) {
  return {
    tipo: form.tipo,
    patrimonio: form.patrimonio || null,
    numeroSerie: form.numeroSerie || null,
    marca: form.marca,
    modelo: form.modelo,
    localId: form.localId,
    responsavelId: form.responsavelId,
    notaFiscalId: form.notaFiscal?.id ?? null,
    anoAquisicao: paraNumeroOuNulo(form.anoAquisicao),
    garantiaAte: form.garantiaAte || null,
    observacao: form.observacao || null,
    subtipoComputador: form.subtipoComputador || null,
    sistemaOperacional: form.sistemaOperacional || null,
    ramGb: paraNumeroOuNulo(form.ramGb),
    armazenamentoGb: paraNumeroOuNulo(form.armazenamentoGb),
    tipoArmazenamento: form.tipoArmazenamento || null,
    processador: form.processador || null,
    tipoImpressao: form.tipoImpressao || null,
    colorida: !!form.colorida,
    conexao: form.conexao || null,
    contadorPaginas: paraNumeroOuNulo(form.contadorPaginas),
    subtipoDispositivoRede: form.subtipoDispositivoRede || null,
    enderecoIp: form.enderecoIp || null,
    enderecoMac: form.enderecoMac || null,
    numPortas: paraNumeroOuNulo(form.numPortas),
    versaoFirmware: form.versaoFirmware || null,
  };
}

function CamposComputador({ form, set }) {
  return (
    <>
      <Stack direction="row" spacing={2}>
        <TextField label="Subtipo" select value={form.subtipoComputador} onChange={(e) => set("subtipoComputador", e.target.value)} fullWidth>
          <MenuItem value=""><em>Não informado</em></MenuItem>
          {SUBTIPO_COMPUTADOR.map((v) => <MenuItem key={v} value={v}>{v}</MenuItem>)}
        </TextField>
        <TextField label="Sistema operacional" value={form.sistemaOperacional} onChange={(e) => set("sistemaOperacional", e.target.value)} fullWidth />
      </Stack>
      <Stack direction="row" spacing={2}>
        <TextField label="RAM (GB)" type="number" value={form.ramGb} onChange={(e) => set("ramGb", e.target.value)} fullWidth />
        <TextField label="Armazenamento (GB)" type="number" value={form.armazenamentoGb} onChange={(e) => set("armazenamentoGb", e.target.value)} fullWidth />
        <TextField label="Tipo de armazenamento" select value={form.tipoArmazenamento} onChange={(e) => set("tipoArmazenamento", e.target.value)} fullWidth>
          <MenuItem value=""><em>Não informado</em></MenuItem>
          {TIPO_ARMAZENAMENTO.map((v) => <MenuItem key={v} value={v}>{v}</MenuItem>)}
        </TextField>
      </Stack>
      <TextField label="Processador" value={form.processador} onChange={(e) => set("processador", e.target.value)} />
    </>
  );
}

function CamposImpressora({ form, set }) {
  return (
    <>
      <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
        <TextField label="Tipo de impressão" select value={form.tipoImpressao} onChange={(e) => set("tipoImpressao", e.target.value)} fullWidth>
          <MenuItem value=""><em>Não informado</em></MenuItem>
          {TIPO_IMPRESSAO.map((v) => <MenuItem key={v} value={v}>{v}</MenuItem>)}
        </TextField>
        <TextField label="Conexão" select value={form.conexao} onChange={(e) => set("conexao", e.target.value)} fullWidth>
          <MenuItem value=""><em>Não informado</em></MenuItem>
          {CONEXAO_IMPRESSORA.map((v) => <MenuItem key={v} value={v}>{v}</MenuItem>)}
        </TextField>
      </Stack>
      <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
        <TextField label="Contador de páginas" type="number" value={form.contadorPaginas} onChange={(e) => set("contadorPaginas", e.target.value)} fullWidth />
        <FormControlLabel
          control={<Checkbox checked={form.colorida} onChange={(e) => set("colorida", e.target.checked)} />}
          label="Colorida"
          sx={{ minWidth: 160 }}
        />
      </Stack>
    </>
  );
}

function CamposDispositivoRede({ form, set }) {
  return (
    <>
      <Stack direction="row" spacing={2}>
        <TextField label="Subtipo" select value={form.subtipoDispositivoRede} onChange={(e) => set("subtipoDispositivoRede", e.target.value)} fullWidth>
          <MenuItem value=""><em>Não informado</em></MenuItem>
          {SUBTIPO_DISPOSITIVO_REDE.map((v) => <MenuItem key={v} value={v}>{v}</MenuItem>)}
        </TextField>
        <TextField label="Nº de portas" type="number" value={form.numPortas} onChange={(e) => set("numPortas", e.target.value)} fullWidth />
      </Stack>
      <Stack direction="row" spacing={2}>
        <TextField label="Endereço IP" value={form.enderecoIp} onChange={(e) => set("enderecoIp", e.target.value)} fullWidth />
        <TextField label="Endereço MAC" value={form.enderecoMac} onChange={(e) => set("enderecoMac", e.target.value)} fullWidth />
      </Stack>
      <TextField label="Versão do firmware" value={form.versaoFirmware} onChange={(e) => set("versaoFirmware", e.target.value)} />
    </>
  );
}

function DialogEquipamento({ aberto, onFechar, onSalvar, salvando, erro, equipamentoId }) {
  const ehEdicao = !!equipamentoId;

  const { data: equipamentoCarregado, isLoading: carregando } = useQuery({
    queryKey: ["equipamento", equipamentoId],
    queryFn: () => obterEquipamento(equipamentoId),
    enabled: ehEdicao,
  });

  const [form, setForm] = useState(() => valoresIniciais(null));
  const [formInicializado, setFormInicializado] = useState(!ehEdicao);

  if (ehEdicao && equipamentoCarregado && !formInicializado) {
    setForm(valoresIniciais(equipamentoCarregado));
    setFormInicializado(true);
  }

  const set = (campo, valor) => setForm((f) => ({ ...f, [campo]: valor }));

  const valido = form.marca.trim() && form.modelo.trim();

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="md">
      <DialogTitle>{ehEdicao ? "Editar equipamento" : "Novo equipamento"}</DialogTitle>
      <DialogContent>
        {ehEdicao && carregando ? (
          <Stack alignItems="center" sx={{ py: 4 }}>
            <CircularProgress size={28} />
          </Stack>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {erro && <Alert severity="error">{erro}</Alert>}

            <TextField
              label="Tipo"
              select
              value={form.tipo}
              onChange={(e) => set("tipo", e.target.value)}
              disabled={ehEdicao}
              helperText={ehEdicao ? "Não é possível mudar o tipo depois de criado." : undefined}
            >
              {TIPOS.map((t) => (
                <MenuItem key={t} value={t}>{RESUMO_TIPO[t]}</MenuItem>
              ))}
            </TextField>

            <Stack direction="row" spacing={2}>
              <TextField label="Marca" value={form.marca} onChange={(e) => set("marca", e.target.value)} required fullWidth />
              <TextField label="Modelo" value={form.modelo} onChange={(e) => set("modelo", e.target.value)} required fullWidth />
            </Stack>

            <Stack direction="row" spacing={2}>
              <TextField label="Patrimônio" value={form.patrimonio} onChange={(e) => set("patrimonio", e.target.value)} fullWidth />
              <TextField label="Número de série" value={form.numeroSerie} onChange={(e) => set("numeroSerie", e.target.value)} fullWidth />
            </Stack>

            <Divider textAlign="left">Localização e responsabilidade</Divider>

            <Stack direction="row" spacing={2}>
              <Box sx={{ flex: 1 }}>
                <SeletorLocal value={form.localId} onChange={(v) => set("localId", v)} fullWidth />
              </Box>
              <Box sx={{ flex: 1 }}>
                <SeletorResponsavel value={form.responsavelId} onChange={(v) => set("responsavelId", v)} fullWidth />
              </Box>
            </Stack>
            <SeletorNotaFiscal value={form.notaFiscal} onChange={(v) => set("notaFiscal", v)} />

            <Divider textAlign="left">Aquisição</Divider>

            <Stack direction="row" spacing={2}>
              <TextField label="Ano de aquisição" type="number" value={form.anoAquisicao} onChange={(e) => set("anoAquisicao", e.target.value)} fullWidth />
              <TextField
                label="Garantia até"
                type="date"
                value={form.garantiaAte}
                onChange={(e) => set("garantiaAte", e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                fullWidth
              />
            </Stack>

            {form.tipo !== "Outro" && <Divider textAlign="left">Detalhes do {RESUMO_TIPO[form.tipo].toLowerCase()}</Divider>}
            {form.tipo === "Computador" && <CamposComputador form={form} set={set} />}
            {form.tipo === "Impressora" && <CamposImpressora form={form} set={set} />}
            {form.tipo === "DispositivoRede" && <CamposDispositivoRede form={form} set={set} />}

            <TextField label="Observação" value={form.observacao} onChange={(e) => set("observacao", e.target.value)} multiline minRows={2} />
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Cancelar</Button>
        <Button variant="contained" onClick={() => onSalvar(paraPayload(form))} disabled={!valido || salvando || (ehEdicao && carregando)}>
          Salvar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function DialogHistorico({ equipamentoId, aberto, onFechar }) {
  const queryClient = useQueryClient();
  const [tipo, setTipo] = useState("Manutencao");
  const [data, setData] = useState(() => new Date().toISOString().slice(0, 10));
  const [descricao, setDescricao] = useState("");

  const { data: historico = [], isLoading } = useQuery({
    queryKey: ["historico", equipamentoId],
    queryFn: () => listarHistorico(equipamentoId),
    enabled: aberto,
  });

  const mutation = useMutation({
    mutationFn: () => criarHistorico(equipamentoId, { tipo, data, descricao }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["historico", equipamentoId] });
      setDescricao("");
    },
  });

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>Histórico do equipamento</DialogTitle>
      <DialogContent>
        <Stack spacing={1} sx={{ mb: 2, maxHeight: 280, overflowY: "auto" }}>
          {isLoading && <CircularProgress size={24} />}
          {!isLoading && historico.length === 0 && (
            <Typography variant="body2" color="text.secondary">Nenhum lançamento ainda.</Typography>
          )}
          {historico.map((h) => (
            <Box key={h.id} sx={{ p: 1, borderRadius: 1, bgcolor: "background.default" }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Chip size="small" label={h.tipo} />
                <Typography variant="caption" color="text.secondary">{h.data}</Typography>
                {h.registradoPor && (
                  <Typography variant="caption" color="text.secondary">— {h.registradoPor}</Typography>
                )}
              </Stack>
              <Typography variant="body2">{h.descricao}</Typography>
            </Box>
          ))}
        </Stack>

        <Divider textAlign="left">Novo lançamento</Divider>
        <Stack spacing={2} sx={{ mt: 2 }}>
          {mutation.isError && <Alert severity="error">{mutation.error.message}</Alert>}
          <Stack direction="row" spacing={2}>
            <TextField label="Tipo" select value={tipo} onChange={(e) => setTipo(e.target.value)} sx={{ minWidth: 180 }}>
              {TIPOS_HISTORICO_MANUAL.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <TextField
              label="Data"
              type="date"
              value={data}
              onChange={(e) => setData(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
          <TextField label="Descrição" value={descricao} onChange={(e) => setDescricao(e.target.value)} multiline minRows={2} />
          <Box>
            <Button
              variant="outlined"
              onClick={() => mutation.mutate()}
              disabled={!descricao.trim() || mutation.isPending}
            >
              Adicionar lançamento
            </Button>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Fechar</Button>
      </DialogActions>
    </Dialog>
  );
}

export default function EquipamentosPage() {
  const queryClient = useQueryClient();

  const [termo, setTermo] = useState("");
  const [tipoFiltro, setTipoFiltro] = useState("");
  const [statusFiltro, setStatusFiltro] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogEquipamento, setDialogEquipamento] = useState(null); // null | "novo" | id
  const [historicoDe, setHistoricoDe] = useState(null); // id | null
  const [mensagem, setMensagem] = useState(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["equipamentos", { termo, tipoFiltro, statusFiltro, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarEquipamentos({
        termo,
        tipo: tipoFiltro || undefined,
        status: statusFiltro || undefined,
        page: paginationModel.page + 1,
        pageSize: paginationModel.pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["equipamentos"] });
    setDialogEquipamento(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarEquipamento,
    onSuccess: () => invalidarEFechar("Equipamento cadastrado com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarEquipamento(id, dados),
    onSuccess: () => invalidarEFechar("Equipamento atualizado."),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, ativar }) => (ativar ? reativarEquipamento(id) : baixarEquipamento(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipamentos"] });
      setMensagem({ tipo: "success", texto: "Status atualizado." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const colunas = [
    { field: "tipo", headerName: "Tipo", width: 130, valueFormatter: (v) => RESUMO_TIPO[v] ?? v },
    { field: "patrimonio", headerName: "Patrimônio", width: 120 },
    { field: "marca", headerName: "Marca", flex: 1 },
    { field: "modelo", headerName: "Modelo", flex: 1 },
    { field: "localNome", headerName: "Local", flex: 1 },
    { field: "responsavelNome", headerName: "Responsável", flex: 1 },
    {
      field: "status",
      headerName: "Status",
      width: 110,
      renderCell: (params) => (
        <Chip
          size="small"
          label={params.value}
          color={params.value === "Ativo" ? "success" : params.value === "Baixado" ? "default" : "warning"}
          variant="outlined"
        />
      ),
    },
    {
      field: "acoes",
      headerName: "",
      sortable: false,
      width: 140,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Histórico">
            <IconButton size="small" onClick={() => setHistoricoDe(params.row.id)}>
              <HistoryOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Editar">
            <IconButton size="small" onClick={() => setDialogEquipamento(params.row.id)}>
              <EditOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title={params.row.status === "Baixado" ? "Reativar" : "Dar baixa"}>
            <IconButton
              size="small"
              onClick={() => statusMutation.mutate({ id: params.row.id, ativar: params.row.status === "Baixado" })}
            >
              {params.row.status === "Baixado" ? (
                <CheckCircleOutlinedIcon fontSize="small" />
              ) : (
                <BlockOutlinedIcon fontSize="small" />
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
        Equipamentos
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <TextField
          label="Buscar por patrimônio, série, marca ou modelo"
          size="small"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          sx={{ minWidth: 320 }}
        />
        <TextField label="Tipo" select size="small" value={tipoFiltro} onChange={(e) => setTipoFiltro(e.target.value)} sx={{ minWidth: 160 }}>
          <MenuItem value="">Todos</MenuItem>
          {TIPOS.map((t) => <MenuItem key={t} value={t}>{RESUMO_TIPO[t]}</MenuItem>)}
        </TextField>
        <TextField label="Status" select size="small" value={statusFiltro} onChange={(e) => setStatusFiltro(e.target.value)} sx={{ minWidth: 160 }}>
          <MenuItem value="">Todos</MenuItem>
          {STATUS_OPCOES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
        </TextField>
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={() => setDialogEquipamento("novo")}>
          Novo equipamento
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

      {dialogEquipamento && (
        <DialogEquipamento
          aberto
          equipamentoId={dialogEquipamento === "novo" ? null : dialogEquipamento}
          salvando={criarMutation.isPending || atualizarMutation.isPending}
          erro={
            criarMutation.isError
              ? criarMutation.error.message
              : atualizarMutation.isError
                ? atualizarMutation.error.message
                : null
          }
          onFechar={() => setDialogEquipamento(null)}
          onSalvar={(payload) =>
            dialogEquipamento === "novo"
              ? criarMutation.mutate(payload)
              : atualizarMutation.mutate({ id: dialogEquipamento, dados: payload })
          }
        />
      )}

      {historicoDe && (
        <DialogHistorico equipamentoId={historicoDe} aberto onFechar={() => setHistoricoDe(null)} />
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
