namespace SIGA.Api.DTOs;

public record LoginRequest(string NomeUsuario, string Senha);

public record LoginResponse(string Token, DateTime ExpiraEm, string Nome, string[] Papeis);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
