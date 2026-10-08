state("SluMAN") {}

// TODO: Move the start episode splitting logic to update loop, 
// and only check for flags in split/start to avoid duplicate code

startup
{	
    
    settings.Add("ANY", false, "Any%");
    settings.Add("EPISODE_1", false, "Episode 1");
    settings.Add("EPISODE_2", false, "Episode 2");
    settings.Add("EPISODE_3", false, "Episode 3");
    settings.Add("EPISODE_4", false, "Episode 4");
    settings.Add("EPISODE_5", false, "Episode 5");
    settings.Add("EPISODE_6", false, "Episode 6");
    settings.Add("AIRTIME", false, "Air Time");
    settings.Add("OCTAVIO", false, "Octavio's Last Stand");
    settings.Add("ROCKRUN", false, "Rock Run");
    settings.Add("PRESSURE", false, "Pressure Brawl");
    settings.Add("CARMELITA", false, "Carmelita Climb");
    settings.Add("GOINGOUT", false, "Going Out On A Wind");
    settings.Add("AIRCHINA", false, "Big Air in China");
    settings.Add("BEAST", false, "Beauty versus the Beast");
    settings.Add("GAUNTLET", false, "Ultimate Gauntlet");
    settings.Add("DRM", false, "Battle Against Time");
}

init
{
    System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("racman-autosplitter");
    System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream();
    vars.reader = new System.IO.BinaryReader(stream);
    
    vars.reader.BaseStream.Position = 0;

    vars.splitPending = false;
    vars.splitPendingTime = DateTime.Now;
    vars.kaineStarted = false;
    vars.hollandStarted = false;

    vars.airTimeChanged = false;
    vars.currentAirTimeSection = 0;
    vars.totalAirTime = 0.0f;
    vars.currentAirTime = 0.0f;

    vars.getAJobDone = false;
    vars.cooperHangarDefenceDone = 0;

    current.isLoading = vars.reader.ReadUInt32();
    current.currentJob = vars.reader.ReadUInt32();
    current.currentCheckpoint = vars.reader.ReadUInt32();
    current.currentMap = vars.reader.ReadUInt32();
    current.gameSpeed = vars.reader.ReadSingle();
    current.slyCharacterPointer = vars.reader.ReadUInt32();
    current.activeCharacterPointer = vars.reader.ReadUInt32();
    current.cameraFov = vars.reader.ReadSingle();
    current.veniceStarted = vars.reader.ReadUInt32();
    current.outbackStarted = vars.reader.ReadUInt32();
    current.chinaStarted = vars.reader.ReadUInt32();
    current.pirateStarted = vars.reader.ReadUInt32();
    current.mtcTimerValue = vars.reader.ReadSingle();
    current.characterId = vars.reader.ReadUInt32();
    current.pauseLocked = vars.reader.ReadUInt32();
}

update
{
    vars.reader.BaseStream.Position = 0;

    current.isLoading = vars.reader.ReadUInt32();
    current.currentJob = vars.reader.ReadUInt32();
    current.currentCheckpoint = vars.reader.ReadUInt32();
    current.currentMap = vars.reader.ReadUInt32();
    current.gameSpeed = vars.reader.ReadSingle();
    current.slyCharacterPointer = vars.reader.ReadUInt32();
    current.activeCharacterPointer = vars.reader.ReadUInt32();
    current.cameraFov = vars.reader.ReadSingle();
    current.veniceStarted = vars.reader.ReadUInt32();
    current.outbackStarted = vars.reader.ReadUInt32();
    current.chinaStarted = vars.reader.ReadUInt32();
    current.pirateStarted = vars.reader.ReadUInt32();
    current.mtcTimerValue = vars.reader.ReadSingle();
    current.characterId = vars.reader.ReadUInt32();
    current.pauseLocked = vars.reader.ReadUInt32();

    if (settings["AIRTIME"])
    {
        if (current.currentCheckpoint != 2215)
        {
            vars.currentAirTime = Math.Floor(100*45*(1-old.mtcTimerValue))/100;   
        }
        if (current.currentJob == 2199 && current.mtcTimerValue > old.mtcTimerValue && vars.currentAirTimeSection < 5)
        {
            vars.totalAirTime += vars.currentAirTime;
            vars.airTimeChanged = true;
            vars.currentAirTimeSection += 1;
            vars.currentAirTime = 0.0f;
        }
        else if (current.currentCheckpoint == 2215 && old.currentCheckpoint != 2215)
        {
            vars.totalAirTime = 6*Math.Floor(100*45*(1-current.mtcTimerValue))/100;
            vars.currentAirTime = 0.0f;
        }
    }
}

