using System;
using System.Collections.Generic;

namespace TeamService.Core.DTOs;

public class PagedTeamsResponseDto
{
    public IEnumerable<GetTeamResponseDto> Items { get; set; } = new List<GetTeamResponseDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
