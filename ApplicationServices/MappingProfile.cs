using AutoMapper;
using UsersService.Dtos.ResponseDtos;
using UsersService.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserInfoResponseDto>();
    }
}