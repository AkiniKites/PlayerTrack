﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

using Dalamud.DrunkenToad.Core;

public class EncounterRepository : BaseRepository
{
    public EncounterRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public Encounter? GetOpenEncounter()
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterRepository.GetOpenEncounter()");
        try
        {
            const string sql = @"
                                SELECT *
                                FROM encounters
                                WHERE ended = 0
                                ORDER BY created DESC
                                LIMIT 1";
            var encounterDTO = this.Connection.QueryFirstOrDefault<EncounterDTO>(sql);
            return encounterDTO == null ? null : this.Mapper.Map<Encounter>(encounterDTO);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to get open encounter where ended is 0.");
            return null;
        }
    }

    public List<Encounter>? GetAllEncounters()
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterRepository.GetAllEncounters()");
        try
        {
            const string sql = @"
                                    SELECT *
                                    FROM encounters
                                    ORDER BY created DESC";
            var encounterDTOs = this.Connection.Query<EncounterDTO>(sql).AsList();
            return this.Mapper.Map<List<Encounter>>(encounterDTOs);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to get all encounters.");
            return null;
        }
    }

    public List<Encounter>? GetAllOpenEncounters()
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterRepository.GetAllOpenEncounters()");
        try
        {
            const string sql = @"
                                SELECT *
                                FROM encounters
                                WHERE ended = 0
                                ORDER BY created DESC";
            var encounterDTOs = this.Connection.Query<EncounterDTO>(sql).AsList();
            return this.Mapper.Map<List<Encounter>>(encounterDTOs);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to get all open encounters where ended is 0.");
            return null;
        }
    }

    public bool UpdateEncounter(Encounter encounter)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterRepository.UpdateEncounter(), encounter: {encounter.Id}");
        try
        {
            var encounterDTO = this.Mapper.Map<EncounterDTO>(encounter);
            SetUpdateTimestamp(encounterDTO);
            const string sql = @"
                                UPDATE encounters
                                SET
                                    territory_type_id = @territory_type_id,
                                    ended = @ended,
                                    updated = @updated
                                WHERE id = @id";
            this.Connection.Execute(sql, encounterDTO);
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to update encounter.", encounter);
            return false;
        }
    }

    public Encounter? GetEncounter(int encounterId)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterRepository.GetEncounter(), encounterId: {encounterId}");
        try
        {
            const string sql = @"
                                SELECT *
                                FROM encounters
                                WHERE id = @id";
            var encounterDTO = this.Connection.QueryFirstOrDefault<EncounterDTO>(sql, new { id = encounterId });
            return encounterDTO == null ? null : this.Mapper.Map<Encounter>(encounterDTO);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get encounter by id {encounterId}.");
            return null;
        }
    }

    public int CreateEncounter(Encounter encounter)
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterRepository.CreateEncounter()");
        try
        {
            var encounterDTO = this.Mapper.Map<EncounterDTO>(encounter);
            SetCreateTimestamp(encounterDTO);
            const string insertSql = @"
        INSERT INTO encounters
        (
            created,
            updated,
            territory_type_id,
            ended
        )
        VALUES
        (
            @created,
            @updated,
            @territory_type_id,
            @ended
        ) RETURNING id";
            var newId = this.Connection.ExecuteScalar<int>(insertSql, encounterDTO);
            return newId;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to create and retrieve encounter based on created date.", encounter);
            return 0;
        }
    }

    public bool CreateEncounters(IEnumerable<Encounter> encounters)
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterRepository.CreateEncounters()");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
                INSERT INTO encounters
                (
                    created,
                    updated,
                    territory_type_id,
                    ended
                )
                VALUES
                (
                    @created,
                    @updated,
                    @territory_type_id,
                    @ended
                )";

            var encounterDTOs = encounters.Select(this.Mapper.Map<EncounterDTO>).ToList();
            this.Connection.Execute(sql, encounterDTOs, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to insert encounters batch.");
            transaction.Rollback();
            return false;
        }
    }

    public void DeleteEncountersWithRelations(List<int> currentBatch)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterRepository.DeleteEncountersWithRelations(): {string.Join(", ", currentBatch)}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string deletePlayerEncountersSql = "DELETE FROM player_encounters WHERE encounter_id IN @encounter_ids";
            this.Connection.Execute(deletePlayerEncountersSql, new { encounter_ids = currentBatch }, transaction);

            const string deleteEncountersSql = "DELETE FROM encounters WHERE id IN @encounter_ids";
            this.Connection.Execute(deleteEncountersSql, new { encounter_ids = currentBatch }, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            DalamudContext.PluginLog.Error(ex, $"Failed to delete encounters and their relations for EncounterIDs {string.Join(", ", currentBatch)}.");
        }
    }
}
