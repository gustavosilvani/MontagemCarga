namespace MontagemCarga.Domain.Interfaces;

public interface ITenantService
{
    Guid? ObterEmbarcadorIdAtual();
    string? ObterOperadorIdAtual();
    string? ObterNomeOperadorAtual();
    void DefinirEmbarcadorId(Guid embarcadorId);
}
