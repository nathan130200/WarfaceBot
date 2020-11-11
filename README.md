# Warface Bot
Yet another headless XMPP client emulation layer for [Warface](https://pc.warface.com).

# Archived
Due some players abusing of bot (farming battlepass points, etc) mygames released new authentication mechanism to prevent bots from login into server, so this repository will be on hold for a long time, if you find way to bypass/implement the new auth mechanism, you can submit an PR.

# Remarks
This is an C#/.NET Core port to original [Levak/warfacebot](https://github.com/Levak/warfacebot) - Thanks levak!

# About
This is a XMPP client for Warface specific protocol. This is a headless client because it peform lobby tasks only without need launch the game (also can be hosted on an VPS).

The main difference between this and original warfacebot is because you can control everything with C#.

# License

It is mandatory to understand the concept of licensing and _free software_ that yields in the GNU world. Indeed, this program is distributed under the terms of AGPLv3. Please take the time to read and understand the file **LICENSE** shipped within this repository.

# How This Works

TL;DR - Act same as Levak's warfacebot.

Except, that i didn't implemented D-Bus interprocess communication, because you can control client directly from code.

# Platform Invoking
Well if you are familiar with .NET platform, you've been hear something about P/Invoking, is technique to call native code from managed code. In this .NET implementation of warfacebot, i didn't found any good way to implement the _protect protocol_ (Warface extra encryptation layer) using managed code, the best alternative is implement that in native code, then invoke with P/Invoke.

Using native p/invoke for that is ultrafast and simple, you just need build manually native library shipped in this repository.

# Important Notices
- HWID is not implemented. Some servers ban/kick HWIDs that are not valid (TODO: Set in configuration)

- This use an XMPP library (also that i've ported to .net core) [AgsXMPP.NetCore](https://github.com/nathan130200/AgsXMPP) that have an built-in xml parser.

# Building Required Native Library
CMake is best building system that i used to implement native library in multiplatform (Windows/MacOs/Unix).

Follow these steps to build native library:
```
$ git clone https://github.com/nathan130200/WarfaceBot/
$ cd ./Warface.Native
$ mkdir build
$ cd build
$ cmake .. -G "<Generator>"
$ cmake build .
$ cd ./<Configuration>
```

- CMake Generators are platform specific.
	- <b>Windows</b>: `Visual Studio [msvc] [vs] [arch]` or you can use NMake (compile makefile in windows) by using `NMake Files`
		- Tested with VS2019 (Generator: `Visual Studio 16 2019`) and working!
	
	- <b>Unix</b>: `Unix Makefiles`

> Important notes: Same platform you build the library must be same from bot. For example: When builing bot in x64 platform, native library must be build with x64. Or .net will trown an exception because library and native library are different arch.

# Contributing
- Need known at least basic C#/.NET Development (specific with .NET Core)
- .NET TAP Pattern that we use on this project.
- Follow `.editorconfig` coding rules.
- _Other stuff maybe._

#### TODO
Later i will add some automation to build automatically `Warface.Native` library.
