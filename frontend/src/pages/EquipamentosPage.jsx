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
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import AddIcon from "@mui/icons-material/Add";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
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
import {
  listarConfiguracoes,
  criarConfiguracao,
  atualizarConfiguracao,
  removerConfiguracao,
} from "../api/configuracoes";
import SeletorLocal from "../components/SeletorLocal";
import SeletorNotaFiscal from "../components/SeletorNotaFiscal";
import { usePodeEscrever } from "../auth/AuthContext";

const TIPOS = ["Computador", "Impressora", "Monitor", "DispositivoRede", "Nobreak", "Camera", "DvrNvr", "TelefoneIp", "Outro"];
const RESUMO_TIPO = {
  Computador: "Computador",
  Impressora: "Impressora",
  Monitor: "Monitor",
  DispositivoRede: "Disp. de rede",
  Nobreak: "Nobreak",
  Camera: "Câmera",
  DvrNvr: "DVR/NVR",
  TelefoneIp: "Telefone IP",
  Outro: "Outro",
};
const STATUS_OPCOES = ["Ativo", "Manutencao", "Baixado"];
const TIPOS_HISTORICO_MANUAL = ["Manutencao", "Formatacao", "Outro"];

// Campos específicos de cada tipo — a "lista de campos por tipo" fica só
// aqui, num lugar só (ver CLAUDE.md). Cada entrada vira um input na tela,
// lida/gravada dentro do JSON `Detalhes` do equipamento; os campos comuns
// (marca, modelo, patrimônio, local...) continuam fora daqui, como colunas.
// IMPORTANTE: a `chave` é o nome gravado no JSON — não renomear depois que
// houver equipamentos cadastrados desse tipo (o rótulo pode mudar à vontade).
const CAMPOS_POR_TIPO = {
  Computador: [
    { chave: "Subtipo", rotulo: "Subtipo", tipo: "select", opcoes: ["Desktop", "Notebook", "All-in-One"] },
    { chave: "Processador", rotulo: "Processador", tipo: "texto" },
    { chave: "RamGb", rotulo: "RAM (GB)", tipo: "numero" },
    { chave: "ArmazenamentoGb", rotulo: "Armazenamento (GB)", tipo: "numero" },
    { chave: "TipoArmazenamento", rotulo: "Tipo de armazenamento", tipo: "select", opcoes: ["SSD", "HDD", "NVMe"] },
    { chave: "SistemaOperacional", rotulo: "Sistema operacional", tipo: "texto" },
  ],
  Impressora: [
    { chave: "TipoImpressao", rotulo: "Tipo de impressão", tipo: "select", opcoes: ["Laser", "JatoDeTinta", "Termica"] },
    { chave: "Colorida", rotulo: "Colorida", tipo: "booleano" },
    { chave: "Conexao", rotulo: "Conexão", tipo: "select", opcoes: ["USB", "Rede", "USB+Rede"] },
    { chave: "ContadorPaginas", rotulo: "Contador de páginas", tipo: "numero" },
  ],
  Monitor: [
    { chave: "Polegadas", rotulo: "Polegadas", tipo: "numero" },
    { chave: "Resolucao", rotulo: "Resolução", tipo: "texto" },
    { chave: "TaxaAtualizacao", rotulo: "Taxa de atualização (Hz)", tipo: "numero" },
    { chave: "TipoPainel", rotulo: "Tipo de painel", tipo: "select", opcoes: ["IPS", "VA", "TN"] },
    { chave: "Conexoes", rotulo: "Conexões", tipo: "texto" },
  ],
  DispositivoRede: [
    { chave: "Subtipo", rotulo: "Subtipo", tipo: "select", opcoes: ["Switch", "Roteador", "AccessPoint", "Firewall"] },
    { chave: "NumPortas", rotulo: "Nº de portas", tipo: "numero" },
    { chave: "VersaoFirmware", rotulo: "Versão do firmware", tipo: "texto" },
  ],
  Nobreak: [
    { chave: "PotenciaVA", rotulo: "Potência (VA)", tipo: "numero" },
    { chave: "NumTomadas", rotulo: "Nº de tomadas", tipo: "numero" },
    { chave: "TempoAutonomiaMin", rotulo: "Autonomia (min)", tipo: "numero" },
    { chave: "TipoBateria", rotulo: "Tipo de bateria", tipo: "select", opcoes: ["Selada", "Externa"] },
    { chave: "DataTrocaBateria", rotulo: "Data da troca de bateria", tipo: "data" },
  ],
  Camera: [
    { chave: "Subtipo", rotulo: "Subtipo", tipo: "select", opcoes: ["Bullet", "Dome", "PTZ"] },
    { chave: "Resolucao", rotulo: "Resolução", tipo: "texto" },
    { chave: "Alimentacao", rotulo: "Alimentação", tipo: "select", opcoes: ["PoE", "12V"] },
    { chave: "Localizacao", rotulo: "O que a câmera cobre", tipo: "texto" },
  ],
  DvrNvr: [
    { chave: "CanaisTotais", rotulo: "Canais totais", tipo: "numero" },
    { chave: "CanaisUsados", rotulo: "Canais usados", tipo: "numero" },
    { chave: "ArmazenamentoTb", rotulo: "Armazenamento (TB)", tipo: "numero" },
    { chave: "DiasRetencao", rotulo: "Dias de retenção", tipo: "numero" },
  ],
  TelefoneIp: [
    { chave: "Ramal", rotulo: "Ramal", tipo: "texto" },
    { chave: "ProtocoloVoip", rotulo: "Protocolo VoIP", tipo: "texto" },
  ],
  Outro: [],
};

