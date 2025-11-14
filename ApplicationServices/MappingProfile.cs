using AutoMapper;
using AuthService.Dtos.ResponseDtos;
using AuthService.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserInfoResponseDto>();
    }
}