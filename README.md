***NinoImager*** is a tool to import and export images from the Nintendo DS game *"Ni no kuni: Shikkoku no Madōshi"*. It supports NPCK pack files (N2D and N3D) and all NitroImage formats:
* Background: NCLR + NCGR + NSCR
* Sprites: NCLR + NCGR + NCER
* Textures: BTX0 (TEX0) and BMD0 (only texture support).

It allows to do batch imports so all images can be imported in just one click. It can also update [modime](https://github.com/pleonex/modime) XML translation files. It has been made for the Spanish translation, more info [here](http://www.gradienwords.tk/Ninokuniproject/). Also it's being used by the Italian translation.

It runs under *mono* (tested only with version *3.10.x*) or *.NET 4.0* and it has been tested under *Windows 7* and *Fedora 20*.

## Dependencies
It requires *mono* or *.NET 4.0* .

## Compile instructions
First clone the repository
``` shell
git clone https://github.com/pleonex/ninoimager.git
cd ninoimager
git submodule update --init --recursive
```

If you want to compile Emug.CV (Open.CV wrapper for C#, it's ~1.85 GB) follow these steps

* Windows:
``` shell
git submodule update --init --recursive
cd lib\emgucv
Build_Binary_x86.bat
```
* Unix:
``` shell
git submodule update --init --recursive
cd lib/emgucv/Solution/VS2010_2012_2013
xbuild Emgu.CV.sln
```
Else, you can download the binaries from here: [Windows](https://db.tt/zaq5LoXt), [Unix](https://db.tt/4nbhoHNa). Extract them into `lib/emgucv/bin/` (create the dirs).

Finally compile ninoimager
```
xbuild ninoimager.sln
```
