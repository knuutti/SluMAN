state("SluMAN") {}

startup
{	
    settings.Add("ANY", true, "Any%");
    settings.Add("SKIPS", false, "Split after World Skips", "ANY");
}

init
{
    System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("racman-autosplitter");
    System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream();
    vars.reader = new System.IO.BinaryReader(stream);
    
    vars.reader.BaseStream.Position = 0;
    vars.runStarted = 0;
    vars.w3KeyCounter = 0;
    vars.splitAfterLoad = false;

    vars.asaState = 0;
    vars.raleighState = 0;
    vars.arsState = 0;
    vars.muggshotState = 0;
    vars.rubyState = 0;
    vars.apaState = 0;
    vars.pandaState = 0;
    vars.adrState = 0;

    current.levelId = vars.reader.ReadUInt32();
    current.worldId = vars.reader.ReadUInt32();
    current.loadingState = vars.reader.ReadUInt32();
    current.w3Keys = vars.reader.ReadUInt32();
    current.charmsCount = vars.reader.ReadUInt32();
    current.livesCount = vars.reader.ReadUInt32();
    current.coinCount = vars.reader.ReadUInt32();
    current.transitionState = vars.reader.ReadUInt32();
    current.clockwerkHealth = vars.reader.ReadUInt32();
}

update
{
    vars.reader.BaseStream.Position = 0;

    current.levelId = vars.reader.ReadUInt32();
    current.worldId = vars.reader.ReadUInt32();
    current.loadingState = vars.reader.ReadUInt32();
    current.w3Keys = vars.reader.ReadUInt32();
    current.charmsCount = vars.reader.ReadUInt32();
    current.livesCount = vars.reader.ReadUInt32();
    current.coinCount = vars.reader.ReadUInt32();
    current.transitionState = vars.reader.ReadUInt32();
    current.clockwerkHealth = vars.reader.ReadUInt32();
}

start
{
    if (current.levelId == 3 && current.worldId == 0 && current.transitionState == 1 && vars.runStarted == 0)
    {
        if (current.charmsCount == 0 && current.livesCount == 5 && current.coinCount == 0)
        {
            // Extra checks
            vars.runStarted = 1;
            return true;
        }
    }
}

isLoading
{

}

onReset
{
    vars.runStarted = 0;
    vars.w3KeyCounter = 0;
    vars.splitAfterLoad = false;
    vars.asaState = 0;
    vars.raleighState = 0;
    vars.arsState = 0;
    vars.muggshotState = 0;
    vars.rubyState = 0;
    vars.apaState = 0;
    vars.adrState = 0;
}

split
{

    if (current.levelId == 1 && current.worldId == 1 && vars.asaState == 0)
    {
        vars.asaState = 1;
        vars.splitAfterLoad = true;
    }

    if (current.levelId == 8 && current.worldId == 1 && vars.raleighState == 0 && old.levelId == 1 && old.worldId == 1)
    {
        vars.raleighState = 1;
        if (settings["SKIPS"])
        {
            vars.splitAfterLoad = true;
        }
    }

    // A Rocky Start
    if (current.levelId == 1 && current.worldId == 2 && vars.arsState == 0)
    {
        vars.arsState = 1;
        vars.splitAfterLoad = true;
    }

    // W2 Skip
    if (current.levelId == 8 && current.worldId == 2 && vars.muggshotState == 0 && old.levelId == 1 && old.worldId == 2)
    {
        vars.muggshotState = 1;
        if (settings["SKIPS"])
        {
            vars.splitAfterLoad = true;
        }
    }

    // W3 Levels
    if (current.levelId == 1 && current.worldId == 3 && vars.w3KeyCounter < current.w3Keys)
    {
        vars.w3KeyCounter = current.w3Keys;
        vars.splitAfterLoad = true;
    }

    // Enter Ruby
    if (current.levelId == 8 && current.worldId == 3 && vars.rubyState == 0 && old.levelId == 1 && old.worldId == 3)
    {
        vars.rubyState = 1;
    }

    // A Perilous Ascent
    if (current.levelId == 1 && current.worldId == 4 && vars.apaState == 0)
    {
        vars.apaState = 1;
        vars.splitAfterLoad = true;
    }

    // W2 Skip
    if (current.levelId == 8 && current.worldId == 4 && vars.pandaState == 0 && old.levelId == 1 && old.worldId == 4)
    {
        vars.pandaState = 1;
        if (settings["SKIPS"])
        {
            vars.splitAfterLoad = true;
        }
    }

    // Splits at Hideout after bosses
    if (current.levelId == 4 && current.worldId == 0 && old.worldId != 0)
    {
        if (vars.raleighState == 1)
        {
            vars.raleighState = 2;
            vars.splitAfterLoad = true;
        }
        else if (vars.muggshotState == 1)
        {
            vars.muggshotState = 2;
            vars.splitAfterLoad = true;
        }
        else if (vars.rubyState == 1)
        {
            vars.rubyState = 2;
            vars.splitAfterLoad = true;
        }
        else if (vars.pandaState == 1)
        {
            vars.pandaState = 2;
            vars.splitAfterLoad = true;
        }
    }

    // W5 Splits
    if (current.worldId == 5)
    {
        if (current.levelId == 2 && old.levelId == 0)
        {
            vars.splitAfterLoad = true;
        }
        if (current.levelId == 3 && old.levelId == 2 && vars.adrState == 0)
        {
            vars.adrState = 1;
            vars.splitAfterLoad = true;
        }
        if (current.levelId == 5 && old.levelId == 3)
        {
            vars.splitAfterLoad = true;
        }
        if (current.levelId == 6 && old.levelId == 5)
        {
            vars.splitAfterLoad = true;
        }
        if (current.levelId == 8 && old.levelId == 6)
        {
            vars.splitAfterLoad = true;
        }
    }

    // Clockwerk
    if (current.levelId == 8 && current.worldId == 5 && current.clockwerkHealth == 0 && old.clockwerkHealth > 0)
    {
        return true;
    }

    // SPLIT TIMING LOGIC BELOW 

    if (vars.splitAfterLoad && current.loadingState == 0 && old.loadingState == 1)
    {
        vars.splitAfterLoad = false;
        return true;
    }

}

reset
{

}