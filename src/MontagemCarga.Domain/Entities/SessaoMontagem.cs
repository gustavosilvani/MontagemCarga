using MontagemCarga.Domain.Enums;

namespace MontagemCarga.Domain.Entities;

public class SessaoMontagem
{
    public Guid Id { get; protected set; }
    public Guid EmbarcadorId { get; protected set; }
    public string OperadorId { get; protected set; } = string.Empty;
    public string OperadorNome { get; protected set; } = string.Empty;
    public Guid FilialId { get; protected set; }
    public Guid? EmpresaId { get; protected set; }
    public SituacaoSessaoMontagem Situacao { get; protected set; }
    public string ParametrosJson { get; protected set; } = "{}";
    public string PedidosJson { get; protected set; } = "[]";
    public string ResultadoJson { get; protected set; } = "{}";
    public string NumerosCarregamentoReservadosJson { get; protected set; } = "[]";
    public string CarregamentosCriadosJson { get; protected set; } = "[]";
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public DateTime? ProcessadaEmUtc { get; protected set; }
    public DateTime? PersistidaEmUtc { get; protected set; }
    public DateTime? CanceladaEmUtc { get; protected set; }

    protected SessaoMontagem()
    {
    }

    public SessaoMontagem(
        Guid embarcadorId,
        string operadorId,
        string operadorNome,
        Guid filialId,
        Guid? empresaId,
        string parametrosJson,
        string pedidosJson,
        string resultadoJson,
        string numerosCarregamentoReservadosJson)
    {
        Id = Guid.NewGuid();
        EmbarcadorId = embarcadorId;
        OperadorId = operadorId;
        OperadorNome = operadorNome;
        FilialId = filialId;
        EmpresaId = empresaId;
        ParametrosJson = parametrosJson;
        PedidosJson = pedidosJson;
        ResultadoJson = resultadoJson;
        NumerosCarregamentoReservadosJson = numerosCarregamentoReservadosJson;
        Situacao = SituacaoSessaoMontagem.Processada;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ProcessadaEmUtc = DateTime.UtcNow;
    }

    public void AtualizarEstado(
        Guid filialId,
        Guid? empresaId,
        string parametrosJson,
        string pedidosJson,
        string resultadoJson,
        string numerosCarregamentoReservadosJson)
    {
        EnsureNotCancelled();
        EnsureNotPersisted();

        FilialId = filialId;
        EmpresaId = empresaId;
        ParametrosJson = parametrosJson;
        PedidosJson = pedidosJson;
        ResultadoJson = resultadoJson;
        NumerosCarregamentoReservadosJson = numerosCarregamentoReservadosJson;
        Situacao = SituacaoSessaoMontagem.Processada;
        ProcessadaEmUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarcarAtiva()
    {
        EnsureNotCancelled();
        EnsureNotPersisted();
        Situacao = SituacaoSessaoMontagem.Ativa;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarcarPersistida(string carregamentosCriadosJson)
    {
        EnsureNotCancelled();
        CarregamentosCriadosJson = carregamentosCriadosJson;
        Situacao = SituacaoSessaoMontagem.Persistida;
        PersistidaEmUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        EnsureNotPersisted();
        Situacao = SituacaoSessaoMontagem.Cancelada;
        CanceladaEmUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureNotCancelled()
    {
        if (Situacao == SituacaoSessaoMontagem.Cancelada)
            throw new InvalidOperationException("Sessao cancelada nao pode ser alterada.");
    }

    private void EnsureNotPersisted()
    {
        if (Situacao == SituacaoSessaoMontagem.Persistida)
            throw new InvalidOperationException("Sessao persistida nao pode ser alterada.");
    }
}
