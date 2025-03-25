﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Newtonsoft.Json;
using WebApiCursoVideo.Data;
using WebApiCursoVideo.Dto.Login;
using WebApiCursoVideo.Dto.Usuario;
using WebApiCursoVideo.Models;
using WebApiCursoVideo.Services.Auditoria;
using WebApiCursoVideo.Services.Senha;

namespace WebApiCursoVideo.Services.Usuario
{
    public class UsuarioService : IUsuarioInterface
    {

        private readonly AppDbContext _context;
        private readonly ISenhaInterface _senhaInterface;
        private readonly IMapper _mapper;
        private readonly IAuditoriaInterface _auditoriaInterface;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioService(AppDbContext context, ISenhaInterface senhaInterface, 
            IMapper mapper, 
            IAuditoriaInterface auditoriaInterface,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _senhaInterface = senhaInterface;
            _mapper = mapper;
            _auditoriaInterface = auditoriaInterface;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseModel<UsuarioModel>> BuscarUsuarioPorId(int id)
        {
            ResponseModel<UsuarioModel> response = new ResponseModel<UsuarioModel>();


            try
            {


                var usuario = await _context.Usuarios.FindAsync(id);

                if(usuario == null)
                {
                    response.Mensagem = "Usuário não localizado!";
                    return response;
                }

                response.Dados = usuario;
                response.Mensagem = "Usuário localizado!";

                return response;


            }
            catch (Exception ex) 
            {
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            }




        }
        public async Task<ResponseModel<UsuarioModel>> EditarUsuario(UsuarioEdicaoDto usuarioEdicaoDto)
        {
            ResponseModel<UsuarioModel> response = new ResponseModel<UsuarioModel>();

            try
            {

                UsuarioModel usuarioBanco = await _context.Usuarios.FindAsync(usuarioEdicaoDto.Id);

                if( usuarioBanco == null)
                {
                    response.Mensagem = "Usuario não localizado!";
                    return response;
                }


                var dadosAntes = JsonConvert.SerializeObject(usuarioBanco);


                usuarioBanco.Nome = usuarioEdicaoDto.Nome;
                usuarioBanco.Sobrenome = usuarioEdicaoDto.Sobrenome;
                usuarioBanco.Email = usuarioEdicaoDto.Email;
                usuarioBanco.Usuario = usuarioEdicaoDto.Usuario;
                usuarioBanco.DataAlteracao = DateTime.Now;

                _context.Update(usuarioBanco);
                await _context.SaveChangesAsync();

                response.Mensagem = "Usuário Editado com sucesso!";
                response.Dados = usuarioBanco;


                var dadosDepois = JsonConvert.SerializeObject(usuarioBanco);

                var usuarioId = _httpContextAccessor.HttpContext.User.Claims
                        .FirstOrDefault(c => c.Type == "UserId")?.Value;

                await _auditoriaInterface.RegistrarAuditoriaAsync("Atualização", usuarioId,
                    $"Antes: {dadosAntes}, Depois: {dadosDepois}");

                return response;

            }
            catch (Exception ex) 
            { 
                
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            
            }
        }
        public async Task<ResponseModel<List<UsuarioModel>>> ListarUsuarios()
        {

            ResponseModel<List<UsuarioModel>> response = new ResponseModel<List<UsuarioModel>>();

            try
            {
                var usuarios = await _context.Usuarios.ToListAsync();

                if(usuarios.Count() == 0)
                {
                    response.Mensagem = "Nenhum Usuário Cadastrado!";
                    return response;
                }

                response.Dados = usuarios;
                response.Mensagem = "Usuários Localizados com sucesso!";

                return response;

            }
            catch (Exception ex) 
            {
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            }





        }

        public async Task<ResponseModel<UsuarioModel>> Login(UsuarioLoginDto usuarioLoginDto)
        {
            ResponseModel<UsuarioModel> response = new ResponseModel<UsuarioModel>();

            try
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(userBanco => userBanco.Email == usuarioLoginDto.Email);

                if(usuario == null)
                {
                    response.Mensagem = "Credenciais inválidas!";
                    return response;
                }

                if (!_senhaInterface.VerificaSenhaHash(usuarioLoginDto.Senha, usuario.SenhaHash, usuario.SenhaSalt ))
                {
                    response.Mensagem = "Credenciais inválidas!";
                    return response;
                }

                var token = _senhaInterface.CriarToken(usuario);

                usuario.Token = token;

                _context.Update(usuario);
                await _context.SaveChangesAsync();


                response.Dados = usuario;
                response.Mensagem = "Usuário logado com sucesso!";

                return response;


            }catch(Exception ex)
            {
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<ResponseModel<UsuarioModel>> RegistrarUsuario(UsuarioCriacaoDto usuarioCriacaoDto)
        {

            ResponseModel<UsuarioModel> response = new ResponseModel<UsuarioModel>();

            try
            {

                if (!VerificaSeExisteEmailUsuarioRepetidos(usuarioCriacaoDto))
                {
                    response.Mensagem = "Email/Usuário já cadastrado!";
                    return response;
                }


                _senhaInterface.CriarSenhaHash(usuarioCriacaoDto.Senha, out byte[] senhaHash, out byte[] senhaSalt);

                UsuarioModel usuario = _mapper.Map<UsuarioModel>(usuarioCriacaoDto);

                usuario.SenhaHash = senhaHash;
                usuario.SenhaSalt = senhaSalt;
                usuario.DataCriacao = DateTime.Now;
                usuario.DataAlteracao = DateTime.Now;

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                response.Mensagem = "Usuário Cadastrado com sucesso!";
                response.Dados = usuario;

                return response;



            }
            catch (Exception ex)
            {
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            }
        }
        public async Task<ResponseModel<UsuarioModel>> RemoverUsuario(int id)
        {

            ResponseModel<UsuarioModel> response = new ResponseModel<UsuarioModel>();

            try
            {

                var usuario = await _context.Usuarios.FindAsync(id);

                if(usuario == null)
                {
                    response.Mensagem = "Usuário não localizado!";
                    return response;
                }


                _context.Remove(usuario);
                await _context.SaveChangesAsync();
                
                response.Mensagem = $"Usuário {usuario.Nome} Removido com sucesso!";


                var dadosAntes = JsonConvert.SerializeObject(usuario);

                var usuarioId = _httpContextAccessor.HttpContext.User.Claims
                        .FirstOrDefault(c => c.Type == "UserId")?.Value;

                await _auditoriaInterface.RegistrarAuditoriaAsync("Remoção", usuarioId,
                    $"Antes: {dadosAntes}");


                return response;


            }
            catch (Exception ex)
            {
                response.Mensagem = ex.Message;
                response.Status = false;
                return response;
            }
        }
        private bool VerificaSeExisteEmailUsuarioRepetidos(UsuarioCriacaoDto usuarioCriacaoDto)
        {
            var usuario = _context.Usuarios.FirstOrDefault(item => item.Email == usuarioCriacaoDto.Email || 
                                                                   item.Usuario == usuarioCriacaoDto.Usuario);

            if (usuario != null)
            {
                return false;
            }


            return true;
        }

    }
}
