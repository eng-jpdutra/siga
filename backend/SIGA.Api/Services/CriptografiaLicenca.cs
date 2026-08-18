using System.Security.Cryptography;
using System.Text;

namespace SIGA.Api.Services;

// AES-GCM: criptografia reversível (ao contrário do hash de senha) — o
// administrador precisa conseguir ver a chave de volta pra usar numa
// reinstalação, por exemplo. A chave de criptografia é segredo (user-secrets
// em dev, variável de ambiente em produção), nunca fica no appsettings.
public class CriptografiaLicenca
{
    private readonly byte[] _chave;

    public CriptografiaLicenca(IConfiguration configuration)
    {
        var chaveBase64 = configuration["Licenca:ChaveCriptografia"];
        if (string.IsNullOrWhiteSpace(chaveBase64))
        {
            throw new InvalidOperationException(
                "Licenca:ChaveCriptografia não configurada. Em dev, rode: dotnet user-secrets set \"Licenca:ChaveCriptografia\" \"<chave-base64-de-32-bytes>\".");
        }

        _chave = Convert.FromBase64String(chaveBase64);
        if (_chave.Length != 32)
        {
            throw new InvalidOperationException("Licenca:ChaveCriptografia precisa ter exatamente 32 bytes (256 bits) depois de decodificada.");
        }
    }

    public string Criptografar(string textoPlano)
    {
        using var aes = new AesGcm(_chave, AesGcm.TagByteSizes.MaxSize);

        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var textoPlanoBytes = Encoding.UTF8.GetBytes(textoPlano);
        var textoCifradoBytes = new byte[textoPlanoBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(nonce, textoPlanoBytes, textoCifradoBytes, tag);

        // nonce + tag + cifrado, tudo junto num único valor base64.
        var resultado = new byte[nonce.Length + tag.Length + textoCifradoBytes.Length];
        nonce.CopyTo(resultado, 0);
        tag.CopyTo(resultado, nonce.Length);
        textoCifradoBytes.CopyTo(resultado, nonce.Length + tag.Length);

        return Convert.ToBase64String(resultado);
    }

    public string Descriptografar(string textoCriptografado)
    {
        var dados = Convert.FromBase64String(textoCriptografado);

        var nonceTam = AesGcm.NonceByteSizes.MaxSize;
        var tagTam = AesGcm.TagByteSizes.MaxSize;

        var nonce = dados.AsSpan(0, nonceTam);
        var tag = dados.AsSpan(nonceTam, tagTam);
        var textoCifrado = dados.AsSpan(nonceTam + tagTam);

        var textoPlanoBytes = new byte[textoCifrado.Length];
        using var aes = new AesGcm(_chave, tagTam);
        aes.Decrypt(nonce, textoCifrado, tag, textoPlanoBytes);

        return Encoding.UTF8.GetString(textoPlanoBytes);
    }
}
