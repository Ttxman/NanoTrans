NanoTrans
=========

Editor for Orthographic and Phonetic Transcriptions in C# and WPF

### Publication:  
Seps, Ladislav. "NanoTrans—Editor for orthographic and phonetic transcriptions." Telecommunications and Signal Processing (TSP), 2013 36th International Conference on. IEEE, 2013.  
  
This publication should work as feature overview, copy (nanotrans_TSP2013.pdf) is included in root


# If you have any question or in need of help with NanoTrans please feel free to email me.


# Libraries used
FFmpeg executable http://www.ffmpeg.org/ ... for loading and converting nearly any media files into WAV  
USBHIDDRIVER http://www.florian-leitner.de/index.php/projects/usb-hid-driver-library/ .. for "Pedals.exe" used for infinity foot control pedals  
For rest please check nuget packages


# Warnings!!!
1. You can currently download (from settings) only czech spellchecker definitions (for other languages you have to   download it manually and load zip)


# Todo
readme :)  
example files  
manuals  
adapt versioning and updater to GIT  
translate updater  
repair bugs  


# Work in progress
Online database of speakers. (for synchronizing speakers across documents)


# Information
## core library
NanoTrans.Core can be used in any project, its independent on UI libraries (and can be used on mono)


## Transcription format
format is defined in TRSXSchema3.xsd  
in general all tags can have any number of custom attributes, NanoTrans will not delete or modify them. (but if tag is deleted or merged custom attributes are lost)  
For custom structured data there is \<meta\> tag. You can store anything in it and it will be preserved.  

## Import and export plugins
plugins are defined in Plugins.xml  
there are 2 possible ways to create plugin:  
### exe/script
commmand is executed by shell with parameters {0} and {1}, where {0} is input file and {1} is output file  
exact parameter string for executable is specified in Plugins.xml  
### .NET assembly
assembly and class are specified in Plugins.xml  
On the specified class public static method is called to do the importin/exporting  
required formats are:  
import: Func\<Stream, Transcription, bool\> ... public static bool Import(Stream input, Transcription storage)  
export: Func\<Transcription, Stream, bool\> ... public static bool Export(Transcription t, Stream Output)  

## Localization
To add localization you have to add resource file called "Strings.LOCALE.resx" to the project "NanoTrans" in Properties section. It should contains same lines as "Strings.resx".  
Only requirement to LOCALE is to be usable in constuctor new System.Globalization.CultureInfo(Locale);  
Localization selection is automatic based on culture set in Windows. You can override this in settings.  
Additional editing of source shouldn't be necessary.  


