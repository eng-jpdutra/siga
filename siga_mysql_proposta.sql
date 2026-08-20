-- =====================================================================
-- SIGA — modelo de dados (documentação, não é a fonte executável)
--
-- Este arquivo tem DUAS partes bem separadas, resultado de uma conversa
-- de design mais longa:
--
--   PARTE 1 — o que realmente é do SIGA. É o schema atual (EF Core, ver
--   backend/SIGA.Api/Domain) mais UMA adição: local.vereador_id, pra
--   localizar o gabinete de um vereador. Só isso pertence a este projeto.
--
--   PARTE 2 — schema de referência de OUTRO sistema (cadastro de
--   vereadores, servidores, partidos, legislaturas...). O SIGA não
--   implementa nada disso — fica aqui só pra não perder o desenho da
--   conversa. `vereador` na Parte 1 é uma cópia local mínima (Id, Nome)
--   do que existir na tabela `vereador` da Parte 2.
--
-- Importar no MySQL Workbench: Database > Reverse Engineer... > apontar
-- pra este arquivo.
--
-- Convenção: nomes de tabela e coluna em português, snake_case, sem
-- acento — mesma convenção do restante do SIGA (ver CLAUDE.md).
-- =====================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================================
-- PARTE 1 — SIGA (schema real do projeto + a adição de vereador)
-- =====================================================================

