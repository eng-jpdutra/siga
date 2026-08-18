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
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import OutlinedInput from "@mui/material/OutlinedInput";
import PersonAddAltOutlinedIcon from "@mui/icons-material/PersonAddAltOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import { listarUsuarios, criarUsuario, atualizarUsuario, desativarUsuario, ativarUsuario, redefinirSenha } from "../api/usuarios";
import { listarPapeis } from "../api/papeis";
import { useAuth } from "../auth/AuthContext";
import CardMeuPerfil from "../components/CardMeuPerfil";

function SeletorDePapeis({ papeis, papeisSelecionados, onChange }) {
  return (
    <FormControl fullWidth>
      <InputLabel id="papeis-label">Papéis</InputLabel>
      <Select
        labelId="papeis-label"
        multiple
        value={papeisSelecionados}
        onChange={(e) => onChange(e.target.value)}
        input={<OutlinedInput label="Papéis" />}
        renderValue={(selecionados) => (
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: "wrap" }}>
            {selecionados.map((id) => (
              <Chip key={id} size="small" label={papeis.find((p) => p.id === id)?.nome ?? id} />
            ))}
          </Stack>
        )}
      >
        {papeis.map((papel) => (
          <MenuItem key={papel.id} value={papel.id}>
            {papel.nome}
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

function DialogUsuario({ aberto, onFechar, onSalvar, salvando, erro, papeis, usuarioEditando }) {
  const ehEdicao = !!usuarioEditando;

  const [nome, setNome] = useState(usuarioEditando?.nome ?? "");
  const [nomeUsuario, setNomeUsuario] = useState(usuarioEditando?.nomeUsuario ?? "");
  const [senha, setSenha] = useState("");
  const [papeisIds, setPapeisIds] = useState(
    usuarioEditando ? papeis.filter((p) => usuarioEditando.papeis.includes(p.nome)).map((p) => p.id) : []
  );

  const handleSalvar = () => {
    const dados = ehEdicao
      ? { nome, nomeUsuario, papeisIds }
      : { nome, nomeUsuario, senha, papeisIds, responsavelId: null };
    onSalvar(dados);
  };

  const valido = nome.trim() && nomeUsuario.trim() && papeisIds.length > 0 && (ehEdicao || senha.length >= 8);

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="sm">
      <DialogTitle>{ehEdicao ? "Editar usuário" : "Novo usuário"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}

          <TextField label="Nome" value={nome} onChange={(e) => setNome(e.target.value)} autoFocus required />
          <TextField
            label="Usuário"
            value={nomeUsuario}
            onChange={(e) => setNomeUsuario(e.target.value)}
            required
            helperText="Usado para fazer login — sem espaços."
          />
          {!ehEdicao && (
            <TextField
              label="Senha"
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              required
              helperText="Pelo menos 8 caracteres."
            />
          )}
          <SeletorDePapeis papeis={papeis} papeisSelecionados={papeisIds} onChange={setPapeisIds} />
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

function DialogRedefinirSenha({ aberto, onFechar, onSalvar, salvando, erro }) {
  const [novaSenha, setNovaSenha] = useState("");

  return (
    <Dialog open={aberto} onClose={onFechar} fullWidth maxWidth="xs">
      <DialogTitle>Redefinir senha</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {erro && <Alert severity="error">{erro}</Alert>}
          <TextField
            label="Nova senha"
            type="password"
            value={novaSenha}
            onChange={(e) => setNovaSenha(e.target.value)}
            autoFocus
            helperText="Pelo menos 8 caracteres."
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => onSalvar(novaSenha, () => setNovaSenha(""))}
          disabled={novaSenha.length < 8 || salvando}
        >
          Redefinir
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function UsuariosPage() {
  const queryClient = useQueryClient();
  const { usuario: usuarioLogado } = useAuth();
  const souAdministrador = !!usuarioLogado?.papeis?.includes("Administrador");

  const [nome, setNome] = useState("");
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 });
  const [dialogUsuario, setDialogUsuario] = useState(null); // null | "novo" | usuario
  const [dialogSenhaPara, setDialogSenhaPara] = useState(null); // usuario | null
  const [mensagem, setMensagem] = useState(null);

  // Gestão de outras contas é ação sensível — só busca esses dados quando
  // a pessoa logada de fato tem o papel Administrador (a API já bloqueia
  // isso também, mas não faz sentido nem tentar buscar aqui).
  const { data: papeis = [] } = useQuery({
    queryKey: ["papeis"],
    queryFn: listarPapeis,
    enabled: souAdministrador,
  });

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["usuarios", { nome, page: paginationModel.page, pageSize: paginationModel.pageSize }],
    queryFn: () =>
      listarUsuarios({ nome, page: paginationModel.page + 1, pageSize: paginationModel.pageSize }),
    placeholderData: keepPreviousData,
    enabled: souAdministrador,
  });

  function invalidarEFechar(mensagemSucesso) {
    queryClient.invalidateQueries({ queryKey: ["usuarios"] });
    setDialogUsuario(null);
    setDialogSenhaPara(null);
    setMensagem({ tipo: "success", texto: mensagemSucesso });
  }

  const criarMutation = useMutation({
    mutationFn: criarUsuario,
    onSuccess: () => invalidarEFechar("Usuário criado com sucesso."),
  });

  const atualizarMutation = useMutation({
    mutationFn: ({ id, dados }) => atualizarUsuario(id, dados),
    onSuccess: () => invalidarEFechar("Usuário atualizado."),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, ativar }) => (ativar ? ativarUsuario(id) : desativarUsuario(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["usuarios"] });
      setMensagem({ tipo: "success", texto: "Status atualizado." });
    },
    onError: (err) => setMensagem({ tipo: "error", texto: err.message }),
  });

  const redefinirSenhaMutation = useMutation({
    mutationFn: ({ id, novaSenha }) => redefinirSenha(id, novaSenha),
    onSuccess: () => invalidarEFechar("Senha redefinida."),
  });

  const colunas = [
    { field: "nome", headerName: "Nome", flex: 1 },
    { field: "nomeUsuario", headerName: "Usuário", flex: 1 },
    {
      field: "papeis",
      headerName: "Papéis",
      flex: 1,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} sx={{ overflow: "hidden" }}>
          {params.value.map((p) => (
            <Chip key={p} size="small" label={p} />
          ))}
        </Stack>
      ),
    },
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
      width: 140,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Editar">
            <IconButton size="small" onClick={() => setDialogUsuario(params.row)}>
              <EditOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Redefinir senha">
            <IconButton size="small" onClick={() => setDialogSenhaPara(params.row)}>
              <LockResetOutlinedIcon fontSize="small" />
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
    <Stack spacing={3}>
      <CardMeuPerfil />

      {souAdministrador && (
        <Box>
          <Typography variant="h5" sx={{ mb: 2 }}>
            Outros usuários
          </Typography>

          <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
            <TextField
              label="Buscar por nome ou usuário"
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
              onClick={() => setDialogUsuario("novo")}
            >
              Novo usuário
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

          {dialogUsuario && (
            <DialogUsuario
              aberto
              usuarioEditando={dialogUsuario === "novo" ? null : dialogUsuario}
              papeis={papeis}
              salvando={criarMutation.isPending || atualizarMutation.isPending}
              erro={
                criarMutation.isError
                  ? criarMutation.error.message
                  : atualizarMutation.isError
                    ? atualizarMutation.error.message
                    : null
              }
              onFechar={() => setDialogUsuario(null)}
              onSalvar={(dados) =>
                dialogUsuario === "novo"
                  ? criarMutation.mutate(dados)
                  : atualizarMutation.mutate({ id: dialogUsuario.id, dados })
              }
            />
          )}

          {dialogSenhaPara && (
            <DialogRedefinirSenha
              aberto
              salvando={redefinirSenhaMutation.isPending}
              erro={redefinirSenhaMutation.isError ? redefinirSenhaMutation.error.message : null}
              onFechar={() => setDialogSenhaPara(null)}
              onSalvar={(novaSenha) => redefinirSenhaMutation.mutate({ id: dialogSenhaPara.id, novaSenha })}
            />
          )}
        </Box>
      )}

      <Snackbar
        open={!!mensagem}
        autoHideDuration={4000}
        onClose={() => setMensagem(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        {mensagem && <Alert severity={mensagem.tipo}>{mensagem.texto}</Alert>}
      </Snackbar>
    </Stack>
  );
}
