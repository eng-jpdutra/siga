import { useQuery } from "@tanstack/react-query";
import { obterToken } from "../api/client";

const BASE_URL = import.meta.env.VITE_API_URL;

// Uma tag <img> sozinha não consegue mandar o header Authorization — por
// isso a busca é feita via fetch (com o token) e o resultado vira uma URL
// de blob local, que aí sim serve como src de uma <img>/<Avatar>.
export function useFotoPerfil() {
  return useQuery({
    queryKey: ["minha-foto"],
    queryFn: async () => {
      const token = obterToken();
      const response = await fetch(`${BASE_URL}/api/auth/minha-foto`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (response.status === 404) return null;
      if (!response.ok) throw new Error("Não foi possível carregar a foto de perfil.");

      const blob = await response.blob();
      return URL.createObjectURL(blob);
    },
    staleTime: Infinity,
  });
}
