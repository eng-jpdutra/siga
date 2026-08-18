import { useQuery } from "@tanstack/react-query";
import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Alert from "@mui/material/Alert";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import EventBusyOutlinedIcon from "@mui/icons-material/EventBusyOutlined";
import PersonOffOutlinedIcon from "@mui/icons-material/PersonOffOutlined";
import { obterDashboard } from "../api/dashboard";

const RESUMO_TIPO = { Computador: "Computadores", Impressora: "Impressoras", DispositivoRede: "Disp. de rede", Outro: "Outros" };
const COR_STATUS = { Ativo: "success", Manutencao: "warning", Baixado: "default" };

const formatoData = new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" });

function CartaoContador({ icone: Icone, titulo, valor, corIcone = "primary.main" }) {
  return (
    <Paper sx={{ p: 2.5, flex: "1 1 200px", display: "flex", gap: 2 }}>
      <Box sx={{ color: corIcone, display: "flex", alignItems: "center" }}>
        <Icone sx={{ fontSize: 36 }} />
      </Box>
      <Box>
        <Typography variant="h4" component="div">{valor}</Typography>
        <Typography variant="body2" color="text.secondary">{titulo}</Typography>
      </Box>
    </Paper>
  );
}

function PainelLista({ titulo, total, itens, itemVazio, renderItem }) {
  return (
    <Paper sx={{ p: 2.5, flex: "1 1 340px" }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1.5 }}>
        <Typography variant="subtitle1">{titulo}</Typography>
        {total > 0 && <Chip size="small" label={total} color="warning" />}
      </Stack>
      {itens.length === 0 ? (
        <Typography variant="body2" color="text.secondary">{itemVazio}</Typography>
      ) : (
        <Stack spacing={1}>{itens.map(renderItem)}</Stack>
      )}
    </Paper>
  );
}

export default function HomePage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["dashboard"],
    queryFn: obterDashboard,
  });

  if (isLoading) {
    return (
      <Stack sx={{ alignItems: "center", mt: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (isError) {
    return <Alert severity="error">{error.message}</Alert>;
  }

  const porStatusMapa = Object.fromEntries(data.porStatus.map((s) => [s.chave, s.quantidade]));

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h5" sx={{ mb: 0.5 }}>Visão geral</Typography>
        <Typography variant="body2" color="text.secondary">
          Resumo do inventário de TI da Câmara.
        </Typography>
      </Box>

      <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap" }}>
        <CartaoContador icone={Inventory2OutlinedIcon} titulo="Equipamentos cadastrados" valor={data.totalEquipamentos} />
        <CartaoContador icone={EventBusyOutlinedIcon} titulo="Garantias vencendo/vencidas" valor={data.totalGarantiasVencendo} corIcone="secondary.main" />
        <CartaoContador icone={PersonOffOutlinedIcon} titulo="Sem responsável" valor={data.totalSemResponsavel} corIcone="secondary.main" />
      </Stack>

      <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap" }}>
        <Paper sx={{ p: 2.5, flex: "1 1 260px" }}>
          <Typography variant="subtitle1" sx={{ mb: 1.5 }}>Por status</Typography>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
            {data.porStatus.length === 0 && <Typography variant="body2" color="text.secondary">Nenhum equipamento cadastrado.</Typography>}
            {data.porStatus.map((s) => (
              <Chip key={s.chave} label={`${s.chave}: ${s.quantidade}`} color={COR_STATUS[s.chave] ?? "default"} variant="outlined" />
            ))}
          </Stack>
        </Paper>

        <Paper sx={{ p: 2.5, flex: "1 1 260px" }}>
          <Typography variant="subtitle1" sx={{ mb: 1.5 }}>Por tipo (ativo + manutenção)</Typography>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
            {data.porTipo.length === 0 && <Typography variant="body2" color="text.secondary">Nenhum equipamento em uso.</Typography>}
            {data.porTipo.map((t) => (
              <Chip key={t.chave} label={`${RESUMO_TIPO[t.chave] ?? t.chave}: ${t.quantidade}`} variant="outlined" />
            ))}
          </Stack>
        </Paper>
      </Stack>

      <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap" }}>
        <PainelLista
          titulo="Garantias vencendo (60 dias) ou vencidas"
          total={data.totalGarantiasVencendo}
          itens={data.garantiasVencendo}
          itemVazio="Nenhuma garantia vencendo."
          renderItem={(item) => (
            <Stack key={item.equipamentoId} direction="row" spacing={1} sx={{ alignItems: "center", justifyContent: "space-between" }}>
              <Typography variant="body2">{item.descricao}</Typography>
              <Typography variant="caption" color="text.secondary">
                {formatoData.format(new Date(`${item.garantiaAte}T00:00:00Z`))}
              </Typography>
            </Stack>
          )}
        />

        <PainelLista
          titulo="Equipamentos sem responsável"
          total={data.totalSemResponsavel}
          itens={data.semResponsavel}
          itemVazio="Todos os equipamentos ativos têm responsável."
          renderItem={(item) => (
            <Typography key={item.equipamentoId} variant="body2">{item.descricao}</Typography>
          )}
        />
      </Stack>

      <Paper sx={{ p: 2.5 }}>
        <Typography variant="subtitle1" sx={{ mb: 1.5 }}>Atividade recente</Typography>
        {data.atividadeRecente.length === 0 ? (
          <Typography variant="body2" color="text.secondary">Nenhum lançamento de histórico ainda.</Typography>
        ) : (
          <Stack spacing={1.5}>
            {data.atividadeRecente.map((a, i) => (
              <Box key={i}>
                <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                  <Chip size="small" label={a.tipo} />
                  <Typography variant="body2" fontWeight={500}>{a.equipamentoDescricao}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatoData.format(new Date(`${a.data}T00:00:00Z`))}
                  </Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary">{a.descricao}</Typography>
              </Box>
            ))}
          </Stack>
        )}
      </Paper>
    </Stack>
  );
}
