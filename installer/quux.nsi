; This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton
; Use of this file is subject to the terms of the General Public License Version 3.
; See License.txt included in this package for full terms and conditions.

;QuuxPlayer Setup Script

!define VERSION '2.8.0.5'
!define VER_MAJOR '2'
!define VER_MINOR '8'
!define VER_REVISION '0'

OutFile quuxinstall.exe

SetCompressor /SOLID lzma

InstallDir "$PROGRAMFILES\Quux Software\QuuxPlayer"
InstallDirRegKey HKLM "Software\Quux Software\QuuxPlayer" "InstallDir"

RequestExecutionLevel admin

LicenseForceSelection checkbox

!define MUI_ICON "quuxplayer.ico"
!define MUI_UNICON "quuxplayer.ico"

VIProductVersion ${VERSION}
VIAddVersionKey "ProductName" "QuuxPlayer"
VIAddVersionKey "CompanyName" "Quux Software"
VIAddVersionKey "LegalTrademarks" "QuuxPlayer is a trademark of Matthew Hamilton"
VIAddVersionKey "LegalCopyright" "(c) 2008, 2009 Matthew Hamilton / Quux Software"
VIAddVersionKey "FileDescription" "QuuxPlayer Installer ${VERSION}"
VIAddVersionKey "FileVersion" ${VERSION}
VIAddVersionKey "ProductVersion" ${VERSION}

;--------------------------------
;Header Files

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh" ; needed for dot_net
!insertmacro un.GetParent

;--------------------------------
;Definitions

!define SHCNE_ASSOCCHANGED 0x8000000
!define SHCNF_IDLIST 0

;--------------------------------

;Names
Name "QuuxPlayer"
Caption "QuuxPlayer ${VERSION} Setup"

;Interface Settings
!define MUI_ABORTWARNING

!define MUI_HEADERIMAGE
!define MUI_WELCOMEFINISHPAGE_BITMAP "panel.bmp"
!define MUI_HEADERIMAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Header\win.bmp"
!define MUI_HEADERIMAGE_UNBITMAP "${NSISDIR}\Contrib\Graphics\Header\win.bmp"

;Pages

!define MUI_WELCOMEPAGE_TITLE "Welcome to the QuuxPlayer ${VERSION} Setup Wizard"
!define MUI_WELCOMEPAGE_TEXT "This wizard will guide you through the installation of QuuxPlayer ${VERSION}$\r$\n$\r$\nWe recommend that you close all running applications before continuing."

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\QuuxPlayer\QuuxPlayer\bin\Release\license.rtf"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_LINK "Visit www.quuxplayer.com for updates and support"
!define MUI_FINISHPAGE_LINK_LOCATION "http://www.quuxplayer.com/"

!define MUI_FINISHPAGE_RUN "$INSTDIR\QuuxPlayer.exe"
!define MUI_FINISHPAGE_NOREBOOTSUPPORT
!define MUI_FINISHPAGE_TEXT_LARGE
!define MUI_FINISHPAGE_TEXT "We hope you enjoy using QuuxPlayer! Please visit our website at www.quuxplayer.com for updates and more information!"

!insertmacro MUI_PAGE_FINISH

!define MUI_UNWELCOMEFINISHPAGE_BITMAP "panel.bmp"

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES


!insertmacro MUI_LANGUAGE "English"

BrandingText "Quux Software QuuxPlayer ${VERSION} Installer"

Var /GLOBAL ProgID
Var /GLOBAL FileDesc
Var /GLOBAL Ext
Var /GLOBAL DPPath

Function RegExt

  WriteRegStr HKLM "$DPPath\Capabilities\FileAssociations" "$Ext" "$ProgID"
  WriteRegStr HKLM "Software\Classes\$Ext\OpenWithProgIDs" "$ProgID" ""
  WriteRegStr HKLM "Software\Classes\$Ext\OpenWithList\QuuxPlayer.exe" "" ""

FunctionEnd

