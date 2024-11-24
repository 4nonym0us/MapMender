# MapMender
A tool to fix broken Warcraft 3 maps after Reforged 2.0 release (November 13, 2024):
- Fixes invisible/transparent units/buildings
- Fixes `R2S`/`R2SW` functions (incorrect number display, broken scripts)

## Usage

### Easy way
Put the `*.w3x` map next to the MapMender (`MapMender.exe`) and start the application. Patched maps will be created in the `Fixed` directory.

### Advanced way
It's possible to execute the MapMender from the terminal, which allows to manually specify location(s) of the map(s). It also supports a few command line options (`no-r2s-patch`, `no-unitskin-patch`; case-insensitive, with prefixed `/`, `-`, `--`), which could be used to disable one of the patchers for testing purposes. Patched maps will be available in the `Fixed` directory.
```
Usage:
  MapMender.exe [options] [files]

Arguments:
  files                Specify one or more map files to process.
                       If no files are specified, MapMender will process maps in its directory.

Options:
  /no-r2s-patch        Disables R2S/R2SW patch
  /no-unitskin-patch   Disables UnitSkin patch
Notes:
  At least one patch type should be enabled.
  It is forbidden to use both `/no-r2s-patch` and `/no-unitskin-patch` options simultaneously.
```

## What does it fix? What is wrong with the Reforged 2.0?

As a result of the Reforged 2.0 update, many maps stopped working dues one two main issues, both of these these are quite severe and should have been fixed during the first round of Q/A, yet here we are. Severity of these bugs cannot be overstated, even Classic Campaign maps are affected and even if you were to create a blank new map in the latest version of the World Editor, you would still end up using broken R2S/R2SW ("incorrect number formatting" bug).

### #1: "Invisible units and buildings bug" (Lack of backwards compatibility for a new UnitSkin system)

As a part of improvements in the Reforged 2.0, a new system were introduced, where a map developer can specify different resources for SD/HD graphics modes. This information is stored in the `UnitSkin.txt` and seems like a good feature overall, but it has no backwards compatibility whatsoever. In some cases, a game would fail to load a fallback value for the unit and its' model would become completely transparent and impossible to select using mouse.

In addition to this, there were reports of the bug being reproducible in Classic Campaign mode (meaning, that is even present on the maps, authored and supported by Blizzard themselves). Obviously, issues is more severe in Custom Maps because these are usually no longer supported and lack of any backwards compatibility basically means that map is no longer playable on Reforged (no matter what Graphics mode you use).

Read more at: https://us.forums.blizzard.com/en/warcraft3/t/invisible-units-in-classic-campaign/33675

### #2: "Incorrect number formatting" (Broken real to string conversion `R2S`/`R2SW`)

There are two functions in core library, which are responsible for conversion of real numbers to a string with a given precision (affects numbers of decimal digits) and width (min width of the string; if the result is smaller, a padding should be added). `R2S(r)` seem to be just a shortcut for `R2SW(r, 1, 3)`, which means that `R2S` just performs real-to-string conversion with 3-digit "precision".
```j
native R2S  takes real r returns string
native R2SW takes real r, integer width, integer precision returns string
```
After the Reforged 2.0 update, calling `R2S` and `R2SW` produces a gradually truncated result until it eventually starts returning `null`. Following screenshot displays a chain of `R2SW(123.234, 1, 3)` invocations on a brand new map created in the latest version of World Editor.

![image](https://github.com/user-attachments/assets/895d8769-914b-4e09-95f1-25628ffeadd9)

In some maps this bug just results in a visual glitches and inaccuracies of displayed text, but I've also seen maps, which were completely non-functional after the upgrade because not a single sane map developer would expect `R2S`/`R2SW` to start returning `null` at any point of time for a valid input.

Read more at: https://us.forums.blizzard.com/en/warcraft3/t/200-r2s-native-is-broken/33534

## Workarounds

### Automated patcher (MapMender)

The easiest way to fix both issues is to use MapMender, which automatically reads all maps from the executing directory, applies all patches (if they are not already applied), and saves a new version of the map in the `Fixed` directory. Refer to the **Usage** section for more details.

### Manual

* `UnitSkin.txt` fix

Open and read the `UnitUI.slk`, extract properties `skinType`, `file`, `unitSound`, `armor` of every record in the file, then populate `UnitSkin.txt` using following format and a single pre-defined "header" (`UIID` section):
```ini
[UIID]
skinType=unit
file=
unitSound=
armor=
[Hamg]
skinType=unit
file=units\human\HeroArchMage\HeroArchMage
unitSound=HeroArchMage
armor=Flesh
; ... add other units using the same template.
```

* `R2S`/`R2SW` fix

Here is a source code for `R2SWF` and `R2SF`, which are replacements for `R2SW` and `R2S` correspondingly, use these functions instead of default ones. Replace any usages of original API with calls to new functions.
```j
function R2SWF takes real r, integer width, integer precision returns string
    local string result = I2S(R2I(r))
    local real absValue = RAbsBJ(r)
    local integer div = 1
    
    if precision > 0 then
        set result = result + "."
        loop
            set div = div * 10
            set result = result + I2S(ModuloInteger(R2I(absValue / (1.0 / I2R(div))), 10))
            set precision = precision - 1
            exitwhen precision == 0
        endloop
    endif
    
    loop
        exitwhen width <= 0
        set width = width - 1
        exitwhen width < StringLength(result)
        set result = " " + result
    endloop
    
    return result
endfunction

function R2SF takes real r returns string
    return R2SWF(r, 1, 3)
endfunction
```

## Limitations

Obviously, patching a MQP implies that the map is not protected, otherwise it would be impossible to re-build modified version of them map. It's not an issue for map devs who own the map, but some outdated maps are no longer supported, but it doesn't necessarily implies that the map is protected, the only way to find it out is to run the app (protected maps will print a corresponding warning) or open it using MPQ editor.

In order to manually test whether a map is protected, load the map using Ladik's MPQ Editor and check whether `(listfile)` is valid and contains correct file names. If all files in the MPQ are being displayed as `File00000000.xxx` (actual extension and digits in the name might vary), than the map is protected and you need to deprotect it first if you want to modify it.

Some maps can be easily rebuilt without protections, which would allow applying patches, but this is out of the scope of this project. If you are interested on finding out how to rebuild an MPQ, check out following [guide](https://forum.wc3edit.net/viewtopic.php?t=34876) with excellent illustrations.

## Credits

Big thanks to [terrio](https://www.hiveworkshop.com/members/terrio.241355/) for [his version](https://www.hiveworkshop.com/threads/populate-unitskin-txt-missing-models-ingame-after-patch-2-0-fix.356611/) of UnitSkin.txt patcher (it works well for simple maps, but might have issues with more complex maps due to their SLK parser not being very robust, which sometimes leads to UnitSkin.txt being incorrect or incomplete).
