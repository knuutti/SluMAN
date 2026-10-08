state("SluMAN") {}

startup
{	
    settings.Add("EPISODE_1", false, "Episode 1");
    settings.Add("EPISODE_2", false, "Episode 2");
    settings.Add("EPISODE_3", false, "Episode 3");
    settings.Add("EPISODE_4", false, "Episode 4");
    settings.Add("EPISODE_5", false, "Episode 5");
    settings.Add("EPISODE_6", false, "Episode 6");
    settings.Add("EPISODE_7", false, "Episode 7");
    settings.Add("EPISODE_8", false, "Episode 8");
    settings.Add("ANY", false, "Any%");
}

init
{
    System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("racman-autosplitter");
    System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream();
    vars.reader = new System.IO.BinaryReader(stream);
    
    vars.reader.BaseStream.Position = 0;

    vars.splitPending = false;
    vars.splitPendingTime = DateTime.Now;

    vars.cairoStarted = false;
    vars.palaceStarted = false;

    current.isLoading = vars.reader.ReadUInt32();
    current.jobId = vars.reader.ReadUInt32();
    current.checkpointId = vars.reader.ReadUInt32();
    current.mapId = vars.reader.ReadUInt32();
    current.gameSpeed = vars.reader.ReadSingle();
    current.slyCharacterPointer = vars.reader.ReadUInt32();
    current.activeCharacterPointer = vars.reader.ReadUInt32();
    current.cameraFov = vars.reader.ReadSingle();
    current.cameraParameter = vars.reader.ReadSingle();
    current.pauseMenuState = vars.reader.ReadUInt32();
    current.parisStarted = vars.reader.ReadUInt32();
    current.rajanHealth = vars.reader.ReadUInt32();
    current.templeStarted = vars.reader.ReadUInt32();
    current.prisonStarted = vars.reader.ReadUInt32();
    current.castleStarted = vars.reader.ReadUInt32();
    current.trainStarted = vars.reader.ReadUInt32();
    current.sawmillStarted = vars.reader.ReadUInt32();
    current.blimpStarted = vars.reader.ReadUInt32();
    current.parisCinemaState = vars.reader.ReadUInt32();
}

update
{
    vars.reader.BaseStream.Position = 0;

    current.isLoading = vars.reader.ReadUInt32();
    current.jobId = vars.reader.ReadUInt32();
    current.checkpointId = vars.reader.ReadUInt32();
    current.mapId = vars.reader.ReadUInt32();
    current.gameSpeed = vars.reader.ReadSingle();
    current.slyCharacterPointer = vars.reader.ReadUInt32();
    current.activeCharacterPointer = vars.reader.ReadUInt32();
    current.cameraFov = vars.reader.ReadSingle();
    current.cameraParameter = vars.reader.ReadSingle();
    current.pauseMenuState = vars.reader.ReadUInt32();
    current.parisStarted = vars.reader.ReadUInt32();
    current.rajanHealth = vars.reader.ReadUInt32();
    current.templeStarted = vars.reader.ReadUInt32();
    current.prisonStarted = vars.reader.ReadUInt32();
    current.castleStarted = vars.reader.ReadUInt32();
    current.trainStarted = vars.reader.ReadUInt32();
    current.sawmillStarted = vars.reader.ReadUInt32();
    current.blimpStarted = vars.reader.ReadUInt32();
    current.parisCinemaState = vars.reader.ReadUInt32();
}

start
{

    if (vars.splitPending == true)
    {
        if (vars.splitPendingTime < DateTime.Now)
        {
            vars.splitPending = false;
            return true;
        }
        return false;
    }

    if (settings["ANY"] && vars.cairoStarted == false && current.cameraParameter == 49.0f && current.jobId == 1696) 
    { 
        vars.cairoStarted = true;
        vars.splitPending = true;
        vars.splitPendingTime = DateTime.Now.AddMilliseconds(133);
        return false; 
    }
    if (settings["EPISODE_1"] && current.parisStarted == 0 && current.parisCinemaState == 3 && current.mapId == 2 && current.activeCharacterPointer != 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_2"] && vars.palaceStarted == false && current.mapId == 8 && current.cameraFov == 1.0f && old.cameraFov == 0.95f && current.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        vars.palaceStarted = true;
        return true;
    }
    if (settings["EPISODE_3"] && current.templeStarted == 0 && current.mapId == 12 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_4"] && current.prisonStarted == 0 && current.mapId == 14 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_5"] && current.castleStarted == 0 && current.mapId == 17 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_6"] && current.trainStarted == 2 && current.mapId == 27 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_7"] && current.sawmillStarted == 0 && current.mapId == 32 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }
    if (settings["EPISODE_8"] && current.blimpStarted == 0 && current.mapId == 38 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
    {
        return true;
    }


    return false;
}

isLoading
{
    return current.isLoading != 3;
}

