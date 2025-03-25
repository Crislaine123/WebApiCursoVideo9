using AutoMapper;
using WebApiCursoVideo.Dto.Usuario;
using WebApiCursoVideo.Models;

namespace WebApiCursoVideo.Profiles
{
    public class ProfilesAutoMapper : Profile
    {

        public ProfilesAutoMapper()
        {
            CreateMap<UsuarioCriacaoDto, UsuarioModel>().ReverseMap();
        }

    }
}