-- Cópia local mínima do vereador cadastrado no outro sistema. Sem
-- mandato, sem partido, sem legislatura — só o suficiente pra apontar
-- "esse gabinete é desse vereador". `external_id` guarda o id de origem
-- no outro sistema, pra permitir sincronizar/atualizar depois.
CREATE TABLE vereador (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome          VARCHAR(120) NOT NULL,
  external_id   INT UNSIGNED NULL COMMENT 'id da tabela vereador no outro sistema',
  PRIMARY KEY (id),
  UNIQUE KEY uk_vereador_external_id (external_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Local físico onde um equipamento ou responsável fica. `vereador_id`
-- só é preenchido quando o local for o gabinete de um vereador — não
-- existe tabela `gabinete` separada, o volume de locais não justifica
-- (é um punhado de linhas, não vale o TPT que o inventário usa).
CREATE TABLE local (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome          VARCHAR(100) NOT NULL,
  descricao     VARCHAR(255) NULL,
  vereador_id   INT UNSIGNED NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_local_nome (nome),
  UNIQUE KEY uk_local_vereador (vereador_id),
  CONSTRAINT fk_local_vereador FOREIGN KEY (vereador_id) REFERENCES vereador (id)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Pessoa responsável por um ou mais equipamentos. Desativação é soft
-- delete (status = Inativo), igual ao restante do inventário.
CREATE TABLE responsavel (
  id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome         VARCHAR(120) NOT NULL,
  cargo        VARCHAR(100) NULL,
  local_id     INT UNSIGNED NULL,
  contato      VARCHAR(120) NULL,
  status       VARCHAR(20) NOT NULL DEFAULT 'Ativo',
  observacao   TEXT NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_responsavel_local FOREIGN KEY (local_id) REFERENCES local (id)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Nota fiscal — tabela-base, sem FK pra fora (equipamento/licença que
-- referenciam ela, nunca o contrário).
CREATE TABLE nota_fiscal (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  numero        VARCHAR(50) NOT NULL,
  fornecedor    VARCHAR(150) NULL,
  data_emissao  DATE NULL,
  valor         DECIMAL(12,2) NULL,
  arquivo_path  VARCHAR(255) NULL,
  observacao    TEXT NULL,
  PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Inventário de TI — herança TPT: equipamento é a base, computador /
-- impressora / dispositivo_rede são subtabelas com a mesma PK.
CREATE TABLE equipamento (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  tipo            VARCHAR(30) NOT NULL,
  patrimonio      VARCHAR(50) NULL,
  numero_serie    VARCHAR(100) NULL,
  marca           VARCHAR(100) NOT NULL,
  modelo          VARCHAR(100) NOT NULL,
  local_id        INT UNSIGNED NULL,
  responsavel_id  INT UNSIGNED NULL,
  nota_fiscal_id  INT UNSIGNED NULL,
  status          VARCHAR(20) NOT NULL DEFAULT 'Ativo',
  ano_aquisicao   SMALLINT NULL,
  garantia_ate    DATE NULL,
  observacao      TEXT NULL,
  criado_em       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atualizado_em   DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_equipamento_patrimonio (patrimonio),
  UNIQUE KEY uk_equipamento_numero_serie (numero_serie),
  KEY ix_equipamento_marca (marca),
  KEY ix_equipamento_modelo (modelo),
  KEY ix_equipamento_tipo (tipo),
  KEY ix_equipamento_status (status),
  CONSTRAINT fk_equipamento_local FOREIGN KEY (local_id) REFERENCES local (id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_equipamento_responsavel FOREIGN KEY (responsavel_id) REFERENCES responsavel (id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_equipamento_nota_fiscal FOREIGN KEY (nota_fiscal_id) REFERENCES nota_fiscal (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE computador (
  id                     INT UNSIGNED NOT NULL,
  subtipo                VARCHAR(20) NULL,
  sistema_operacional    VARCHAR(100) NULL,
  ram_gb                 SMALLINT NULL,
  armazenamento_gb       INT NULL,
  tipo_armazenamento     VARCHAR(10) NULL,
  processador            VARCHAR(100) NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_computador_equipamento FOREIGN KEY (id) REFERENCES equipamento (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE impressora (
  id                 INT UNSIGNED NOT NULL,
  tipo_impressao     VARCHAR(20) NULL,
  colorida           TINYINT(1) NOT NULL DEFAULT 0,
  conexao            VARCHAR(10) NULL,
  contador_paginas   INT NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_impressora_equipamento FOREIGN KEY (id) REFERENCES equipamento (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE dispositivo_rede (
  id                 INT UNSIGNED NOT NULL,
  subtipo            VARCHAR(20) NULL,
  endereco_ip        VARCHAR(45) NULL,
  endereco_mac       VARCHAR(17) NULL,
  num_portas         SMALLINT NULL,
  versao_firmware    VARCHAR(50) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_dispositivo_rede_mac (endereco_mac),
  CONSTRAINT fk_dispositivo_rede_equipamento FOREIGN KEY (id) REFERENCES equipamento (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Diário do equipamento — texto gerado de forma padronizada pela
-- aplicação no momento do lançamento (ver CLAUDE.md).
CREATE TABLE historico (
  id               INT UNSIGNED NOT NULL AUTO_INCREMENT,
  equipamento_id   INT UNSIGNED NOT NULL,
  tipo             VARCHAR(30) NOT NULL,
  data             DATE NOT NULL,
  descricao        TEXT NOT NULL,
  registrado_por   VARCHAR(120) NULL,
  registrado_em    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_historico_equipamento (equipamento_id),
  CONSTRAINT fk_historico_equipamento FOREIGN KEY (equipamento_id) REFERENCES equipamento (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Chave de licença gravada criptografada — nunca em texto puro.
CREATE TABLE licenca (
  id                     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  equipamento_id         INT UNSIGNED NOT NULL,
  produto                VARCHAR(150) NOT NULL,
  chave_criptografada    TEXT NOT NULL,
  tipo                   VARCHAR(10) NULL,
  observacao             TEXT NULL,
  nota_fiscal_id         INT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY ix_licenca_equipamento (equipamento_id),
  CONSTRAINT fk_licenca_equipamento FOREIGN KEY (equipamento_id) REFERENCES equipamento (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_licenca_nota_fiscal FOREIGN KEY (nota_fiscal_id) REFERENCES nota_fiscal (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Identidade e segurança (RBAC) — separada do inventário.
CREATE TABLE papel (
  id     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome   VARCHAR(50) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_papel_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE usuario (
  id                          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome                        VARCHAR(120) NOT NULL,
  nome_usuario                VARCHAR(60) NOT NULL,
  senha_hash                  TEXT NOT NULL,
  ativo                       TINYINT(1) NOT NULL DEFAULT 1,
  troca_senha_obrigatoria     TINYINT(1) NOT NULL DEFAULT 0,
  foto_path                   VARCHAR(255) NULL,
  responsavel_id              INT UNSIGNED NULL,
  falhas_acesso               INT NOT NULL DEFAULT 0,
  bloqueado_ate               DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_usuario_nome_usuario (nome_usuario),
  CONSTRAINT fk_usuario_responsavel FOREIGN KEY (responsavel_id) REFERENCES responsavel (id)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE usuario_papel (
  usuario_id  INT UNSIGNED NOT NULL,
  papel_id    INT UNSIGNED NOT NULL,
  PRIMARY KEY (usuario_id, papel_id),
  KEY ix_usuario_papel_papel (papel_id),
  CONSTRAINT fk_usuario_papel_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_usuario_papel_papel FOREIGN KEY (papel_id) REFERENCES papel (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sessão de longa duração — permite revogar acesso sem esperar o JWT
-- expirar (ex.: usuário desligado, dispositivo perdido).
CREATE TABLE refresh_token (
  id             INT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id     INT UNSIGNED NOT NULL,
  token_hash     VARCHAR(100) NOT NULL,
  expira_em      DATETIME NOT NULL,
  revogado_em    DATETIME NULL,
  ativo          TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uk_refresh_token_hash (token_hash),
  KEY ix_refresh_token_usuario (usuario_id),
  CONSTRAINT fk_refresh_token_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- =====================================================================
-- PARTE 2 — OUTRO SISTEMA (referência de design, o SIGA não implementa
-- nada daqui). `vereador` aqui é o cadastro completo — a tabela
-- `vereador` da Parte 1 é só uma cópia mínima dele.
-- =====================================================================

CREATE TABLE pessoa (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome          VARCHAR(120) NOT NULL,
  cpf           CHAR(11) NOT NULL,
  contato       VARCHAR(120) NULL,
  email         VARCHAR(150) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_pessoa_cpf (cpf)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE cargo (
  id     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome   VARCHAR(100) NOT NULL,
  ativo  TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uk_cargo_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE regime_contratacao (
  id     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome   VARCHAR(100) NOT NULL,
  ativo  TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uk_regime_contratacao_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE matricula (
  id     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  numero VARCHAR(45) NOT NULL,
  tipo   ENUM('Servidor', 'Vereador') NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_matricula_numero (numero)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Servidor da Câmara — a pessoa no papel de funcionário.
CREATE TABLE servidor (
  id         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  pessoa_id  INT UNSIGNED NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_servidor_pessoa (pessoa_id),
  CONSTRAINT fk_servidor_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoa (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Vínculo empregatício do servidor — histórico de contratos ao longo do
-- tempo (data_admissao/data_desligamento), cada um com seu cargo,
-- regime e matrícula.
CREATE TABLE vinculo (
  id                     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  servidor_id            INT UNSIGNED NOT NULL,
  cargo_id               INT UNSIGNED NOT NULL,
  regime_contratacao_id  INT UNSIGNED NOT NULL,
  matricula_id           INT UNSIGNED NULL,
  data_admissao          DATE NOT NULL,
  data_desligamento      DATE NULL,
  PRIMARY KEY (id),
  KEY ix_vinculo_servidor (servidor_id),
  UNIQUE KEY uk_vinculo_matricula (matricula_id),
  CONSTRAINT fk_vinculo_servidor FOREIGN KEY (servidor_id) REFERENCES servidor (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_vinculo_cargo FOREIGN KEY (cargo_id) REFERENCES cargo (id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_vinculo_regime FOREIGN KEY (regime_contratacao_id) REFERENCES regime_contratacao (id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_vinculo_matricula FOREIGN KEY (matricula_id) REFERENCES matricula (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE legislatura (
  id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome         VARCHAR(60) NOT NULL,
  data_inicio  DATE NOT NULL,
  data_fim     DATE NULL,
  ativo        TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE partido (
  id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
  sigla   VARCHAR(25) NOT NULL,
  nome    VARCHAR(100) NOT NULL,
  numero  SMALLINT UNSIGNED NULL,
  ativo   TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uk_partido_sigla (sigla)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Permanente por pessoa: reeleição não cria um vereador novo, cria um
-- mandato novo (ver tabela abaixo).
CREATE TABLE vereador (
  id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  pessoa_id         INT UNSIGNED NOT NULL,
  nome_legislativo  VARCHAR(100) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_vereador_pessoa (pessoa_id),
  CONSTRAINT fk_vereador_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoa (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE mandato (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  vereador_id     INT UNSIGNED NOT NULL,
  legislatura_id  INT UNSIGNED NOT NULL,
  matricula_id    INT UNSIGNED NULL,
  data_inicio     DATE NOT NULL,
  data_fim        DATE NULL,
  condicao        VARCHAR(45) NULL COMMENT 'Titular, Suplente, etc.',
  PRIMARY KEY (id),
  KEY ix_mandato_vereador (vereador_id),
  KEY ix_mandato_legislatura (legislatura_id),
  UNIQUE KEY uk_mandato_matricula (matricula_id),
  CONSTRAINT fk_mandato_vereador FOREIGN KEY (vereador_id) REFERENCES vereador (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_mandato_legislatura FOREIGN KEY (legislatura_id) REFERENCES legislatura (id)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT fk_mandato_matricula FOREIGN KEY (matricula_id) REFERENCES matricula (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Diário partidário do vereador: o partido atual é sempre a linha mais
-- recente sem data_desfiliacao, nunca um campo fixo.
CREATE TABLE filiacao_partidaria (
  id                  INT UNSIGNED NOT NULL AUTO_INCREMENT,
  vereador_id         INT UNSIGNED NOT NULL,
  partido_id          INT UNSIGNED NOT NULL,
  data_filiacao       DATE NOT NULL,
  data_desfiliacao    DATE NULL,
  PRIMARY KEY (id),
  KEY ix_filiacao_vereador (vereador_id),
  KEY ix_filiacao_partido (partido_id),
  CONSTRAINT fk_filiacao_vereador FOREIGN KEY (vereador_id) REFERENCES vereador (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_filiacao_partido FOREIGN KEY (partido_id) REFERENCES partido (id)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Histórico de assessoria: quem já trabalhou pra qual vereador, e
-- quando. Sem cargo aqui — o cargo do servidor já vem do vinculo dele;
-- repetir isso aqui seria duplicar uma informação que já existe.
-- Assessor que saiu e voltou = duas linhas com o mesmo servidor_id.
CREATE TABLE assessoria (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  servidor_id   INT UNSIGNED NOT NULL,
  vereador_id   INT UNSIGNED NOT NULL,
  data_inicio   DATE NOT NULL,
  data_fim      DATE NULL,
  PRIMARY KEY (id),
  KEY ix_assessoria_servidor (servidor_id),
  KEY ix_assessoria_vereador (vereador_id),
  CONSTRAINT fk_assessoria_servidor FOREIGN KEY (servidor_id) REFERENCES servidor (id)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_assessoria_vereador FOREIGN KEY (vereador_id) REFERENCES vereador (id)
    ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
