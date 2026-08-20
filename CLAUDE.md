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
- **Estrutura de pastas em `src/`:** `api/` (um arquivo por recurso do backend, espelhando
  `Endpoints/*.cs`), `auth/`, `components/`, `hooks/`, `layouts/`, `pages/`, `theme/`.

### Backend
- C# .NET 8 (LTS), **Minimal APIs**.
- **Modularização:** `Program.cs` fica enxuto (pipeline, DI, auth, CORS). As rotas ficam
  em métodos de extensão por domínio (ex.: `Endpoints/EquipamentoEndpoints.cs` com
  `app.MapEquipamentoEndpoints()`). Cada grupo usa `MapGroup()` com prefixo e
  `RequireAuthorization()`.
- **DTOs explícitos** em todo contrato de API. Nunca expor entidades do EF nas rotas.
- **Enums como string no JSON** (`JsonStringEnumConverter` global em `Program.cs`) — evita
  que o front dependa do valor numérico do enum, que muda se a ordem mudar no C#.

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
- **Campos específicos por tipo ficam em `Detalhes` (JSON), não em subtabelas.**
  `equipamento` é uma tabela única para todos os tipos (computador, impressora, monitor,
  dispositivo de rede, nobreak, câmera, DVR/NVR, telefone IP...); os campos comuns
  (marca, modelo, patrimônio, local, status...) são colunas normais, e os campos
  específicos de cada tipo (RAM de um computador, polegadas de um monitor) ficam dentro
  da coluna `Detalhes`, um dicionário serializado como **texto JSON simples** (não
  `jsonb` do Postgres — ver "Código 100% agnóstico de banco" acima; a particularidade
  fica isolada na configuração do `Detalhes` no `SigaDbContext`, com um `ValueConverter`
  de/para `Dictionary<string, object?>`).
  - **Por quê não voltamos pra TPT (subtabela por tipo):** o parque de equipamentos tem
    muitos tipos e cresce com frequência; uma tabela nova por tipo faria o banco crescer
    sem parar. `Detalhes` resolve isso sem exigir migration a cada tipo novo.
  - **O "mapa" de quais campos cada tipo tem mora no código do frontend**
    (`CAMPOS_POR_TIPO` em `EquipamentosPage.jsx`), não em tabelas de metadados no banco —
    os tipos mudam pouco depois de definidos, então não compensa a complexidade de um
    catálogo dinâmico. **Regra de ouro: nunca renomear uma `chave` de `Detalhes` depois
    que já existem equipamentos daquele tipo gravados** — o rótulo exibido pode mudar à
    vontade, a chave interna não (senão os dados antigos ficam com uma chave "morta").
  - **Atributo que precisa ser único vira coluna real**, não fica em `Detalhes` (um JSON
    não garante unicidade). Hoje é o caso de `EnderecoMac` (índice único quando
    preenchido) e `EnderecoIp` (coluna comum, sem unicidade forçada) — usados só pelos
    tipos com placa de rede.
  - `Detalhes` **não entra em filtro de busca** (lista fechada de filtros do CLAUDE.md já
    cobre isso: só campos comuns são filtráveis hoje).
- `patrimonio`, `numero_serie` e `EnderecoMac` são únicos quando preenchidos, mas podem
  ficar vazios.
- **Sem exclusão física (soft delete):** nunca apagar de verdade. "Excluir" um item é
  mudar o `status` para `baixado`; consultas normais escondem os baixados.
- `historico` é o **diário do item** (manutenção, formatação, mudança de local). O texto
  das trocas é **gerado de forma padronizada pela aplicação** no momento do lançamento. A
  base guarda o estado atual; o `historico` guarda a trajetória.
  - Não existe cadastro de "responsável" por equipamento — só `local` (ex.: gabinete,
    setor). Já existiu uma tabela `responsavel` (pessoa vinculada a um equipamento);
    foi removida (migration `RemoveResponsavel`) porque a responsabilidade por um
    equipamento é resolvida por onde ele está (`local`), não por uma pessoa cadastrada à
    parte — mais simples e evita manter dois cadastros de gente (`responsavel` e, no
    futuro, `vereador`) fazendo papéis parecidos.
  - **Padrão de geração:** ao salvar uma edição, compara o valor antigo com o novo (ex.:
    `LocalId`) e, se mudou, insere automaticamente uma linha de
    `Historico` com texto padronizado (ex.: `Local alterado de "X" para "Y".`). Baixa e
    reativação também geram uma linha própria. `RegistradoPor` vem sempre do claim de
    nome do usuário autenticado no JWT, nunca de um campo digitado; `RegistradoEm` é
    `UtcNow` do servidor. Esse padrão deve se repetir para futuros tipos de ativo.
- `configuracao` é uma **anotação técnica em texto livre** por equipamento (senha de BIOS,
  passo a passo de uma config de rede etc.) — ao contrário do `historico`, pode ser
  editada e removida depois: não é um diário de eventos, é uma nota viva. Guardado em
  texto direto no banco (coluna `text`), sem arquivo em disco — simplifica backup e
  permite buscar por conteúdo.
- A `chave` de licença é gravada **criptografada com AES-GCM** (chave de 256 bits, vinda
  de `Licenca:ChaveCriptografia` em configuração/segredo) — nunca em texto puro. É
  criptografia reversível (não hash), porque o administrador precisa recuperar a chave
  original quando for reinstalar o software.
- O arquivo da nota fiscal fica no disco via o serviço `ArmazenamentoArquivos`; no banco
  guarda-se apenas o caminho relativo (`arquivo_path`). O nome salvo em disco é sempre um
  GUID novo + extensão original — nunca o nome enviado pelo usuário — para não permitir
  path traversal. Pasta raiz configurável (`Uploads:Diretorio`, padrão `uploads/`, fora do
  controle de versão).

