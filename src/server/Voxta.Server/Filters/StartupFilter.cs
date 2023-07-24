﻿using Voxta.Abstractions.Repositories;
using Voxta.Characters.Samples;
using Voxta.Data.LiteDB;

namespace Voxta.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly LiteDBMigrations _migrations;
    private readonly ICharacterRepository _charactersRepository;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            await _migrations.MigrateAsync();
            
            await _charactersRepository.SaveCharacterAsync(Kally.Create());
            await _charactersRepository.SaveCharacterAsync(Melly.Create());
            await _charactersRepository.SaveCharacterAsync(Kate.Create());
            await _charactersRepository.SaveCharacterAsync(Test.Create());
        }).Wait();
        
        return next;
    }
}