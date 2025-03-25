using Microsoft.EntityFrameworkCore;
using WebApiCursoVideo.Data;
using WebApiCursoVideo.Models;

namespace WebApiCursoVideo.Services.Auditoria
{
    public class AuditoriaService : IAuditoriaInterface
    {
        private readonly AppDbContext _context;

        public AuditoriaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AuditoriaModel>> BuscarAuditorias()
        {
            var auditorias = await _context.Auditorias.OrderByDescending(a => a.Data).ToListAsync();
            return auditorias;
        }

        public async Task RegistrarAuditoriaAsync(string acao, string usuarioId, string dadosAlterados)
        {
            var auditoria = new AuditoriaModel
            {
                Acao = acao,
                UsuarioId = usuarioId,
                DadosAlterados = dadosAlterados
            };

            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync();
        }
    }
}
