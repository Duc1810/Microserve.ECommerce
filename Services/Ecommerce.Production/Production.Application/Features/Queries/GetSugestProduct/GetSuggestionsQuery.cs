using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Features.Queries.GetSugestproduct;

public record GetSuggestionsQuery(string Prefix, int Limit = 10) : IQuery<Result<List<string>>>;

