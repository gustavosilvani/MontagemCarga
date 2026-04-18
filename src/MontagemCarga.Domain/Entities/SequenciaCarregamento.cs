namespace MontagemCarga.Domain.Entities;

public class SequenciaCarregamento
{
    public Guid FilialId { get; protected set; }
    public int UltimoNumero { get; protected set; }

    protected SequenciaCarregamento()
    {
    }

    public SequenciaCarregamento(Guid filialId, int ultimoNumero)
    {
        FilialId = filialId;
        UltimoNumero = ultimoNumero;
    }
}
