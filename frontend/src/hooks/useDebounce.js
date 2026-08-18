import { useEffect, useState } from "react";

// Espera parar de digitar antes de disparar uma busca no servidor — evita
// uma requisição a cada tecla.
export function useDebounce(valor, atrasoMs = 300) {
  const [valorComAtraso, setValorComAtraso] = useState(valor);
  useEffect(() => {
    const timer = setTimeout(() => setValorComAtraso(valor), atrasoMs);
    return () => clearTimeout(timer);
  }, [valor, atrasoMs]);
  return valorComAtraso;
}
