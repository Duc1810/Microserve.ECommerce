using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;

namespace Dashboard.Application.Commands;

public record RebuildMaterializedViewsCommand() : ICommand<RebuildResult>;