Function RegVerbs

  WriteRegStr HKLM "Software\Classes\$ProgID" "" "$FileDesc"
  WriteRegStr HKLM "Software\Classes\$ProgID\DefaultIcon" "" '"$INSTDIR\quuxfile.ico"'  
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell" "" "play"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\add" "" "Add to QuuxPlayer &Library"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\add\command" "" '"$INSTDIR\quuxplayer.exe" /add "%1"'
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\enqueue" "" "Add to &Now Playing in QuuxPlayer"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\enqueue\command" "" '"$INSTDIR\quuxplayer.exe" /enqueue "%1"'
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\open" "" "&Open in QuuxPlayer"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\open\command" "" '"$INSTDIR\quuxplayer.exe" "%1"'
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\play" "" "&Play now with QuuxPlayer"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\play\command" "" '"$INSTDIR\quuxplayer.exe" /play "%1"'
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\playnext" "" "Play Ne&xt in QuuxPlayer"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\playnext\command" "" '"$INSTDIR\quuxplayer.exe" /playnext "%1"'

FunctionEnd

Function RegVerbsPlaylist

  WriteRegStr HKLM "Software\Classes\$ProgID" "" "$FileDesc"
  WriteRegStr HKLM "Software\Classes\$ProgID\DefaultIcon" "" '"$INSTDIR\quuxfile.ico"'  
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell" "" "add"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\add" "" "Add to QuuxPlayer"
  WriteRegExpandStr HKLM "Software\Classes\$ProgID\shell\add\command" "" '"$INSTDIR\quuxplayer.exe" /add "%1"'
  
FunctionEnd

Function un.Reg

  DeleteRegValue HKLM "Software\Classes\$Ext\OpenWithProgIDs" "$ProgID"
  DeleteRegKey HKLM "Software\Classes\$Ext\OpenWithList\QuuxPlayer.exe"
  DeleteRegValue HKLM "Software\Classes\$Ext" "QuuxBackup"
  DeleteRegKey /ifempty HKLM "Software\Classes\$Ext\OpenWithList"
  DeleteRegKey /ifempty HKLM "Software\Classes\$Ext\OpenWithProgIDs"

FunctionEnd

;--------------------------------
;Installer Sections

!include dotnet.nsh
!define DOTNET_VERSION "3.5"

Section ""

  ReadRegDWORD $0 HKLM "Software\Microsoft\NET Framework Setup\NDP\v3.5" "Install"
  IntCmp $0 1 HaveDotNet NoDotNet HaveDotNet
  
NoDotNet:

  !insertmacro CheckDotNET ${DOTNET_VERSION}

HaveDotNet:

  StrCpy $DPPath "Software\Clients\Media\QuuxPlayer"
  
  ;SetDetailsPrint textonly
  DetailPrint "Installing QuuxPlayer Core Files..."
  ;SetDetailsPrint listonly

  SetOutPath $INSTDIR

  SetOverwrite on

; For now, get rid of old installer's cruft

  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|Amazon.ECS.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|Bass.Net.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|Microsoft.DirectX.DirectInput.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|Microsoft.DirectX.DirectSound.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|Microsoft.DirectX.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|QTextBox.dll"
  DeleteRegKey HKLM "Software\Classes\Installer\Assemblies\C:|Program Files|Quux Software|QuuxPlayer|QuuxPlayer.exe"

StrCpy $0 0

loop:
      
      EnumRegKey $1 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall" $0
      StrCmp $1 "" done
      
      StrCmp $1 "Quux Software" okkey
      
      ReadRegStr $2 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\$1" "Contact"
      StrCmp $2 "Quux Software" badkey okkey
      
    badkey:
    
        DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\$1"
        goto nextkey
    
    okkey:
      
      IntOp $0 $0 + 1
    
    nextkey:
    
      goto loop

done:

StrCpy $0 0

loop2:
      
      EnumRegKey $1 HKLM "Software\Classes\Installer\Products" $0
      StrCmp $1 "" done2
      
      StrCmp $1 "Quux Software" okkey2
      
      ReadRegStr $2 HKLM "Software\Classes\Installer\Products\$1" "ProductName"
      StrCmp $2 "QuuxPlayer" badkey2 okkey2
      
    badkey2:
    
        DeleteRegKey HKLM "Software\Classes\Installer\Products\$1"
        goto nextkey2
    
    okkey2:
      
      IntOp $0 $0 + 1
    
    nextkey2:
    
      goto loop2

