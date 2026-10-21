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
    System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("SluMAN-autosplitter");
    System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream();
    vars.reader = new System.IO.BinaryReader(stream);
    
    vars.reader.BaseStream.Position = 0;

    vars.splitPending = false;
    vars.splitPendingTime = DateTime.Now;
    // Jobs (by their last checkpoint) already split this run: each splits once, even if the
    // game speed changes again while the checkpoint stays.
    vars.splitDone = new System.Collections.Generic.HashSet<int>();
    vars.lastSpeedChange = DateTime.MinValue;
    vars.speedChangedRecently = false;
    // How much earlier than a job's last checkpoint its speed change may arrive and still count.
    vars.speedRaceMs = 100;

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

    if (current.gameSpeed != old.gameSpeed)
    {
        vars.lastSpeedChange = DateTime.Now;
    }
    vars.speedChangedRecently = (DateTime.Now - vars.lastSpeedChange).TotalMilliseconds < vars.speedRaceMs;
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
    vars.splitDone.Clear();
    vars.cairoStarted = false;
    vars.palaceStarted = false;
}

split
{
    // The job's last checkpoint, with the game speed changing as its completion animation
    // starts. The two values arrive separately, so a speed change up to vars.speedRaceMs before the
    // checkpoint (which then arrived late) counts too.
    Func<int, bool> SpeedChangeAt = cp => current.checkpointId == cp
        && (current.gameSpeed != old.gameSpeed || (old.checkpointId != cp && vars.speedChangedRecently));
    Func<int, bool> Entered = cp => current.checkpointId == cp && old.checkpointId != cp;
    // Splits a fixed time after the completion animation starts.
    Action<DateTime> PendingSplit = from =>
    {
        vars.splitPending = true;
        vars.splitPendingTime = from.AddMilliseconds(1950);
    };

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
        if (SpeedChangeAt(1793) && vars.splitDone.Add(1793))
        {
            // Satellite Sabotage
            PendingSplit(vars.lastSpeedChange);
        }
        else if (current.checkpointId == 1820 && old.activeCharacterPointer == 0 && current.activeCharacterPointer != 0 && current.gameSpeed == 0.0f && vars.splitDone.Add(1820))
        {
            // Breaking and Entering
            PendingSplit(DateTime.Now);
        }
        else if (SpeedChangeAt(1632) && vars.splitDone.Add(1632))
        {
            // Bug Dimitri's Office
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(1845) && vars.splitDone.Add(1845))
        {
            // Follow Dimitri
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(1908) && current.gameSpeed == 0.0f && vars.splitDone.Add(1908))
        {
            // Waterpump Destruction
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(1831) && vars.splitDone.Add(1831))
        {
            // Silence the Alarms
            PendingSplit(DateTime.Now);
        }
        else if (SpeedChangeAt(1881) && vars.splitDone.Add(1881))
        {
            // Moonlight Rendezvous
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(1871) && vars.splitDone.Add(1871))
        {
            // Theater Pickpocketing
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(1898) && vars.splitDone.Add(1898))
        {
            // Disco Demolitions
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(1953) && vars.splitDone.Add(1953))
        {
            // Operation: Thunderbeak
            return true;
        }
    }

    if (settings["EPISODE_2"] || settings["ANY"])
    {
        if (SpeedChangeAt(2018) && vars.splitDone.Add(2018))
        {
            // Recon The Ballroom
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2032) && vars.splitDone.Add(2032))
        {
            // Lower the Drawbridge
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2063) && vars.splitDone.Add(2063))
        {
            // Steal a Tuxedo
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2038) && current.activeCharacterPointer == 0 && current.cameraFov == 1.0f && vars.splitDone.Add(2038))
        {
            // Battle the Chopper
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2082) && vars.splitDone.Add(2082))
        {
            // Dominate the Dance Floor
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2090) && vars.splitDone.Add(2090))
        {
            // RC Bombing Run
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2105) && vars.splitDone.Add(2105))
        {
            // Elephant Rampage
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2054) && vars.splitDone.Add(2054))
        {
            // Boardroom Brawl
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(2149) && vars.splitDone.Add(2149))
        {
            // OP: Hippo Drop
            return true;
        }
    }

    if (settings["EPISODE_3"] || settings["ANY"])
    {
        if (SpeedChangeAt(2205) && vars.splitDone.Add(2205))
        {
            // Spice Room Recon
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2233) && vars.splitDone.Add(2233))
        {
            // Elephant Rampage
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2212) && vars.splitDone.Add(2212))
        {
            // Waterbug Run
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2280) && vars.splitDone.Add(2280))
        {
            // Leading Rajan
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2306) && vars.splitDone.Add(2306))
        {
            // Blow the Dam
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2295) && vars.splitDone.Add(2295))
        {
            // Spice Grinder Destruction
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2262) && vars.splitDone.Add(2262))
        {
            // Neyla's Secret
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2315) && vars.splitDone.Add(2315))
        {
            // Rip Off the Ruby
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(2354) && current.rajanHealth == 0 && vars.splitDone.Add(2354))
        {
            // OP: Wet Tiger
            return true;
        }
    }

    if (settings["EPISODE_4"] || settings["ANY"])
    {
        if (SpeedChangeAt(2426) && vars.splitDone.Add(2426))
        {
            // Eavesdrop on Contessa
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2444) && vars.splitDone.Add(2444))
        {
            // Train Hack
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(2461) && vars.splitDone.Add(2461))
        {
            // Wall Bombing
            PendingSplit(DateTime.Now);
        }
        else if (SpeedChangeAt(2528) && vars.splitDone.Add(2528))
        {
            // Code Capture
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(1637) && vars.splitDone.Add(1637))
        {
            // Close to Contessa
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2471) && vars.splitDone.Add(2471))
        {
            // Big House Brawl
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2503) && vars.splitDone.Add(2503))
        {
            // Disguise Bridge
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2620) && vars.splitDone.Add(2620))
        {
            // OP: Trojan Tank
            return true;
        }
    }

    if (settings["EPISODE_5"] || settings["ANY"])
    {
        if (SpeedChangeAt(2659) && vars.splitDone.Add(2659))
        {
            // Know Your Enemy
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2705) && vars.splitDone.Add(2705))
        {
            // Kidnap the General
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2681) && vars.splitDone.Add(2681))
        {
            // Ghost Capture
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2698) && current.activeCharacterPointer == 0 && current.cameraFov == 0.8f && vars.splitDone.Add(2698))
        {
            // Mojo Trap Action
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2761) && vars.splitDone.Add(2761))
        {
            // Crypt Hack
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2733) && current.gameSpeed == 1.0f && current.mapId == 17 && vars.splitDone.Add(2733))
        {
            // Stealing Voices
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2742) && vars.splitDone.Add(2742))
        {
            // Tank Showdown
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(2835) && vars.splitDone.Add(2835))
        {
            // OP: High Road
            return true;
        }
    }

    if (settings["EPISODE_6"] || settings["ANY"])
    {
        if (SpeedChangeAt(2877) && vars.splitDone.Add(2877))
        {
            // Cabin Crimes
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2891) && current.gameSpeed == 0.0f && vars.splitDone.Add(2891))
        {
            // Spice in the Sky
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2929) && current.activeCharacterPointer == 0 && vars.splitDone.Add(2929))
        {
            // A Friend in Need
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2907) && vars.splitDone.Add(2907))
        {
            // Ride the Ironhorse
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2938) && vars.splitDone.Add(2938))
        {
            // Aerial Assault
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2954) && vars.splitDone.Add(2954))
        {
            // Bearcub Kidnapping
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(2942) && vars.splitDone.Add(2942))
        {
            // Theft on the Rails
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(1644) && vars.splitDone.Add(1644))
        {
            // OP: Choo-Choo
            return true;
        }
    }

    if (settings["EPISODE_7"] || settings["ANY"])
    {
        if (SpeedChangeAt(3053) && vars.splitDone.Add(3053))
        {
            // Recon the Sawmill
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3126) && vars.splitDone.Add(3126))
        {
            // Laser Redirection
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3081) && vars.splitDone.Add(3081))
        {
            // Bearcave Bugging
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3094) && vars.splitDone.Add(3094))
        {
            // RC Combat Club
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3154) && vars.splitDone.Add(3154))
        {
            // Old Grizzle Face
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3144) && vars.splitDone.Add(3144))
        {
            // Lighthouse Break in
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3197) && vars.splitDone.Add(3197))
        {
            // Thermal Ride
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3186) && vars.splitDone.Add(3186))
        {
            // Boat Hack
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(3301) && vars.splitDone.Add(3301))
        {
            // OP: Canada Games
            return true;
        }
    }

    if (settings["EPISODE_8"] || settings["ANY"])
    {
        if (SpeedChangeAt(3385) && vars.splitDone.Add(3385))
        {
            // Blimp HQ Recon
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3460) && vars.splitDone.Add(3460))
        {
            // Bentley/Murray Team Up
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3414) && vars.splitDone.Add(3414))
        {
            // Murray/Sly Tag Team
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3429) && vars.splitDone.Add(3429))
        {
            // Sly/Bentley Conspire
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3397) && vars.splitDone.Add(3397))
        {
            // Charged TNT Run
            PendingSplit(vars.lastSpeedChange);
        }
        else if (SpeedChangeAt(3475) && vars.splitDone.Add(3475))
        {
            // Mega-Jump Job
            PendingSplit(vars.lastSpeedChange);
        }
        else if (Entered(3497) && vars.splitDone.Add(3497))
        {
            // Showdown with Clock-La
            return true;
        }
    }

    
    return false;
}

reset
{

}