using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MontagemCarga.Application.Commands.AgruparPedidos;
using MontagemCarga.Application.Commands.CarregamentoLifecycle;
using MontagemCarga.Application.Commands.CriarCarregamentos;
using MontagemCarga.Application.Commands.SessoesMontagem;
using MontagemCarga.Application.DTOs;
using MontagemCarga.Application.Queries.ListarCarregamentos;
using MontagemCarga.Application.Queries.ObterCarregamento;
using MontagemCarga.Application.Queries.SessoesMontagem;

namespace MontagemCarga.Api.Controllers;

[ApiController]
[Route("api/v1/montagem-carga")]
[Authorize]
public class MontagemCargaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MontagemCargaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Agrupa pedidos conforme parametros e retorna apenas um preview operacional, sem persistencia de sessao.
    /// O motor suporta fluxo deterministico e modos VRP conforme o payload informado.
    /// </summary>
    [HttpPost("agrupar")]
    [ProducesResponseType(typeof(AgruparResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgruparResponseDto>> Agrupar([FromBody] AgruparRequestDto request, CancellationToken cancellationToken)
    {
        var command = new AgruparPedidosCommand(request.Pedidos, request.Parametros);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cria carregamentos a partir de pedidos + parametros.
    /// Grupos sao opcionais e servem apenas para validar se o preview segue consistente.
    /// Nao ha suporte, nesta fase, para editar, cancelar ou reagrupar carregamentos ja persistidos.
    /// </summary>
    [HttpPost("carregamentos")]
    [ProducesResponseType(typeof(List<CarregamentoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CarregamentoResponseDto>>> CriarCarregamentos([FromBody] CriarCarregamentosRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CriarCarregamentosCommand(
            request.Grupos,
            request.Pedidos,
            request.Parametros,
            request.FilialId,
            request.EmpresaId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cria uma sessao persistida de montagem e devolve o resultado corrente do agrupamento.
    /// Esta sessao vira o workspace de operacao, reprocessamento e persistencia final.
    /// </summary>
    [HttpPost("sessoes")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> CriarSessao([FromBody] CriarSessaoMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var command = new CriarSessaoMontagemCommand(request.FilialId, request.EmpresaId, request.Pedidos, request.Parametros);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtem uma sessao persistida de montagem.
    /// </summary>
    [HttpGet("sessoes/{id:guid}")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> ObterSessao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ObterSessaoMontagemQuery(id), cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Atualiza a base da sessao e reprocessa o agrupamento corrente.
    /// </summary>
    [HttpPut("sessoes/{id:guid}")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> AtualizarSessao(Guid id, [FromBody] AtualizarSessaoMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var command = new AtualizarSessaoMontagemCommand(id, request.EmpresaId, request.Pedidos, request.Parametros);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Adiciona pedidos em uma sessao persistida e reprocessa a rota.
    /// </summary>
    [HttpPost("sessoes/{id:guid}/pedidos")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> AdicionarPedidosSessao(Guid id, [FromBody] AdicionarPedidosSessaoRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdicionarPedidosSessaoMontagemCommand(id, request.Pedidos), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove um pedido da sessao e reprocessa o workspace de montagem.
    /// </summary>
    [HttpDelete("sessoes/{id:guid}/pedidos/{codigoPedido}")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> RemoverPedidoSessao(Guid id, string codigoPedido, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoverPedidoSessaoMontagemCommand(id, codigoPedido), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reprocessa a sessao com os pedidos e parametros atuais.
    /// </summary>
    [HttpPost("sessoes/{id:guid}/reprocessar")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> ReprocessarSessao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReprocessarSessaoMontagemCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Persiste os carregamentos gerados pela sessao sem divergencia entre preview e resultado final.
    /// </summary>
    [HttpPost("sessoes/{id:guid}/persistir")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> PersistirSessao(Guid id, [FromBody] PersistirSessaoRequestDto? request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PersistirSessaoMontagemCommand(id, request?.EmpresaId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancela a sessao de montagem e interrompe novas alteracoes no workspace.
    /// </summary>
    [HttpPost("sessoes/{id:guid}/cancelar")]
    [ProducesResponseType(typeof(SessaoMontagemResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessaoMontagemResponseDto>> CancelarSessao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelarSessaoMontagemCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtem carregamento por ID.
    /// </summary>
    [HttpGet("carregamentos/{id:guid}")]
    [ProducesResponseType(typeof(CarregamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarregamentoResponseDto>> ObterCarregamento(Guid id, CancellationToken cancellationToken)
    {
        var query = new ObterCarregamentoQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Lista carregamentos persistidos com paginacao.
    /// </summary>
    [HttpGet("carregamentos")]
    [ProducesResponseType(typeof(ListarCarregamentosResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListarCarregamentosResult>> ListarCarregamentos([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = new ListarCarregamentosQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("carregamentos/{id:guid}/em-transito")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IniciarTransito(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new IniciarTransitoCarregamentoCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("carregamentos/{id:guid}/finalizar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new FinalizarCarregamentoCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("carregamentos/{id:guid}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelarCarregamentoCommand(id), cancellationToken);
        return NoContent();
    }
}
