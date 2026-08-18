import { useQuery } from "@tanstack/react-query";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import { listarResponsaveis } from "../api/responsaveis";

// Mesma ideia do SeletorLocal: carrega uma página generosa de uma vez em
// vez de busca assíncrona — o volume de responsáveis cadastrados é modesto.
export default function SeletorResponsavel({ value, onChange, label = "Responsável", ...props }) {
  const { data } = useQuery({
    queryKey: ["responsaveis-select"],
    queryFn: () => listarResponsaveis({ status: "Ativo", page: 1, pageSize: 100 }),
  });
  const responsaveis = data?.items ?? [];

  return (
    <TextField
      label={label}
      select
      value={value ?? ""}
      onChange={(e) => onChange(e.target.value ? Number(e.target.value) : null)}
      {...props}
    >
      <MenuItem value="">
        <em>Nenhum</em>
      </MenuItem>
      {responsaveis.map((r) => (
        <MenuItem key={r.id} value={r.id}>
          {r.nome}
        </MenuItem>
      ))}
    </TextField>
  );
}
