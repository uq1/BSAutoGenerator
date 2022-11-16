# BSAutoMapper

#### A beat saber automatic map generator based on [Lolighter](https://github.com/Loloppe/Lolighter/) but using completely different automapping methods.

> _I tried all the AI based auto-mapper's out there and quickly came to one conclusion, THEY ALL SUCK!!! ... so ..._

**This generator is based more on casual and fun play (easy, normal and hard levels), not biased toward expert and expert+ levels, but it will do those just fine as well. It is focused on map flow. I want to feel like i'm dancing, not obtaining physical damage trying to hit impossible note combinations...**


> _You (optionally, see below) provide your favorate example map dat files, however many or few you want. More will add more options but slow the generation a little). It calculates patterns based on those for new maps, using pure logic, not dumb AI guesswork (although a BeatSage command line is also available, should you want the chaotic maps, see below)._

#### Download the latest release [HERE](https://github.com/uq1/BSAutoGenerator/releases/tag/release)

## [RealFlow v3 Example Song](https://skystudioapps.com/bs-viewer/?url=https://www2.aonode.com/get/ZmI1ZThlMzg5NGVmZTI5NGJlNjdhOGYwNDQyOWEzMjk%3D/U2tKU3BjYVNmaGpRRUE4c3RGOEFKeStLM2xrbERuc0pjdDVacmRuNE14YWRyU0svMlVwYTIvY3pTS3FtajhlMQ%3D%3D/1668599142/Reckoner%20-%20Above%20and%20Beyond%20Remix(RealFlow%20v3).zip)


#### Based on Lolighter 3.0.0 (WIP) by **Loloppe#6435** [HERE](https://github.com/Loloppe/Lolighter/)


## Instructions:

### Setup: (Skip this step if you want, I provided some)
> 1. Copy your favorate song's (favorate difficulty levels) dat files into either the "default" folder, or a new folder inside the patternData folder (and rename them to anything, but keep the extension as .dat).
> 2. Done.

### Method 1: Drag and Drop to exe.
> 1. Make a new folder somewhere on your computer and put an mp3 or ogg file in there.
> 2. Drag the ogg or mp3 file onto BSAutoGenerator.exe.
> 2a. If it asks for a song name and/or artist (it could not find any in the sound file), type those in if asked.
> 3. Enjoy.

### Method 2: Drag and Drop to shortcut icon on desktop.
> 1. (First Time Only) Right click and drag the exe to your desktop, then let go of the mouse button and pick create shortcut. You can rename it if you wish.
> 2. Make a new folder somewhere on your computer and put an mp3 or ogg file in there.
> 3. Drag the ogg or mp3 file onto the shortcut.
> 3a. If it asks for a song name and/or artist (it could not find any in the sound file), type those in if asked.
> 4. Enjoy.

### Method 3: Drag and Drop onto the BSAutoGenerator window.
> 1. Make a new folder somewhere on your computer and put an mp3 or ogg file in there.
> 2. Open BSAutoGenerator.
> 3. Drag the ogg/mp3 onto the window.
> 4. Enjoy.

### Method 4: Command Line (simple example):
> BSAutoGenerator <optional_command_line_options_below> "c:\soundpath\soundfile.ogg"

### Custom Command Line Options:
> **--silent**                  - run in a more silent mode, it is pretty silent though anyway.
> **--beatsage**                - use beat sage to generate the maps, should you ever want to, takes longer and they are worse.
> **--obstacles**               - to enable obstacles on new auto-generations.
> **--bpmdivider #**            - divide (or multiply if less than 1.0) the bpm by this value for adjusting beat detection.
> **--irangemultiplier #**      - multiply indistinguishable range for beat detection by this. Also alters difficulty.
> **--patterns "<folderName>"** - specify a custom folder within the patternData folder to load patterns from (styles).

### Notes:
> 1. If you specify or drag/drop a .dat file (instead of a ogg/mp3 file), it will only auto-generate lighting for your current map.
> 2. You can also use the UI exactly the same as the original LoLighter, I added the command line and drag/drop for speed, and for automating.
