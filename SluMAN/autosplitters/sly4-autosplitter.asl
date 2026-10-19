state("SluMAN") {}

// Sly Cooper: Thieves in Time. SluMAN (Speedrun Mode, Autosplitter ticked) shares the game state
// through the "SluMAN-autosplitter" memory-mapped file; see Sly4AutosplitterValues.cs for the
// layout. Every value is a little-endian 32-bit int.

startup
{
    settings.Add("START_PROLOGUE", true, "Start when the Prologue job starts");
    settings.Add("SPLIT_JOB", true, "Split when a job's last checkpoint is completed");
    settings.Add("SPLIT_GOAL", false, "Split on every checkpoint completed");
    settings.Add("LOAD_REMOVAL", true, "Pause the timer during loads");

    vars.ReadState = (Action<dynamic>)((state) =>
    {
        vars.reader.BaseStream.Position = 0;
        state.loadPhase = vars.reader.ReadInt32();
        state.inGame = vars.reader.ReadInt32();
        state.levelHash = vars.reader.ReadUInt32();
        state.jobIndex = vars.reader.ReadInt32();
        state.jobFailed = vars.reader.ReadInt32();
        state.goalIndex = vars.reader.ReadInt32();
        state.goalCount = vars.reader.ReadInt32();
    });
}

init
{
    System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("SluMAN-autosplitter");
    System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream();
    vars.reader = new System.IO.BinaryReader(stream);
    vars.ReadState(current);
}

update
{
    vars.ReadState(current);
}

start
{
    // The Prologue is the first job in the game's job list.
    return settings["START_PROLOGUE"] && current.jobIndex == 0 && old.jobIndex != 0;
}

isLoading
{
    return settings["LOAD_REMOVAL"] && current.loadPhase != 0;
}

split
{
    // The current goal is the first one not done; it equals the goal count once all are done.
    // Only count it moving forward within the same job, so starting another job doesn't split.
    bool sameJob = current.jobIndex >= 0 && current.jobIndex == old.jobIndex && current.goalCount == old.goalCount;
    bool advanced = sameJob && old.goalIndex >= 0 && current.goalIndex > old.goalIndex;
    if (!advanced)
    {
        return false;
    }

    if (settings["SPLIT_GOAL"])
    {
        return true;
    }
    return settings["SPLIT_JOB"] && current.goalCount > 0 && current.goalIndex >= current.goalCount;
}
