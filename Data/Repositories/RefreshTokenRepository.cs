using UsersService.Data;
namespace UsersService.Data.Repositories;

public class RefreshTokenRepository 
{
    private readonly AppDBContext _dbContext;

    public RefreshTokenRepository(AppDBContext dBContext)
    {
        _dbContext = dBContext;
    }
}

