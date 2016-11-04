# vpm
A package Manager for VVVV

## Motivation
In vvvv packs plugins and other externals are not coming in proper packages with meta information attached to them, usually they are just a collection of files at the right places extracted from an archive mostly. In itself it wouldn't be a problem as isolated packs are recognized and loaded into vvvv quite well. Problem starts when there are packs depending on X number of other packs. And with ~
630 externals listed only on vvvv.org + more experimental, pre-release packs circulating on github that actually happens quite some time.

So user were supposed to hunt down dependencies and install them manually with varying success. (which btw would be ridiculous if you'd need to do the same with node.js or .NET libraries) This also scared pack developers to reference or use parts of other magnificent packs forcing them not to develop a feature to its full potential.

## Not anymore!
vpm is a simple and also simple to use command line utility which supposed to automate this process through XML pack configuration files or .vpack files.  
See in action here:

[![click here to watch on youtube](http://img.youtube.com/vi/HtHyucsC0cU/0.jpg)](http://www.youtube.com/watch?v=HtHyucsC0cU)

## Features
* Recursive dependency installation
* Stuff you would expect from a regular package Manager  
  (Except for version checking YET)
* Install packs directly from browser or anywhere with vpm:// and vpms:// uri schemes
* Associate pack file with vpm.
* C# scripting for installation and for packs which don't have a complimentary vpack file (yet ;) )

## How it works?
vpm first creates a temporary folder called (guess what) ".vpm" inside specified vvvv's folder. vpm will create working directories for each packs and dependencies in this temporary folder. These folders can be used by the installation scripts to download, clone or create data for the pack, and copy the results from those folders to vvvv\\packs.  
Then it will parse the input vpack file and its dependencies recursively. When that's done the installation scripts will be run for each collected packs/dependencies.
Note that a vvvv instance is not required anymore so you can specify any, even a non-existing folder. vpm will ask you which architecture you want to use if it can't find a vvvv.exe in the specified folder. If there's no registered vvvv and no folder is specified vpm will use its containing folder.

## Getting started for End-users
* A registered vvvv is recommended (but not required anymore)  
  (means run setup.exe and associate .v4p files at least once during the lifetime of current windows install)
* Download latest version from here: https://vvvv.org/contribution/vpm
* Extract anywhere
* Run it at least once so it can register vpm:// or vpms:// uri schemes and associate .vpack files.  
  (requires Admin privileges (UAC dialog box will pop up))
* Now you can either double click a .vpack file or use a vpm(s):// url from browser.

## Getting started for Power-users
or Command line arguments:

| Argument | Example | Description |
|---|---|---|
| -vvvv | -vvvv \Path\To\vvvv.exe | Manually select a vvvv instead of a registered one. It works also if no vvvv is registered. |
| -q | -q | simply don't prompt user and go with default choices |

## Getting started for Pack developers
### XML specification
The mandatory minimal vpack file and its structure looks something like this:

```xml
<vpack>
  <meta>
    <name>MyPack</name>
  </meta>
  <install>
    // C# script here
  </install>
</vpack>
```

However this vpack won't do anything as no instructions are written in the installation script. Here are the currently processed tags: (tags tagged with a * is mandatory)

__`<name>*`__ will also serve as the default folder name and vpm will also determine if an earlier installment is already in the packs folder based on this name (... too, see aliases)

__`<aliases>`__ is a list of possible alternative foldernames might containing current pack in vvvv's packs folder separated with commas. vpm checks if pack is already installed based on the name and aliases if any is specified.

__`<source>`__ It may specify a git repository which vpm will clone in its temporary folder. Otherwise the installation script have to take care of fetching pack data from the internet too. It is optional and it is rather recommended to use the GitClone method in the installation script.

__`<dependencies>`__ The collection of `<dependency>` tags. Any other tags will be ignored by vpm here.

__`<dependency>`__ is pointing to a pack which the current pack is relying on. Its child tags are
* __`<name>*`__ same as /vpack/meta/name just for dependency
* __`<aliases>`__ same as /vpack/meta/name just for dependency
* __`<source>*`__ can point to
  * a .vpack file with vpm(s)://foo.bar/stuff.vpack format
  * a .csx file containing plain C# installation script with http(s)://foo.bar/stuff.csx format
  * a public git repository presumably containing a vpack file in its root with http(s)://foo.bar/stuff.git format

__`<install>*`__ is where the C# script goes which tells vpm what to do with afore-mentioned data. vpm is using C# Scripting language might be dubbed as Roslyn. You can find more on it at https://github.com/dotnet/roslyn/wiki

Here's a real life example using most of the above mentioned tags for mp.dx
```xml
<vpack>
  <meta>
    <name>mp.dx</name>
    <source>https://github.com/microdee/mp.dx.git</source>
    <author>microdee</author>
    <dependencies>
      <dependency>
        <name>mp.essentials</name>
        <source>https://github.com/microdee/mp.essentials.git</source>
      </dependency>
      <dependency>
        <name>mp.fxh</name>
        <source>https://github.com/microdee/mp.fxh.git</source>
      </dependency>
      <dependency>
        <name>dx11-vvvv</name>
        <source>https://raw.githubusercontent.com/vvvvpm/vpdb/vux/dx11-vvvv/github.master.csx</source>
        <aliases>dx11, vvvv-dx11</aliases>
      </dependency>
    </dependencies>
  </meta>
  <install>
    CopyDir(
        Pack.TempDir,
        VVVV.Dir + "\packs\" + Pack.Name,
        ignore: new string[] {"src", ".git*"}
    );
  </install>
</vpack>
```

### Installation scripts
Everything what's specified here https://github.com/dotnet/roslyn/wiki you can do that with vpm too (in theory). It's full fledged C# (in theory) with a python inspired flavor. The only limit here is your dedication. vpm will create a separate "global" object for each installation script containing useful information and common easy to use methods for moving/fetching files around.  
See VpmGlobals at src/vpm/scriptglobals.cs (line 108) to get an idea about what they are and how to use it. (proper docs coming soon to wiki)

Because of how Roslyn works you don't have to "verbally reference" the global object every time you want to access its members. They can be written out implicitly like this:
```csharp
// instead of VpmGlobals.Pack.Name just
Pack.Name
// or instead of VpmGlobals.GitClone(...) just
GitClone(...)
```

Here's a real-life example of the dx11-vvvv installation script

```csharp
GitClone("https://github.com/mrvux/dx11-vvvv.git", Pack.TempDir);
GitClone("https://github.com/mrvux/FeralTic.git", Pack.TempDir + "\\FeralTic");
GitClone("https://github.com/mrvux/dx11-vvvv-girlpower.git", Pack.TempDir + "\\girlpower");

BuildSolution(2013, Pack.TempDir + "\\vvvv-dx11.sln", "Release|" + VVVV.Architecture);

CopyDir(
	Pack.TempDir + "\\Deploy\\Release\\" + VVVV.Architecture + "\\packs\\dx11",
	VVVV.Dir + "\\packs\\dx11"
);
```

Also other one for messages:

```csharp
GitClone("https://github.com/microdee/vvvv-Message.git", Pack.TempDir);

BuildSolution(2013, Pack.TempDir + "\\src\\vvvv-Message.sln", "Release|" + VVVV.Architecture, true);

CopyDir(
	Pack.TempDir + "\\build\\" + VVVV.Architecture + "\\Release",
	VVVV.Dir + "\\packs\\vvvv-Message"
);
CopyDir(
	Pack.TempDir + "\\build\\AnyCPU\\Release",
	VVVV.Dir + "\\packs\\vvvv-Message"
);

```

### Publishing
To make your pack available for the public you need to create your vpack file put it anywhere on the internet and create vpm link on your publishing surface (like vvvv.org or README.md's) with this syntax. (Note: pure html links will work on vvvv.org articles/contributions too ;) )

```xml
<a href="vpm(s)://DOMAIN/PATH/PACK.vpack">My Awesome Pack</a>
```
vpm links are actually http urls just the http:// part is replaced with vpm:// (or https:// is replaced with vpms:// for secure connections).  
For example a vpm link to mp.dx is

```xml
<a href="vpms://raw.githubusercontent.com/microdee/mp.dx/master/p.vpack">mp.dx</a>
```
Apparently uri schemes other than http(s) is not working with github flavored markdown so I can't demonstrate here but you can get the point. :(

License is MIT aka "do whatever you want with it, no fucks are given"
