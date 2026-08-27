using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.Entities;

namespace Server.Core.Interfaces;

public interface ICatalogoService
{
    Task<List<CatalogoEvento>> GetCatalogoEventosAsync(CancellationToken cancellationToken = default);
}
