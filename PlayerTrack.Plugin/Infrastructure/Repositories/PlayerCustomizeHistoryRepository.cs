﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerCustomizeHistoryRepository : BaseRepository
{
    public PlayerCustomizeHistoryRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public int UpdatePlayerId(int oldestPlayerId, int newPlayerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerCustomizeHistoryRepository.UpdatePlayerId(): {oldestPlayerId}, {newPlayerId}.");
        try
        {
            const string updateSql = "UPDATE player_customize_histories SET player_id = @newPlayerId WHERE player_id = @oldestPlayerId";

            var numberOfUpdatedRecords = Connection.Execute(updateSql, new { newPlayerId, oldestPlayerId });
            return numberOfUpdatedRecords;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to update playerIds from {oldestPlayerId} to {newPlayerId}.");
            return 0;
        }
    }

    public bool CreatePlayerCustomizeHistory(PlayerCustomizeHistory playerCustomizeHistory)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistory(): {playerCustomizeHistory}.");
        try
        {
            var historyDto = Mapper.Map<PlayerCustomizeHistoryDTO>(playerCustomizeHistory);
            SetCreateTimestamp(historyDto);
            const string sql = @"
                    INSERT INTO player_customize_histories (is_migrated, player_id, customize, created, updated)
                    VALUES (@is_migrated, @player_id, @Customize, @created, @updated)";
            Connection.Execute(sql, historyDto);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to create player customize history.", playerCustomizeHistory);
            return false;
        }
    }

    public void DeleteCustomizeHistory(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerCustomizeHistoryRepository.DeleteCustomizeHistory(): {playerId}.");
        try
        {
            const string sql = "DELETE FROM player_customize_histories WHERE player_id = @player_id";
            Connection.Execute(sql, new { player_id = playerId });
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete customize history for player id {playerId}.");
        }
    }

    public bool CreatePlayerCustomizeHistories(List<PlayerCustomizeHistory> playerCustomizeHistories)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistories(): {playerCustomizeHistories}.");
        using var transaction = Connection.BeginTransaction();
        try
        {
            const string sql = @"
                INSERT INTO player_customize_histories (player_id, customize, is_migrated, created, updated)
                VALUES (@player_id, @customize, @is_migrated, @created, @updated)";

            var playerCustomizeHistoryDTOs = playerCustomizeHistories.Select(history => new PlayerCustomizeHistoryDTO
            {
                player_id = history.PlayerId,
                customize = history.Customize,
                is_migrated = history.IsMigrated,
                created = history.Created,
                updated = history.Updated,
            }).ToList();

            Connection.Execute(sql, playerCustomizeHistoryDTOs, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to create player customize histories.");
            transaction.Rollback();
            return false;
        }
    }

    public List<PlayerCustomizeHistory> GetPlayerCustomizeHistories(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerCustomizeHistoryRepository.GetPlayerCustomizeHistories(): {playerId}.");
        try
        {
            const string sql = "SELECT * FROM player_customize_histories WHERE player_id = @player_id";
            var playerCustomizeHistories = Connection.Query<PlayerCustomizeHistoryDTO>(sql, new { player_id = playerId });
            return playerCustomizeHistories.Select(history => Mapper.Map<PlayerCustomizeHistory>(history)).ToList();
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to get player customize histories for player id {playerId}.");
            return new List<PlayerCustomizeHistory>();
        }
    }
}