// Tipos com placa de rede — só esses mostram Endereço IP/MAC (campos reais
// do equipamento, não do JSON `Detalhes`, porque o MAC precisa ser único).
const TIPOS_COM_REDE = ["DispositivoRede", "Camera", "DvrNvr", "TelefoneIp"];

function valoresIniciais(eq) {
  return {
    tipo: eq?.tipo ?? "Computador",
    patrimonio: eq?.patrimonio ?? "",
    numeroSerie: eq?.numeroSerie ?? "",
    marca: eq?.marca ?? "",
    modelo: eq?.modelo ?? "",
    localId: eq?.localId ?? null,
    notaFiscal: eq?.notaFiscalId ? { id: eq.notaFiscalId, numero: eq.notaFiscalNumero } : null,
    enderecoMac: eq?.enderecoMac ?? "",
    enderecoIp: eq?.enderecoIp ?? "",
    detalhes: eq?.detalhes ?? {},
    anoAquisicao: eq?.anoAquisicao ?? "",
    garantiaAte: eq?.garantiaAte ?? "",
    observacao: eq?.observacao ?? "",
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
    notaFiscalId: form.notaFiscal?.id ?? null,
    enderecoMac: form.enderecoMac || null,
    enderecoIp: form.enderecoIp || null,
    detalhes: form.detalhes,
    anoAquisicao: paraNumeroOuNulo(form.anoAquisicao),
    garantiaAte: form.garantiaAte || null,
    observacao: form.observacao || null,
  };
}