---

## Identidade e segurança — login é do Portal, não do SIGA
O SIGA **não tem mais login, senha, foto ou cadastro de usuário próprios**. Isso tudo
foi removido de propósito: existe um sistema separado, o **Portal**
(`c:\Users\João Pedro\Documents\PORTAL`, ver `PORTAL/CLAUDE.md`), que é o login único do
ecossistema da Câmara. O SIGA é um dos sistemas que o Portal abre — não tem tabela
`usuario`, `papel` nem `usuario_papel` no banco (foram removidas na migration
`RemoveIdentidadePropria`).

- **Como alguém entra no SIGA:** faz login no Portal; o Portal mostra um card do SIGA (se
  a pessoa tiver acesso); ao clicar, o Portal manda a pessoa pra
  `{SIGA}/sso?token={jwt}` — o mesmo token que o Portal já emitiu. `pages/SsoPage.jsx`
  lê esse token da URL, guarda como se fosse a sessão local e redireciona pra `/`. Não
  existe mais `/login` no SIGA.
- **De quem é o token:** emitido pelo Portal, não pelo SIGA. `Jwt:Key`/`Issuer`/`Audience`
  do SIGA são **os mesmos** configurados no Portal — é isso que permite o SIGA validar o
  token sem nunca chamar o Portal numa requisição.
- **Papel dentro do SIGA:** o token carrega um claim `sistemaPapel` por sistema que a
  pessoa acessa (formato `"NomeDoSistema:NomePapel"`, ex.: `"SIGA:Administrador"`). O
  SIGA só se importa com os que começam com `"SIGA:"` —
  `Services/SistemaPapelClaimsTransformation.cs` promove isso pra um claim de role de
  verdade (`ClaimTypes.Role`) assim que o token é validado, então `RequireRole`/a política
  `SomenteAdministrador` continuam funcionando exatamente como antes, sem saber que o
  papel na origem não é local.
  - Nos endpoints, o nome de quem está logado ainda vem de `ClaimTypes.Name` — é esse
    nome que alimenta o `RegistradoPor` do histórico, sem mudança nenhuma aí.
- **Sessão no front:** token JWT e um objeto `{ nome, papeis }` (decodificado do próprio
  token, ver `auth/jwt.js`) ficam em `localStorage` (`siga_token`, `siga_usuario`).
  `AuthContext.receberToken()` é chamado só pela `SsoPage`; não existe mais um formulário
  de login local nem `entrar(usuario, senha)`. Sem sessão válida, `RotaProtegida` manda
  direto pro Portal (`VITE_PORTAL_URL`) — não tem pra onde mais ir dentro do próprio SIGA.
  O wrapper de `fetch` em `api/client.js` faz o mesmo no 401.

---

## Segurança (aplicação)
- **JWT** em toda a API, emitido pelo Portal — a autorização por papel lê os claims do
  token (depois da transformação acima), sem consultar banco nenhum a cada requisição.
- **RBAC:** rotas da API protegidas por papéis/políticas (`RequireAuthorization`); rotas
  do React protegidas por guarda de rota. 401 manda pro Portal (não tem reautenticação
  local); 403 mostra "sem permissão".
- **CORS restritivo** em produção.
- **Segredos nunca no controle de versão:** string de conexão, chave JWT (a mesma do
  Portal) e chave de criptografia das licenças ficam em variáveis de ambiente /
  user-secrets (dev).
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
por papel (hoje só Administrador/Consulta — granularidade maior seria decisão do
Portal, não do SIGA), refresh tokens com revogação, testes de integração com
Testcontainers, rate limiting e proteção anti-DDoS, cache distribuído, observabilidade
estruturada. Decisões arquiteturais relevantes podem ser registradas de forma curta em
`docs/adr/`.

### Domínio Legislativo (vereador, partido, assessor, gabinete) — futuro sistema separado
Cadastro de vereadores/partidos/assessores **não entra no banco do SIGA**. É um domínio
de negócio próprio (mandato, legislatura, filiação partidária), com dono e ciclo de vida
diferentes dos ativos de TI — mesma razão que levou a tirar login do SIGA e criar o
**Portal**: cada domínio é dono dos seus dados, os outros sistemas só consomem.

Hoje existe uma tabela `vereador` (Nome, Partido, Contato, `LocalId`, Ativo) e uma tela de
cadastro diretamente no SIGA — provisório, até o sistema Legislativo existir. `LocalId`
aponta pro gabinete que o vereador ocupa (a FK fica em `vereador`, não em `local`, porque
nem todo local é gabinete). `equipamento` **não** referencia `vereador` diretamente —
"quais equipamentos estão no gabinete do vereador X" é um JOIN pelo `local` em comum
(equipamento tem `LocalId`, vereador tem `LocalId`), sem precisar de vínculo direto.

Quando o sistema Legislativo existir de fato, o SIGA vai **trocar** esse cadastro próprio
por uma **cópia local sincronizada** (não mais editável aqui), sem chamada direta em
tempo real:
- O SIGA mantém uma tabela própria só de leitura (ex.: `vereador_cache` com `Id`, `Nome`,
  `Ativo`), sem FK de verdade pro banco do Legislativo — são bancos fisicamente separados.
- Essa cópia é atualizada por **webhook/evento** do Legislativo (ou sincronização
  periódica) quando um vereador muda — não por consulta ao vivo a cada requisição.
- **Por quê:** é o mesmo padrão de confiança já usado entre SIGA e Portal (o SIGA valida
  o JWT do Portal sem nunca chamar o Portal numa requisição) — cada sistema continua
  funcionando sozinho mesmo se o outro estiver fora do ar.
