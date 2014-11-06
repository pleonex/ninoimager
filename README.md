***NinoImager*** is a tool to import and export images from the Nintendo DS game *"Ni no kuni: Shikkoku no Madōshi"*. It supports NPCK pack files (N2D and N3D) and all NitroImage formats:
* Background: NCLR + NCGR + NSCR
* Sprites: NCLR + NCGR + NCER
* Textures: BTX0 (TEX0) and BMD0 (only texture support).

It allows to do batch imports so all images can be imported in just one click. It can also update [modime](https://github.com/pleonex/modime) XML translation files. It has been made for the Spanish translation, more info [here](http://www.gradienwords.tk/Ninokuniproject/). Also it's being used by the Italian translation.

It has been coded with *mono 3.10.x* and *monodevelop 5.7* under *Fedora 20* although it has been also tested under *Microsoft Windows* and *.NET 4.0*.

## Dependencies
It requires *mono* or *.NET* and the *Emgu.CV* library.

## Compile instructions
To compile just open the solution file with *monodevelop* or *Visual Studio* and compile it. Be sure to update the Emgu.CV dependecies paths.

To compile from terminal run:
```
xbuild ninoimager.sln
```