onReset
{
    vars.splitPending = false;
    vars.splitPendingTime = DateTime.Now;
    vars.cairoStarted = false;
    vars.palaceStarted = false;
}

split
{
    if (vars.splitPending == true)
    {
        if (vars.splitPendingTime < DateTime.Now)
        {
            vars.splitPending = false;
            return true;
        }
        return false;
    }

    if (settings["ANY"])
    {
        if (current.mapId == 0 && current.checkpointId == 1584 && current.pauseMenuState == 4 && old.pauseMenuState == 7 && current.gameSpeed == 1.0f)
        {
            // Cairo
            return true;
        }
        if (current.parisStarted == 0 && current.parisCinemaState == 3 && current.mapId == 2 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (vars.palaceStarted == false && current.mapId == 8 && current.cameraFov == 1.0f && old.cameraFov == 0.95f && current.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            vars.palaceStarted = true;
            return true;
        }
        if (current.templeStarted == 0 && current.mapId == 12 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (current.prisonStarted == 0 && current.mapId == 14 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (current.castleStarted == 0 && current.mapId == 17 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (current.trainStarted == 2 && current.mapId == 27 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (current.sawmillStarted == 0 && current.mapId == 32 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
        if (current.blimpStarted == 0 && current.mapId == 38 && current.activeCharacterPointer != 0 && old.activeCharacterPointer == 0 && current.pauseMenuState == 0)
        {
            return true;
        }
    }

    if (settings["EPISODE_1"] || settings["ANY"])
    { 
        if (current.checkpointId == 1793 && current.gameSpeed != old.gameSpeed)
        {
            // Satellite Sabotage
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1820 && old.activeCharacterPointer == 0 && current.activeCharacterPointer != 0 && current.gameSpeed == 0.0f)
        {
            // Breaking and Entering
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1632 && current.gameSpeed != old.gameSpeed)
        {
            // Bug Dimitri's Office
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1845 && current.gameSpeed != old.gameSpeed)
        {
            // Follow Dimitri
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1908 && current.gameSpeed == 0.0f && current.gameSpeed != old.gameSpeed)
        {
            // Waterpump Destruction
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1831 && old.checkpointId != 1831)
        {
            // Silence the Alarms
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1881 && current.gameSpeed != old.gameSpeed)
        {
            // Moonlight Rendezvous
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1871 && current.gameSpeed != old.gameSpeed)
        {
            // Theater Pickpocketing
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1898 && current.gameSpeed != old.gameSpeed)
        {
            // Disco Demolitions
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1953 && old.checkpointId != 1953)
        {
            // Operation: Thunderbeak
            return true;
        }
    }
    else if (settings["EPISODE_2"] || settings["ANY"])
    {
        if (current.checkpointId == 2018 && current.gameSpeed != old.gameSpeed)
        {
            // Recon The Ballroom
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2032 && current.gameSpeed != old.gameSpeed)
        {
            // Lower the Drawbridge
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2063 && current.gameSpeed != old.gameSpeed)
        {
            // Steal a Tuxedo
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2038 && current.gameSpeed != old.gameSpeed && current.activeCharacterPointer == 0 && current.cameraFov == 1.0f)
        {
            // Battle the Chopper
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2082 && current.gameSpeed != old.gameSpeed)
        {
            // Dominate the Dance Floor
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2090 && current.gameSpeed != old.gameSpeed)
        {
            // RC Bombing Run
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2105 && current.gameSpeed != old.gameSpeed)
        {
            // Elephant Rampage
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2054 && current.gameSpeed != old.gameSpeed)
        {
            // Boardroom Brawl
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2149 && old.checkpointId != 2149)
        {
            // OP: Hippo Drop
            return true;
        }
    }
    else if (settings["EPISODE_3"] || settings["ANY"])
    {
        if (current.checkpointId == 2205 && current.gameSpeed != old.gameSpeed)
        {
            // Spice Room Recon
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2233 && current.gameSpeed != old.gameSpeed)
        {
            // Elephant Rampage
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2212 && current.gameSpeed != old.gameSpeed)
        {
            // Waterbug Run
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2280 && current.gameSpeed != old.gameSpeed)
        {
            // Leading Rajan
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2306 && current.gameSpeed != old.gameSpeed)
        {
            // Blow the Dam
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2295 && current.gameSpeed != old.gameSpeed)
        {
            // Spice Grinder Destruction
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2262 && current.gameSpeed != old.gameSpeed)
        {
            // Neyla's Secret
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2315 && current.gameSpeed != old.gameSpeed)
        {
            // Rip Off the Ruby
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2354 && old.checkpointId != 2354 && current.rajanHealth == 0)
        {
            // OP: Wet Tiger
            return true;
        }
    }
    else if (settings["EPISODE_4"] || settings["ANY"])
    {
        if (current.checkpointId == 2426 && current.gameSpeed != old.gameSpeed)
        {
            // Eavesdrop on Contessa
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2444 && current.gameSpeed != old.gameSpeed)
        {
            // Train Hack
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2461 && old.checkpointId != 2461)
        {
            // Wall Bombing
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2528 && current.gameSpeed != old.gameSpeed)
        {
            // Code Capture
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1637 && current.gameSpeed != old.gameSpeed)
        {
            // Close to Contessa
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2471 && current.gameSpeed != old.gameSpeed)
        {
            // Big House Brawl
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2503 && current.gameSpeed != old.gameSpeed)
        {
            // Disguise Bridge
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2620 && current.gameSpeed != old.gameSpeed)
        {
            // OP: Trojan Tank
            return true;
        }
    }
    else if (settings["EPISODE_5"] || settings["ANY"])
    {
        if (current.checkpointId == 2659 && current.gameSpeed != old.gameSpeed)
        {
            // Know Your Enemy
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2705 && current.gameSpeed != old.gameSpeed)
        {
            // Kidnap the General
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2681 && current.gameSpeed != old.gameSpeed)
        {
            // Ghost Capture
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2698 && current.gameSpeed != old.gameSpeed && current.activeCharacterPointer == 0 && current.cameraFov == 0.8f)
        {
            // Mojo Trap Action
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2761 && current.gameSpeed != old.gameSpeed)
        {
            // Crypt Hack
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2733 && current.gameSpeed == 1.0f && current.gameSpeed != old.gameSpeed && current.mapId == 17)
        {
            // Stealing Voices
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2742 && current.gameSpeed != old.gameSpeed)
        {
            // Tank Showdown
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2835 && old.checkpointId != 2835)
        {
            // OP: High Road
            return true;
        }
    }
    else if (settings["EPISODE_6"] || settings["ANY"])
    {
        if (current.checkpointId == 2877 && current.gameSpeed != old.gameSpeed)
        {
            // Cabin Crimes
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2891 && current.gameSpeed == 0.0f && current.gameSpeed != old.gameSpeed)
        {
            // Spice in the Sky
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2929 && current.gameSpeed != old.gameSpeed && current.activeCharacterPointer == 0)
        {
            // A Friend in Need
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2907 && current.gameSpeed != old.gameSpeed)
        {
            // Ride the Ironhorse
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2938 && current.gameSpeed != old.gameSpeed)
        {
            // Aerial Assault
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2954 && current.gameSpeed != old.gameSpeed)
        {
            // Bearcub Kidnapping
            vars.splitPending = true;
        }
        else if (current.checkpointId == 2942 && current.gameSpeed != old.gameSpeed)
        {
            // Theft on the Rails
            vars.splitPending = true;
        }
        else if (current.checkpointId == 1644 && old.checkpointId != 1644)
        {
            // OP: Choo-Choo
            return true;
        }
    }
    else if (settings["EPISODE_7"] || settings["ANY"])
    {
        if (current.checkpointId == 3053 && current.gameSpeed != old.gameSpeed)
        {
            // Recon the Sawmill
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3126 && current.gameSpeed != old.gameSpeed)
        {
            // Laser Redirection
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3081 && current.gameSpeed != old.gameSpeed)
        {
            // Bearcave Bugging
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3094 && current.gameSpeed != old.gameSpeed)
        {
            // RC Combat Club
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3154 && current.gameSpeed != old.gameSpeed)
        {
            // Old Grizzle Face
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3144 && current.gameSpeed != old.gameSpeed)
        {
            // Lighthouse Break in
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3197 && current.gameSpeed != old.gameSpeed)
        {
            // Thermal Ride
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3186 && current.gameSpeed != old.gameSpeed)
        {
            // Boat Hack
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3301 && old.checkpointId != 3301)
        {
            // OP: Canada Games
            return true;
        }
    }
    else if (settings["EPISODE_8"] || settings["ANY"])
    {
        if (current.checkpointId == 3385 && current.gameSpeed != old.gameSpeed)
        {
            // Blimp HQ Recon
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3460 && current.gameSpeed != old.gameSpeed)
        {
            // Bentley/Murray Team Up
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3414 && current.gameSpeed != old.gameSpeed)
        {
            // Murray/Sly Tag Team
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3429 && current.gameSpeed != old.gameSpeed)
        {
            // Sly/Bentley Conspire
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3397 && current.gameSpeed != old.gameSpeed)
        {
            // Charged TNT Run
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3475 && current.gameSpeed != old.gameSpeed)
        {
            // Mega-Jump Job
            vars.splitPending = true;
        }
        else if (current.checkpointId == 3497 && old.checkpointId != 3497)
        {
            // Showdown with Clock-La
            return true;
        }
    }


    if (vars.splitPending == true) { vars.splitPendingTime = DateTime.Now.AddMilliseconds(1950);}
    
    return false;
}

reset
{

}