start
{
    if (settings["ANY"] && vars.kaineStarted == false && current.currentMap == 35 && current.cameraFov == 1.06f && current.currentJob == 1798) 
    { 
        vars.kaineStarted = true;
        return true;   
    }
    else if (settings["EPISODE_1"] && current.veniceStarted == 1 && old.veniceStarted == 0 && current.currentMap == 3 && current.isLoading == 3) 
    { 
        return true; 
    }
    else if (settings["EPISODE_2"] && current.outbackStarted == 1 && old.outbackStarted == 0 && current.currentMap == 8 && current.isLoading == 3) 
    { 
        return true; 
    }
    else if (settings["EPISODE_3"] && vars.hollandStarted == false && old.cameraFov == 0.95f && current.cameraFov != 0.95f && current.cameraFov != 1.0f && current.currentMap == 15) 
    { 
        vars.hollandStarted = true;
        return true; 
    }
    else if (settings["EPISODE_4"] && current.chinaStarted == 3 && old.chinaStarted == 1 && current.currentMap == 23 && current.isLoading == 3) 
    { 
        return true; 
    }
    else if (settings["EPISODE_5"] && current.pirateStarted == 3 && old.pirateStarted == 1 && current.currentMap == 31 && current.isLoading == 3) 
    { 
        return true; 
    }
    else if (settings["EPISODE_6"] && current.currentCheckpoint == 4369 && old.currentCheckpoint != 4369) 
    { 
        return true; 
    }
    else if (settings["GAUNTLET"] && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["OCTAVIO"] && current.currentCheckpoint == 2528 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["ROCKRUN"] && current.currentCheckpoint == 2606 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["CARMELITA"] && current.currentCheckpoint == 2934 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["PRESSURE"] && current.currentCheckpoint == 2779 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["GOINGOUT"] && current.currentCheckpoint == 3325 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["AIRCHINA"] && current.currentCheckpoint == 3466 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["BEAST"] && current.currentCheckpoint == 4369 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["DRM"] && current.currentCheckpoint == 4564 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
    else if (settings["AIRTIME"] && current.currentCheckpoint == 2202 && current.mtcTimerValue < 1.0f && old.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }

    return false;
}

isLoading
{
    if (settings["GAUNTLET"] || settings["OCTAVIO"] || settings["ROCKRUN"] || settings["CARMELITA"] || settings["PRESSURE"] || settings["GOINGOUT"] || settings["AIRCHINA"] || settings["BEAST"] || settings["DRM"] || settings["AIRTIME"]) 
    { 
        return true; 
    }
    return current.isLoading != 3;
    
}

gameTime
{
    if (settings["GAUNTLET"] && current.currentJob == 4494 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*15*60*(1-current.mtcTimerValue))/100);
    }
    else if (settings["OCTAVIO"] && current.currentJob == 2448 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*4.5*60*(1-current.mtcTimerValue))/100);
    }
    else if (settings["ROCKRUN"] && current.currentJob == 2605 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*2*60*(1-current.mtcTimerValue))/100);
    }
    else if (settings["CARMELITA"] && current.currentJob == 2867 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*45*(1-current.mtcTimerValue))/100);
    }
    else if (settings["PRESSURE"] && current.currentJob == 2756 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*60*3*(1-current.mtcTimerValue))/100);
    }
    else if (settings["GOINGOUT"] && current.currentJob == 3281 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*210*(1-current.mtcTimerValue))/100);
    }
    else if (settings["AIRCHINA"] && current.currentJob == 3464 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*120*(1-current.mtcTimerValue))/100);
    }
    else if (settings["BEAST"] && current.currentJob == 4342 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*100*(1-current.mtcTimerValue))/100);
    }
    else if (settings["DRM"] && current.currentJob == 4558 && current.mtcTimerValue < old.mtcTimerValue)
    {
        return TimeSpan.FromSeconds(Math.Floor(100*180*(1-current.mtcTimerValue))/100);
    }
    else if (settings["AIRTIME"] && current.currentJob == 2199)
    {
        if (current.currentCheckpoint == 2215 && old.currentCheckpoint != 2215)
        {
            vars.airTimeChanged = true;
            return TimeSpan.FromSeconds(Math.Floor(100*45*6*(1-current.mtcTimerValue))/100);
        }
        else if (current.currentCheckpoint < 2215)
        {
            return TimeSpan.FromSeconds(vars.totalAirTime+vars.currentAirTime);
        }
        
    }
}

