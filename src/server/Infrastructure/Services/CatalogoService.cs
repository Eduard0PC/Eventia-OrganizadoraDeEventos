using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class CatalogoService : ICatalogoService
{
    private readonly AppDbContext _db;

    public CatalogoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CatalogoEvento>> GetCatalogoEventosAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CatalogoEventos
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);
    }
}