done2:

  Delete "$DESKTOP\QuuxPlayer.lnk"
  Delete "$PROGRAMFILES\QuuxPlayer.lnk"

  SetShellVarContext all

  Delete "$DESKTOP\QuuxPlayer.lnk"
  Delete "$PROGRAMFILES\QuuxPlayer.lnk"

  SetShellVarContext current

; Now let's install:

  File "..\QuuxPlayer\QuuxPlayer\bin\Release\QuuxPlayer.exe"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\QTextBox.dll"
    
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\beep.wav"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\license.rtf"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\quuxfile.ico"
  
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\taglib-sharp.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\Bass.Net.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass_aac.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass_ac3.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass_alac.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass_mpc.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bassflac.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\basswma.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\basswv.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bassasio.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\bass_ape.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\basscd.dll"

  
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\ReplayGain.dll"

  File "..\QuuxPlayer\QuuxPlayer\bin\Release\Interop.SHDocVw.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\Interop.iTunesLib.dll"
  File "..\QuuxPlayer\QuuxPlayer\bin\Release\Interop.ITDETECTORLib.dll"

  File "C:\WINDOWS\Microsoft.NET\DirectX for Managed Code\1.0.2902.0\Microsoft.DirectX.dll"
  File "C:\WINDOWS\Microsoft.NET\DirectX for Managed Code\1.0.2902.0\Microsoft.DirectX.DirectInput.dll"
  File "C:\WINDOWS\Microsoft.NET\DirectX for Managed Code\1.0.2902.0\Microsoft.DirectX.DirectSound.dll"

  WriteRegStr HKLM "Software\Quux Software\QuuxPlayer" "InstallDir" "$INSTDIR"

  WriteRegStr HKLM "Software\Clients\Media\QuuxPlayer" "" "QuuxPlayer"

  WriteRegStr HKLM "Software\Classes\.qxp" "" "QuuxPlayer.Library"
  WriteRegStr HKLM "Software\Classes\QuuxPlayer.Library" "" "QuuxPlayer Library"
  WriteRegStr HKLM "Software\Classes\QuuxPlayer.Library\DefaultIcon" "" '"$INSTDIR\quuxfile.ico"'  
  
  WriteRegStr HKLM "Software\RegisteredApplications" "QuuxPlayer" "$DPPath\Capabilities"
  WriteRegStr HKLM "$DPPath\Capabilities" "ApplicationName" "QuuxPlayer"
  WriteRegStr HKLM "$DPPath\Capabilities" "ApplicationDescription" "QuuxPlayer is a full-featured audio player for Windows."
  WriteRegStr HKLM "$DPPath\shell\open\command" "" '"$INSTDIR\QuuxPlayer.exe"'
  WriteRegStr HKLM "$DPPath\DefaultIcon" "" '"$INSTDIR\QuuxPlayer.exe", 0'
  WriteRegDWORD HKLM "$DPPath\InstallInfo" "IconsVisible" "1"
  WriteRegStr HKLM "$DPPath\InstallInfo" "ShowIconsCommand" '"$INSTDIR\QuuxPlayer.exe" /showicons'
  WriteRegStr HKLM "$DPPath\InstallInfo" "HideIconsCommand" '"$INSTDIR\quuxuninstall.exe"'
  WriteRegStr HKLM "$DPPath\InstallInfo" "ReinstallCommand" '"$INSTDIR\QuuxPlayer.exe" /reinstall'  

  StrCpy $ProgID "QuuxPlayer.AACFile"
  StrCpy $FileDesc "Advanced Audio Coding File"
  Call RegVerbs
  StrCpy $Ext ".aac"  
  Call RegExt  
  StrCpy $Ext ".m4a"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.AC3File"
  StrCpy $FileDesc "Dolby Digital File"
  Call RegVerbs
  StrCpy $Ext ".ac3"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.AIFFFile"
  StrCpy $FileDesc "Audio Interchange File Format File"
  Call RegVerbs
  StrCpy $Ext ".aiff"
  Call RegExt
  StrCpy $Ext ".aif"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.ALACFile"
  StrCpy $FileDesc "Apple Lossless Audio Codec File"
  Call RegVerbs
  StrCpy $Ext ".alac"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.APEFile"
  StrCpy $FileDesc "Monkey's Audio Codec File"
  Call RegVerbs
  StrCpy $Ext ".ape"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.FLACFile"
  StrCpy $FileDesc "Free Lossless Audio Codec File"
  Call RegVerbs
  StrCpy $Ext ".flac"
  Call RegExt
  StrCpy $Ext ".fla"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.MP3File"
  StrCpy $FileDesc "MPEG-1 Audio Layer 3 File"
  Call RegVerbs
  StrCpy $Ext ".mp3"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.MPCFile"
  StrCpy $FileDesc "Musepack Audio File"
  Call RegVerbs
  StrCpy $Ext ".mpc"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.OGGFile"
  StrCpy $FileDesc "OGG / Vorbis File"
  Call RegVerbs
  StrCpy $Ext ".ogg"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.WAVFile"
  StrCpy $FileDesc "Waveform Audio Format file"
  Call RegVerbs
  StrCpy $Ext ".wav"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.WMAFile"
  StrCpy $FileDesc "Windows Media Audio File"
  Call RegVerbs
  StrCpy $Ext ".wma"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.WVFile"
  StrCpy $FileDesc "WavPack Audio File"
  Call RegVerbs
  StrCpy $Ext ".wv"
  Call RegExt
  
  StrCpy $ProgID "QuuxPlayer.M3UFile"
  StrCpy $FileDesc "M3U Playlist File"
  Call RegVerbsPlaylist
  StrCpy $Ext ".m3u"
  Call RegExt

  StrCpy $ProgID "QuuxPlayer.PLSFile"
  StrCpy $FileDesc "PLS Playlist File"
  Call RegVerbsPlaylist
  StrCpy $Ext ".pls"
  Call RegExt

  StrCpy $ProgID "Applications\QuuxPlayer.exe"
  Call RegVerbs

  WriteRegStr HKCR "Applications\QuuxPlayer.exe" "FriendlyAppName" "QuuxPlayer Audio Player"

  CreateShortCut "$SMPROGRAMS\QuuxPlayer.lnk" "$INSTDIR\quuxplayer.exe";
  CreateShortCut "$DESKTOP\QuuxPlayer.lnk" "$INSTDIR\quuxplayer.exe";
  CreateShortCut "$QUICKLAUNCH\QuuxPlayer.lnk" "$INSTDIR\quuxplayer.exe"

  WriteRegExpandStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "UninstallString" '"$INSTDIR\quuxuninstall.exe"'
  WriteRegExpandStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "DisplayName" "QuuxPlayer"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "DisplayIcon" "$INSTDIR\quuxplayer.exe,0"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "DisplayVersion" "${VERSION}"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "VersionMajor" "${VER_MAJOR}"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "VersionMinor" "${VER_MINOR}.${VER_REVISION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "URLInfoAbout" "http://www.quuxplayer.com/"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "HelpLink" "http://www.quuxplayer.com/support.php"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "NoModify" "1"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "NoRepair" "1"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer" "Publisher" "Quux Software"

  WriteUninstaller $INSTDIR\quuxuninstall.exe
  
  SetShellVarContext current