onReset
{
    vars.splitPending = false;
    vars.splitPendingTime = DateTime.Now;
    vars.kaineStarted = false;
    vars.hollandStarted = false;
    vars.airTimeChanged = false;
    vars.totalAirTime = 0.0f;
    vars.currentAirTime = 0.0f;
    vars.currentAirTimeSection = 0;
    vars.getAJobDone = false;
    vars.cooperHangarDefenceDone = 0;
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

    if (settings["GAUNTLET"])
    {
        if (current.currentCheckpoint == 4500 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4502 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4504 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4506 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4508 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4510 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4512 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4514 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4516 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
        if (current.currentCheckpoint == 4519 && old.currentCheckpoint != current.currentCheckpoint) {return true;}
    }

    if (settings["OCTAVIO"])
    {
        if (current.currentCheckpoint == 2531 && old.currentCheckpoint != 2531) { return true; }
        if (current.currentCheckpoint == 2531 && old.cameraFov == 1.1f && current.cameraFov == 0.48f) { return true; }
        if (current.currentCheckpoint == 1820 && old.currentCheckpoint != 1820) { return true; }
    }

    if (settings["ROCKRUN"])
    {
        if (current.currentCheckpoint == 2608 && old.currentCheckpoint != 2608) { return true; }
        if (current.currentCheckpoint == 2612 && old.currentCheckpoint != 2612) { return true; }
        if (current.currentCheckpoint == 2615 && old.currentCheckpoint != 2615) { return true; }
        if (current.currentCheckpoint == 2617 && old.currentCheckpoint != 2617) { return true; }
        if (current.currentCheckpoint == 2032 && old.currentCheckpoint != 2032) { return true; }
    }

    if (settings["CARMELITA"])
    {
        if (current.currentCheckpoint == 2944 && old.currentCheckpoint != 2944) { return true; }
        if (current.currentCheckpoint == 2942 && old.currentCheckpoint != 2942) { return true; }
    }

    if (settings["PRESSURE"])
    {
        if (current.currentCheckpoint == 2781 && current.characterId != old.characterId) { return true; }
        if (current.currentCheckpoint == 2788 && old.currentCheckpoint != 2788) { return true; }
    }

    if (settings["GOINGOUT"])
    {
        if (current.currentCheckpoint == 3325 && current.pauseLocked == 1 && old.pauseLocked == 0) { return true; }
        if (current.currentCheckpoint == 1822 && old.currentCheckpoint != 1822) { return true; }
    }

    if (settings["AIRCHINA"])
    {
        if (current.currentCheckpoint == 3467 && old.currentCheckpoint != 3467) { return true; }
        if (current.currentCheckpoint == 3468 && old.currentCheckpoint != 3468) { return true; }
        if (current.currentCheckpoint == 3470 && old.currentCheckpoint != 3470) { return true; }
    }

    if (settings["BEAST"])
    {
        if (current.currentCheckpoint == 4369 && current.pauseLocked == 1 && old.pauseLocked == 0) { return true; }
        if (current.currentCheckpoint == 4382 && old.currentCheckpoint != 4382) { return true; }
    }

    if (settings["DRM"])
    {
        if (current.currentCheckpoint == 2041 && old.currentCheckpoint != 2041) { return true; }
    }

    if (settings["AIRTIME"])
    {
        if (vars.airTimeChanged == true)
        {
            vars.airTimeChanged = false;
            return true;
        }
    }


    if (settings["ANY"])
    {
        if (old.currentCheckpoint == 2029 && current.currentCheckpoint != 2029 && current.currentJob == 1798) 
        { 
            // The Cooper Vault
            return true; 
        }

        if (old.currentCheckpoint == 4618 && current.currentCheckpoint != 4618) 
        { 
            // Hazard Room
            vars.splitPending = true; 
        }

        if (current.veniceStarted == 1 && old.veniceStarted == 0 && current.currentMap == 3 && current.isLoading == 3) 
        { 
            // Start Episode 1
            return true; 
        }

        if (current.outbackStarted == 1 && old.outbackStarted == 0 && current.currentMap == 8 && current.isLoading == 3) 
        { 
            // Start Episode 2
            return true; 
        }

        if (vars.hollandStarted == false && old.cameraFov == 0.95f && current.cameraFov != 0.95f && current.cameraFov != 1.0f && current.currentMap == 15) 
        { 
            // Start Episode 3
            vars.hollandStarted = true;
            return true; 
        }

        if (current.chinaStarted == 3 && old.chinaStarted == 1 && current.currentMap == 23 && current.isLoading == 3) 
        { 
            // Start Episode 4
            return true; 
        }

        if (current.pirateStarted == 3 && old.pirateStarted == 1 && current.currentMap == 31 && current.isLoading == 3) 
        { 
            // Start Episode 5
            return true; 
        }
        if (current.currentCheckpoint == 4369 && old.currentCheckpoint != 4369) 
        { 
            // Start Episode 6
            return true; 
        }

        if (old.currentCheckpoint == 3454 && current.currentCheckpoint != 3454) 
        { 
            // King of Fire
            return true;
        }
    }

    if (settings["EPISODE_1"] || settings["ANY"])
    { 
        if (current.gameSpeed == 0 && old.gameSpeed == 1) 
        {
            if (current.currentCheckpoint == 2035) 
            { 
                // Canal Chase
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2358) 
            { 
                // Into the Depths
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2215) 
            { 
                // Tar Ball
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2197) 
            { 
                // Turf War!
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2434) 
            { 
                // Guard Duty
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2252) 
            { 
                // Run n Bomb
                vars.splitPending = true; 
            }
        }

        if (current.currentCheckpoint == 1820 && old.currentCheckpoint != 1820) 
        { 
            // Operation: Tar Be Gone!
            return true; 
        }
        else if (current.currentCheckpoint == 2165 && old.currentCheckpoint != 2165) 
        { 
            // Police HQ
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 2305 && old.currentCheckpoint != 2305) 
        {
            // Octavio Snap 
            vars.splitPending = true; 
        }
        
    }

    if (settings["EPISODE_2"] || settings["ANY"])
    {
        if (current.gameSpeed == 0 && old.gameSpeed == 1) 
        {
            if (current.currentCheckpoint == 2032) 
            { 
                // Searching for the Guru
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2648) 
            { 
                // Spelunking
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2862) 
            { 
                // Dark Caves
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2033) 
            { 
                // Big Truck
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2720) 
            { 
                // Unleash the Guru
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2688) 
            { 
                // The Claw
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2037) 
            { 
                // Lemon Rage
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2820) 
            { 
                // Hungry Croc
                vars.splitPending = true; 
            }
        }

        if (current.currentCheckpoint == 2944 && old.currentCheckpoint == 2942) {
            // Operation: Moon Crash (1st outcome)
            return true;
        }
        else if (current.currentCheckpoint == 2942 && old.currentCheckpoint == 2944) {
            // Operation: Moon Crash (2nd outcome)
            return true;
        }
    }

    if (settings["EPISODE_3"] || settings["ANY"])
    {
        if (current.gameSpeed == 0 && old.gameSpeed == 1) 
        {
            if (current.currentCheckpoint == 3175) 
            { 
                // ACES Semi-finals
                vars.splitPending = true;
            }
            else if (current.currentCheckpoint == 3040 && current.cameraFov == 1.0f)
            {
                // Hidden Flight Roster
                vars.splitPending = true;
            }
            else if (vars.cooperHangarDefenceDone == 1)
            {
                vars.cooperHangarDefenceDone = 2;
                vars.splitPending = true;
            }
            else if (current.currentCheckpoint == 3223) 
            {
                // Windmill Firewall
                vars.splitPending = true;
            }
            else if (current.currentCheckpoint == 3123) 
            { 
                // Frame Team Iceland 
                vars.splitPending = true; 
            } 
            else if (current.currentCheckpoint == 2043) 
            {
                // Beauty and the Beast
                vars.splitPending = true;
            }
        }

        if (current.currentCheckpoint == 1822 && old.currentCheckpoint != 1822)  
        {
            // Operation: Turbo Dominant Eagle
            return true;
        }
        else if (current.currentCheckpoint != 3080 && old.currentCheckpoint == 3080) 
        { 
            // Frame Team Belgium
            vars.splitPending = true;
        } 
        else if (current.currentCheckpoint == 3040 && old.currentCheckpoint != 3040)
        {
            // Hidden Flight Roster
            return false;
        }
        else if (vars.cooperHangarDefenceDone == 0 && vars.splitPending == false && current.currentCheckpoint == 3158) 
        { 
            // Cooper Hangar Defence
            vars.cooperHangarDefenceDone = 1;
            return false;
        } 
        else if (old.currentCheckpoint == 3244 && current.currentCheckpoint != 3244) 
        { 
            // Giant Wolf Massacre
            vars.splitPending = true;
        } 
    }

    if (settings["EPISODE_4"] || settings["ANY"])
    {

        if (vars.getAJobDone == true)
        {
            // Get a Job
            if (current.currentJob != 3471) 
            { 
                vars.getAJobDone = false; 
                return true;
            }
            return false;
        }

        if (current.gameSpeed == 0 && old.gameSpeed == 1) 
        {
            if (current.currentCheckpoint == 3554) 
            {
                // Tearful Reunion 
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 3644) 
            {
                // Laptop Retrieval 
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2038) 
            {
                // Vampiric Demise 
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 3729) 
            {
                // A Battery of Peril 
                vars.splitPending = true; 
            }
        }

        if (old.currentCheckpoint == 3525 && current.currentCheckpoint != 3525) 
        { 
            // Get a Job (set flag)
            vars.getAJobDone = true;
            return false;
        }

        if (current.currentCheckpoint == 1823 && old.currentCheckpoint != 1823) 
        {
             // Operation: Wedding Crasher
            return true;
        }

        if (current.currentCheckpoint == 3603 && old.currentCheckpoint != 3603) 
        { 
            // Grapple-Cam Break-In
            vars.splitPending = true;
        }
        else if (current.currentCheckpoint == 3704 && old.currentCheckpoint != 3704) 
        { 
            // Down the Line
            vars.splitPending = true; 
        }
    }

    if (settings["EPISODE_5"] || settings["ANY"])
    {
        if (current.gameSpeed == 0 && old.gameSpeed == 1) 
        {
            if (current.currentCheckpoint == 4083) 
            { 
                // Jollyboat of Destruction
                vars.splitPending = true;
            }
            else if (current.currentCheckpoint == 2040) 
            {
                // X Marks the Spot 
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 2044) 
            { 
                // Crusher from the Depths
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 4134) 
            { 
                // Deep Sea Danger
                vars.splitPending = true; 
            }
            else if (current.currentCheckpoint == 4157) 
            { 
                // Battle on the High Seas
                vars.splitPending = true; 
            }
        }

        if (current.currentCheckpoint == 4221 && old.currentCheckpoint != 4221) 
        {
            // Operation: Reverse Double-Cross
            return true;
        }
        if (old.currentCheckpoint == 4083 && current.currentCheckpoint != 4083) 
        { 
            // Jollyboat of Destruction (2nd outcome)
            vars.splitPending = true;
        }
        else if (current.currentCheckpoint == 3923 && old.currentCheckpoint != 3923) 
        {
            // The Talk of the Pirates 
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 4001 && old.currentCheckpoint != 4001) 
        {
            // Dynamic Duo 
            vars.splitPending = true; 
        }
    }

    if (settings["EPISODE_6"] || settings["ANY"])
    {
        if (current.currentCheckpoint == 1825 && old.currentCheckpoint != 1825) 
        {
            // Final Legacy
            return true;
        }
        else if (current.currentCheckpoint == 1843 && old.currentCheckpoint != 1843) 
        { 
            // Carmelita to the Rescue
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 1844 && old.currentCheckpoint != 1844) 
        { 
            // A Deadly Bite
            vars.splitPending = true; 
            }
        else if (current.currentCheckpoint == 1845 && old.currentCheckpoint != 1845) 
        { 
            // The Dark Current
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 1846 && old.currentCheckpoint != 1846) 
        { 
            // Bump-Charge-Jump
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 1847 && old.currentCheckpoint != 1847) 
        { 
            // Danger in the Skies
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 1848 && old.currentCheckpoint != 1848) 
        { 
            // The Ancestors Gauntlet
            vars.splitPending = true; 
        }
        else if (current.currentCheckpoint == 1849 && old.currentCheckpoint != 1849) 
        { 
            // Stand Your Ground
            vars.splitPending = true; 
        }
    }

    if (vars.splitPending == true) { vars.splitPendingTime = DateTime.Now.AddMilliseconds(1283);}
    
    return false;
}

reset
{
    if (settings["GAUNTLET"] && current.currentCheckpoint == 4495 && current.mtcTimerValue == 1.0f) 
    { 
        return true; 
    }
}