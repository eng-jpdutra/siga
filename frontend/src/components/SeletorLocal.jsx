import { useQuery } from "@tanstack/react-query";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import { listarLocais } from "../api/locais";

// Número de locais tende a ser pequeno (salas/setores de uma câmara
// municipal) — carrega tudo de uma vez, sem busca assíncrona no servidor.
export default function SeletorLocal({ value, onChange, label = "Local", ...props }) {
  const { data } = useQuery({
    queryKey: ["locais-select"],
    queryFn: () => listarLocais({ page: 1, pageSize: 100 }),
  });
  const locais = data?.items ?? [];

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
      {locais.map((l) => (
        <MenuItem key={l.id} value={l.id}>
          {l.nome}
        </MenuItem>
      ))}
    </TextField>
  );
}