SectionEnd

;--------------------------------
;Uninstaller Section

Section Uninstall

  StrCpy $DPPath "Software\Clients\Media\QuuxPlayer"

  Delete /REBOOTOK "$INSTDIR\QuuxPlayer.exe"
  Delete /REBOOTOK "$INSTDIR\beep.wav"
  Delete /REBOOTOK "$INSTDIR\license.rtf"
  Delete /REBOOTOK "$INSTDIR\quuxfile.ico"
  Delete /REBOOTOK "$INSTDIR\RevArrow.cur"

  Delete /REBOOTOK "$INSTDIR\QTextBox.dll"
  Delete /REBOOTOK "$INSTDIR\taglib-sharp.dll"

  Delete /REBOOTOK "$INSTDIR\bass.dll"
  Delete /REBOOTOK "$INSTDIR\Bass.Net.dll"
  Delete /REBOOTOK "$INSTDIR\bass_aac.dll"
  Delete /REBOOTOK "$INSTDIR\bass_ac3.dll"
  Delete /REBOOTOK "$INSTDIR\bass_alac.dll"
  Delete /REBOOTOK "$INSTDIR\bass_ape.dll"
  Delete /REBOOTOK "$INSTDIR\bass_mpc.dll"
  Delete /REBOOTOK "$INSTDIR\bassflac.dll"
  Delete /REBOOTOK "$INSTDIR\basswma.dll"
  Delete /REBOOTOK "$INSTDIR\basswv.dll"
  Delete /REBOOTOK "$INSTDIR\bassasio.dll"
  Delete /REBOOTOK "$INSTDIR\bass_ape.dll"
  Delete /REBOOTOK "$INSTDIR\basscd.dll"
  Delete /REBOOTOK "$INSTDIR\Interop.ITDETECTORLib.dll"
  Delete /REBOOTOK "$INSTDIR\Interop.iTunesLib.dll"
  Delete /REBOOTOK "$INSTDIR\Interop.SHDocVw.dll"
  Delete /REBOOTOK "$INSTDIR\ReplayGain.dll"
  
  Delete /REBOOTOK "$INSTDIR\Microsoft.DirectX.dll"
  Delete /REBOOTOK "$INSTDIR\Microsoft.DirectX.DirectInput.dll"
  Delete /REBOOTOK "$INSTDIR\Microsoft.DirectX.DirectSound.dll"
  
  Delete /REBOOTOK "$INSTDIR\quuxuninstall.exe"

  RMDir /REBOOTOK $INSTDIR
  ${un.GetParent} $INSTDIR $R0
  RMDir /REBOOTOK $R0
  
  Delete /REBOOTOK "$SMPROGRAMS\QuuxPlayer.lnk"
  Delete /REBOOTOK "$Desktop\QuuxPlayer.lnk"
  
  DeleteRegKey HKLM "Software\Classes\Applications\QuuxPlayer.exe"
  
  DeleteRegKey HKLM $DPPath
  
  DeleteRegKey HKLM "Software\Quux Software\QuuxPlayer"
  DeleteRegValue HKLM "Software\RegisteredApplications" "QuuxPlayer"
  DeleteRegKey /ifempty HKLM "Software\Quux Software"  
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\QuuxPlayer"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.AACFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.AC3File"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.AIFFFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.ALACFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.FLACFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.MP3File"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.MPCFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.OGGFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.WAVFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.WMAFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.WVFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.PLSFile"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.M3UFile"
  
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer.Library"
  DeleteRegKey HKLM "Software\Classes\.qxp"

  StrCpy $ProgID "QuuxPlayer.AACFile"
  StrCpy $Ext ".aac"  
  Call un.Reg
  StrCpy $Ext ".m4a"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.AC3File"
  StrCpy $Ext ".ac3"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.AIFFFile"
  StrCpy $Ext ".aiff"
  Call un.Reg
  StrCpy $Ext ".aif"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.ALACFile"
  StrCpy $Ext ".alac"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.APEFile"
  StrCpy $Ext ".ape"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.FLACFile"
  StrCpy $Ext ".flac"
  Call un.Reg
  StrCpy $Ext ".fla"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.MP3File"
  StrCpy $Ext ".mp3"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.MPCFile"
  StrCpy $Ext ".mpc"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.OGGFile"
  StrCpy $Ext ".ogg"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.WAVFile"
  StrCpy $Ext ".wav"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.WMAFile"
  StrCpy $Ext ".wma"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.WVFile"
  StrCpy $Ext ".wv"
  Call un.Reg

  StrCpy $ProgID "QuuxPlayer.PLSFile"
  StrCpy $Ext ".pls"
  Call un.Reg
  
  StrCpy $ProgID "QuuxPlayer.M3UFile"
  StrCpy $Ext ".m3u"
  Call un.Reg

  DeleteRegValue HKLM "Sofware\Classes\.qxp" ""
  DeleteRegKey /ifempty HKLM "Software\Classes\.qxp"
  DeleteRegKey HKLM "Software\Classes\qxp_auto_file"
  DeleteRegKey HKLM "Software\Classes\QuuxPlayer Library"
  
SectionEnd