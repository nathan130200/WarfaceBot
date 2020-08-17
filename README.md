# Warface Bot
Yet another headless XMPP client emulation layer for [Warface](https://pc.warface.com).

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

You just need only a thing: CMake! :tada::tada::tada:

CMake is best building system that i used to implement native library in multiplatform (Windows/MacOs/Unix).

Follow these steps to build native library:

1. Clone the repository
2. Navigate to repository directory using an terminal/command prompt.
3. Then navigate to `Warface.Native` directory.
	- And create yet another directory named `build`
	
	Depending on platform you need specifiy specific CMake generator, to creare appropriated building scripts.

	- Windows:
		- `cmake .. -G "Visual Studio XX"` will generate windows/visual studio build files.
			- When `XX` your visual studio msvc version.
			
	- MacOS/Unix:
		- `cmake .. -G "Unix Makefiles"` will generate unix makefile to build.
	
4. After generate build files, you just execute:<br>
	- `cmake --build`
	
5. Then you can grab generated library file `libWarface.Native.so` or `Warface.Native.dll` depending on your OS, and put in same folder `Warface.Bot.dll` is located (grabbed from releases) and now you can use/execute bot.


# Contributing
- Need known at least basic C#/.NET Development (specific with .NET Core)
- .NET TAP Pattern that we use on this project.
- Follow `.editorconfig` coding rules.
- _Other stuff maybe._

#### TODO
Later i will add some automation to build automatically `Warface.Native` library.