// Renderiza os campos específicos do tipo selecionado, lendo/gravando em
// `form.detalhes` — um único componente genérico em vez de um por tipo.
function CamposDetalhes({ tipo, detalhes, setDetalhe }) {
  const campos = CAMPOS_POR_TIPO[tipo] ?? [];
  if (campos.length === 0) return null;

  return (
    <>
      <Divider textAlign="left">Detalhes do {RESUMO_TIPO[tipo].toLowerCase()}</Divider>
      <Stack spacing={2}>
        {campos.map((campo) => {
          const valor = detalhes[campo.chave] ?? (campo.tipo === "booleano" ? false : "");

          if (campo.tipo === "select") {
            return (
              <TextField
                key={campo.chave}
                label={campo.rotulo}
                select
                value={valor}
                onChange={(e) => setDetalhe(campo.chave, e.target.value)}
                fullWidth
              >
                <MenuItem value=""><em>Não informado</em></MenuItem>
                {campo.opcoes.map((op) => <MenuItem key={op} value={op}>{op}</MenuItem>)}
              </TextField>
            );
          }

          if (campo.tipo === "booleano") {
            return (
              <FormControlLabel
                key={campo.chave}
                control={<Checkbox checked={!!valor} onChange={(e) => setDetalhe(campo.chave, e.target.checked)} />}
                label={campo.rotulo}
              />
            );
          }

          if (campo.tipo === "numero") {
            return (
              <TextField
                key={campo.chave}
                label={campo.rotulo}
                type="number"
                value={valor}
                onChange={(e) => setDetalhe(campo.chave, e.target.value === "" ? "" : Number(e.target.value))}
                fullWidth
              />
            );
          }

          if (campo.tipo === "data") {
            return (
              <TextField
                key={campo.chave}
                label={campo.rotulo}
                type="date"
                value={valor}
                onChange={(e) => setDetalhe(campo.chave, e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                fullWidth
              />
            );
          }

          return (
            <TextField
              key={campo.chave}
              label={campo.rotulo}
              value={valor}
              onChange={(e) => setDetalhe(campo.chave, e.target.value)}
              fullWidth
            />
          );
        })}
      </Stack>
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
  const setDetalhe = (chave, valor) =>
    setForm((f) => ({ ...f, detalhes: { ...f.detalhes, [chave]: valor === "" ? undefined : valor } }));

  const valido = form.marca.trim() && form.modelo.trim();
  const mostrarRede = TIPOS_COM_REDE.includes(form.tipo);

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

            <Divider textAlign="left">Localização</Divider>

            <SeletorLocal value={form.localId} onChange={(v) => set("localId", v)} fullWidth />
            <SeletorNotaFiscal value={form.notaFiscal} onChange={(v) => set("notaFiscal", v)} />

            {mostrarRede && (
              <>
                <Divider textAlign="left">Rede</Divider>
                <Stack direction="row" spacing={2}>
                  <TextField label="Endereço IP" value={form.enderecoIp} onChange={(e) => set("enderecoIp", e.target.value)} fullWidth />
                  <TextField label="Endereço MAC" value={form.enderecoMac} onChange={(e) => set("enderecoMac", e.target.value)} fullWidth />
                </Stack>
              </>
            )}

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

            <CamposDetalhes tipo={form.tipo} detalhes={form.detalhes} setDetalhe={setDetalhe} />

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
  const podeEscrever = usePodeEscrever();
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

        {podeEscrever && (
          <>
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
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Fechar</Button>
      </DialogActions>
    </Dialog>
  );
}

// Anotações técnicas do equipamento — diferente do histórico, podem ser
// editadas/removidas (não é um diário de eventos, ver CLAUDE.md).
function DialogConfiguracoes({ equipamentoId, aberto, onFechar }) {
  const queryClient = useQueryClient();
  const podeEscrever = usePodeEscrever();
  const [editando, setEditando] = useState(null); // null | "novo" | configuracao
  const [titulo, setTitulo] = useState("");
  const [conteudo, setConteudo] = useState("");

  const { data: configuracoes = [], isLoading } = useQuery({
    queryKey: ["configuracoes", equipamentoId],
    queryFn: () => listarConfiguracoes(equipamentoId),
    enabled: aberto,
  });

  function iniciarEdicao(config) {
    setEditando(config ?? "novo");
    setTitulo(config?.titulo ?? "");
    setConteudo(config?.conteudo ?? "");
  }

  function cancelarEdicao() {
    setEditando(null);
    setTitulo("");
    setConteudo("");
  }

  const salvarMutation = useMutation({
    mutationFn: () =>
      editando === "novo"
        ? criarConfiguracao(equipamentoId, { titulo, conteudo })
        : atualizarConfiguracao(equipamentoId, editando.id, { titulo, conteudo }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["configuracoes", equipamentoId] });
      cancelarEdicao();
    },
  });

  const removerMutation = useMutation({
    mutationFn: (configuracaoId) => removerConfiguracao(equipamentoId, configuracaoId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["configuracoes", equipamentoId] }),
  });

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>Configurações e anotações</DialogTitle>
      <DialogContent>
        {editando ? (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {salvarMutation.isError && <Alert severity="error">{salvarMutation.error.message}</Alert>}
            <TextField label="Título" value={titulo} onChange={(e) => setTitulo(e.target.value)} autoFocus required />
            <TextField label="Conteúdo" value={conteudo} onChange={(e) => setConteudo(e.target.value)} multiline minRows={4} required />
            <Stack direction="row" spacing={1} justifyContent="flex-end">
              <Button onClick={cancelarEdicao}>Cancelar</Button>
              <Button
                variant="contained"
                onClick={() => salvarMutation.mutate()}
                disabled={!titulo.trim() || !conteudo.trim() || salvarMutation.isPending}
              >
                Salvar
              </Button>
            </Stack>
          </Stack>
        ) : (
          <>
            {isLoading && <CircularProgress size={24} />}
            {!isLoading && configuracoes.length === 0 && (
              <Typography variant="body2" color="text.secondary">Nenhuma anotação ainda.</Typography>
            )}
            <List disablePadding>
              {configuracoes.map((c) => (
                <ListItem
                  key={c.id}
                  disablePadding
                  sx={{ py: 1, alignItems: "flex-start" }}
                  secondaryAction={
                    podeEscrever && (
                      <Stack direction="row" spacing={0.5}>
                        <IconButton size="small" onClick={() => iniciarEdicao(c)}>
                          <EditOutlinedIcon fontSize="small" />
                        </IconButton>
                        <IconButton size="small" onClick={() => removerMutation.mutate(c.id)}>
                          <DeleteOutlineIcon fontSize="small" />
                        </IconButton>
                      </Stack>
                    )
                  }
                >
                  <ListItemText
                    primary={c.titulo}
                    secondary={c.conteudo}
                    secondaryTypographyProps={{ sx: { whiteSpace: "pre-wrap" } }}
                  />
                </ListItem>
              ))}
            </List>
            {podeEscrever && (
              <Box sx={{ mt: 2 }}>
                <Button variant="outlined" startIcon={<AddIcon />} onClick={() => iniciarEdicao(null)}>
                  Nova anotação
                </Button>
              </Box>
            )}
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Fechar</Button>
      </DialogActions>
    </Dialog>
  );
}

export default function EquipamentosPage() {
  const queryClient = useQueryClient();
  const podeEscrever = usePodeEscrever();

  const [termo, setTermo] = useState("");
  const [tipoFiltro, setTipoFiltro] = useState("");
  const [statusFiltro, setStatusFiltro] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogEquipamento, setDialogEquipamento] = useState(null); // null | "novo" | id
  const [historicoDe, setHistoricoDe] = useState(null); // id | null
  const [configuracoesDe, setConfiguracoesDe] = useState(null); // id | null
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
      width: 170,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Configurações">
            <IconButton size="small" onClick={() => setConfiguracoesDe(params.row.id)}>
              <DescriptionOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Histórico">
            <IconButton size="small" onClick={() => setHistoricoDe(params.row.id)}>
              <HistoryOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          {podeEscrever && (
            <>
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
            </>
          )}
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
        {podeEscrever && (
          <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={() => setDialogEquipamento("novo")}>
            Novo equipamento
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

      {configuracoesDe && (
        <DialogConfiguracoes equipamentoId={configuracoesDe} aberto onFechar={() => setConfiguracoesDe(null)} />
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
