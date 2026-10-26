state("SluMAN") {}

// Bentley's Hackpack. SluMAN (Speedrun Mode, Autosplitter ticked) shares the game state through
// the "SluMAN-autosplitter" memory-mapped file, laid out as Sly 4's (see
// Sly4AutosplitterValues.cs); the goal values aren't filled in for the Hackpack. Every value is a
// little-endian 32-bit int.

startup
{
    settings.Add("START_LEAVE_HUB", true, "Start when a level is loaded from the hub");
    settings.Add("START_JOB", false, "Start when a job starts");
    settings.Add("SPLIT_JOB", true, "Split when a job ends without failing");
    settings.Add("SPLIT_HUB", false, "Split when a level returns to the hub");
    settings.Add("LOAD_REMOVAL", true, "Pause the timer during loads");

    // The hub's level, "FrontEnd".
    vars.HubLevelHash = 0xE4927E12u;

    vars.ReadState = (Action<dynamic>)((state) =>
    {
        vars.reader.BaseStream.Position = 0;
        state.loadPhase = vars.reader.ReadInt32();
        state.inGame = vars.reader.ReadInt32();
        state.levelHash = vars.reader.ReadUInt32();
        state.jobIndex = vars.reader.ReadInt32();
        state.jobFailed = vars.reader.ReadInt32();
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
    // The current level hash changes to the new level as its load starts.
    bool leftHub = old.levelHash == vars.HubLevelHash && current.levelHash != vars.HubLevelHash && current.levelHash != 0;
    if (settings["START_LEAVE_HUB"] && leftHub)
    {
        return true;
    }
    return settings["START_JOB"] && current.jobIndex >= 0 && old.jobIndex < 0;
}

isLoading
{
    return settings["LOAD_REMOVAL"] && current.loadPhase != 0;
}

split
{
    // A job that ends after failing is a retry or a quit, not a finish.
    bool jobEnded = old.jobIndex >= 0 && current.jobIndex != old.jobIndex && old.jobFailed == 0;
    if (settings["SPLIT_JOB"] && jobEnded)
    {
        return true;
    }
    bool backToHub = current.levelHash == vars.HubLevelHash && old.levelHash != vars.HubLevelHash && old.levelHash != 0;
    return settings["SPLIT_HUB"] && backToHub;
}
