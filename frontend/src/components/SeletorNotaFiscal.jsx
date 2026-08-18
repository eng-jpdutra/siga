import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Autocomplete from "@mui/material/Autocomplete";
import TextField from "@mui/material/TextField";
import { listarNotasFiscais } from "../api/notasFiscais";
import { useDebounce } from "../hooks/useDebounce";

// Notas fiscais podem se acumular bastante — aqui sim busca assíncrona no
// servidor conforme digita, em vez de carregar tudo de uma vez.
export default function SeletorNotaFiscal({ value, onChange, label = "Nota fiscal (opcional)", helperText }) {
  const [texto, setTexto] = useState("");
  const termo = useDebounce(texto);

  const { data, isFetching } = useQuery({
    queryKey: ["notas-fiscais-busca", termo],
    queryFn: () => listarNotasFiscais({ numero: termo, page: 1, pageSize: 20 }),
  });

  const opcoes = data?.items ?? [];
  const opcoesComSelecionada = value && !opcoes.some((o) => o.id === value.id) ? [value, ...opcoes] : opcoes;

  return (
    <Autocomplete
      options={opcoesComSelecionada}
      value={value}
      onChange={(_, nova) => onChange(nova)}
      onInputChange={(_, novoTexto) => setTexto(novoTexto)}
      getOptionLabel={(nf) => nf?.numero ?? ""}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      loading={isFetching}
      renderInput={(params) => <TextField {...params} label={label} helperText={helperText} />}
    />
  );
}
