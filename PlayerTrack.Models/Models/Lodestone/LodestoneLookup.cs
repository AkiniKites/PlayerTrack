﻿namespace PlayerTrack.Models;

public class LodestoneLookup
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public int PlayerId { get; set; }

    public string PlayerName { get; init; } = string.Empty;

    public uint WorldId { get; init; }
    
    public string UpdatedPlayerName { get; set; } = string.Empty;
    
    public uint UpdatedWorldId { get; set; }

    public uint LodestoneId { get; private set; }

    public int FailureCount { get; set; }
    
    public bool IsDone { get; private set; }
    
    public int? PrerequisiteLookupId { get; set; }

    public LodestoneStatus LodestoneStatus { get; private set; } = LodestoneStatus.Unverified;

    public LodestoneLookupType LodestoneLookupType { get; init; } = LodestoneLookupType.Batch;

    public void SetLodestoneStatus(LodestoneStatus lodestoneStatus)
    {
        this.LodestoneStatus = lodestoneStatus;
        this.IsDone = this.LodestoneStatus is 
            LodestoneStatus.Verified or 
            LodestoneStatus.Banned or 
            LodestoneStatus.NotApplicable or 
            LodestoneStatus.Cancelled or
            LodestoneStatus.Unavailable;
    }
    
    public void SetLodestoneId(uint lodestoneId)
    {
        if (lodestoneId == 0 || this.LodestoneId != 0) return;
        this.LodestoneId = lodestoneId;
    }
    
    public void UpdateLookupAsFailed(bool allowRetry)
    {
        this.FailureCount++;

        if (this.FailureCount < 4 && allowRetry)
        {
            this.SetLodestoneStatus(LodestoneStatus.Failed);
        }
        else
        {
            this.SetLodestoneStatus(LodestoneStatus.Banned);
        }
    }

    public void UpdateLookupAsUnavailable()
    {
        this.SetLodestoneStatus(LodestoneStatus.Unavailable);
    }
    
    public void UpdateLookupAsInvalid()
    {
        this.SetLodestoneStatus(LodestoneStatus.Invalid);
    }

    public void UpdateLookupAsSuccess(LodestoneResponse response, LodestoneStatus status)
    {
        this.UpdatedPlayerName = response.PlayerName;
        this.UpdatedWorldId = response.WorldId;
        this.SetLodestoneId(response.LodestoneId);
        this.SetLodestoneStatus(status);
    }
}
