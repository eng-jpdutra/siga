# SIGA — Sistema Integrado de Gestão de Ativos

Sistema de inventário de ativos da Câmara Municipal. Começa pela TI (computadores,
notebooks, impressoras, dispositivos de rede e licenças) e deve poder crescer, no
futuro, para outros tipos de ativo (mobiliário, veículos, contratos, etc.).

O responsável está começando na programação e mantém o projeto sozinho.
**Explique as decisões** e **prefira o caminho mais claro e sustentável** — sem abrir
mão da segurança. Ao introduzir um padrão novo, diga em uma frase por que ele é
necessário.

## Idioma
- Interface, mensagens ao usuário e comentários de código em **português do Brasil**.
- Nomes de tabelas e colunas seguem o que já está no modelo (em português, sem acento).

---

## Stack

### Frontend
- React.js + Vite, Material UI (v5+), React Router DOM (v6).
- **TanStack Query** para todo estado de servidor (`useQuery` / `useMutation`); nada de
  `useEffect + fetch` manual para dados de API.
- Listagens com **MUI DataGrid Community**, sempre com `paginationMode="server"` e
  `disableColumnFilter`. A filtragem fica numa barra de ferramentas externa (server-side).
- Segredos via `import.meta.env.VITE_...` — nunca no código.

### Backend
- C# .NET 8 (LTS), **Minimal APIs**.
- **Modularização:** `Program.cs` fica enxuto (pipeline, DI, auth, CORS). As rotas ficam
  em métodos de extensão por domínio (ex.: `Endpoints/EquipamentoEndpoints.cs` com
  `app.MapEquipamentoEndpoints()`). Cada grupo usa `MapGroup()` com prefixo e
  `RequireAuthorization()`.
- **DTOs explícitos** em todo contrato de API. Nunca expor entidades do EF nas rotas.

### Persistência
- **Entity Framework Core 8**, abordagem **Code First + Migrations**. As entidades são
  definidas em C# e o EF gera/evolui o banco (`dotnet ef migrations add ...` /
  `dotnet ef database update`).
- **Desenvolvimento: SQLite** (`banco.db`). **Produção: PostgreSQL.**
- **Código 100% agnóstico de banco** (para SQLite e Postgres se comportarem igual):
  - Sem SQL bruto (`FromSqlRaw` / `ExecuteSqlRaw`); só LINQ traduzível pelo provedor.
  - Sem tipos específicos de provedor (`jsonb`, `citext`, UUID nativo) no modelo; usar
    tipos portáveis do C# e isolar particularidades na configuração do DbContext.
  - Comparação de texto normalizada (`ToLower()` dos dois lados).
- O `siga_mysql.sql` e o diagrama do Workbench permanecem como **documentação do modelo**
  — a fonte executável agora são as entidades + migrations.
- Leituras usam `AsNoTracking()`. Listagens paginadas no banco (`Skip()`/`Take()`),
  retornando `PagedResult<T> { Items, TotalCount, Page, PageSize }`.

---

## Modelo de dados — inventário (regras do SIGA)
- **Herança em tabelas:** `equipamento` é a base; `computador`, `impressora` e
  `dispositivo_rede` são subtabelas 1‑para‑1 (a PK delas é também FK para `equipamento`).
  No EF, mapear como TPT (table-per-type). Ao criar/remover um item, manter a base e a
  subtabela em sincronia.
- `patrimonio` e `numero_serie` são únicos quando preenchidos, mas podem ficar vazios.
- **Sem exclusão física (soft delete):** nunca apagar de verdade. "Excluir" um item é
  mudar o `status` para `baixado`; consultas normais escondem os baixados.
- `historico` é o **diário do item** (manutenção, formatação, mudança de local, troca de
  responsável). O texto das trocas é **gerado de forma padronizada pela aplicação** no
  momento do lançamento. A base guarda o estado atual; o `historico` guarda a trajetória.
- A `chave` de licença é gravada **criptografada** — nunca em texto puro.
- O arquivo da nota fiscal fica no disco; no banco guarda-se apenas o caminho
  (`arquivo_path`).

---

## Modelo de dados — identidade e segurança (RBAC)
Tabelas de acesso, separadas do inventário:
- `usuario` — conta de login: nome, email (único), `senha_hash`, `ativo`,
  `responsavel_id` (FK **opcional** para RESPONSAVEL, quando a pessoa também for
  responsável por ativos).
- `papel` — perfis de acesso (ex.: Administrador, Operador, Consulta).
- `usuario_papel` — vínculo N‑para‑N entre usuário e papel.
- (Opcional, só quando precisar de granularidade fina) `permissao` + `papel_permissao`.
  Começar apenas com papéis.

Regra inegociável: a senha é guardada **apenas como hash** (bcrypt ou Argon2), nunca em
texto puro. Usuário desativado é `ativo = false` (soft delete, como no inventário).

---

## Segurança (aplicação)
- **JWT** em toda a API; o front envia o token no header `Authorization`. No login, a API
  valida email + `senha_hash` e emite um JWT com os **papéis** do usuário como claims; a
  autorização por papel lê os claims do token, sem consultar o banco a cada requisição.
- **RBAC:** rotas da API protegidas por papéis/políticas (`RequireAuthorization`); rotas
  do React protegidas por guardas de rota. 401 dispara reautenticação; 403 mostra
  "sem permissão".
- **CORS restritivo** em produção.
- **Segredos nunca no controle de versão:** string de conexão, chave JWT e chave de
  criptografia das licenças ficam em variáveis de ambiente / user-secrets (dev).
- **Fail fast:** validar entradas e retornar `400` cedo; teto de itens por página (ex.: 100);
  nunca expor stack trace — usar `Results.Problem()` para falhas sistêmicas.

---

## Busca e filtros
- **Lista fechada de filtros por tela:** cada tela declara em tempo de projeto os campos
  filtráveis. Nada de filtro genérico sobre colunas arbitrárias.
- **Filtros tipados no handler** (ex.: `string? nome, int? categoriaId, bool? ativo`),
  aplicados por `Where()` condicional sobre `IQueryable` (filtro vazio não entra na query).
- Colunas expostas como filtro/ordenação devem ter **índice** criado na migration.

---

## Identidade visual
Paleta institucional da Câmara. Definir num **tema central do Material UI** (`createTheme`)
e consumir a partir dele — nunca hexadecimais soltos nos componentes. Valores aproximados
a partir de uma referência; refinar com conta-gotas se necessário.
- Verde institucional — fundo escuro, cabeçalhos, cor primária: `#17352E`
- Âmbar — destaque, estados ativos, badges (usar com moderação): `#C4862E`
- Creme — fundo das áreas claras / superfície: `#ECEAE2`
- Branco — cartões: `#FFFFFF`
- Texto sobre escuro (creme claro): `#F3F1EA` — secundário (verde suave): `#7E9C8D`
- Texto sobre claro (tinta): `#1C2A25`

Uso: cabeçalho em verde institucional com texto creme; conteúdo em creme/branco; âmbar só
para destaque e estados ativos. Sentence case, sem caixa-alta decorativa.

---

## Convenções
- Código claro e comentado. Ao alterar o modelo, ajustar as entidades e gerar a migration
  na mesma mudança.

## Evolução (YAGNI — não implementar agora, mas não bloquear)
Fora do MVP de propósito; introduzir só quando houver necessidade real: permissões finas
(`permissao`/`papel_permissao`), refresh tokens com revogação, testes de integração com
Testcontainers, rate limiting e proteção anti-DDoS, cache distribuído, observabilidade
estruturada. Decisões arquiteturais relevantes podem ser registradas de forma curta em
`docs/adr/`.
